using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.Repositories;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class DiningSessionService:IDiningSessionService
{
    readonly IDiningSessionRepository _diningSessionRepository;
    private readonly IBillRepository _billRepository;
    private readonly IRestaurentTableRepository _restaurentTableRepository;
    private readonly IUserRepository _userRepository;
    readonly IOrderItemRepository _orderItemRepository;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly RestaurantContext _context;
    private readonly ILogger<IDiningSessionService> _logger;
    private readonly ICartRepository _cartRepository;
    private readonly ITokenService _tokenService;
    private readonly IEncryptionService _encryptionService;
    private readonly ICustomerRepository _customerRepository;
     private readonly ITaxConfigurationRepository _taxConfigurationRepository;
    public DiningSessionService(IDiningSessionRepository diningSessionRepository, IBillRepository billRepository, IRestaurentTableRepository restaurentTableRepository,
    IUserRepository userRepository, IOrderItemRepository orderItemRepository, IHubContext<NotificationHub> hubContext, RestaurantContext context,
    ILogger<IDiningSessionService> logger, ICartRepository cartRepository, ITokenService tokenService, IEncryptionService encryptionService,
     ICustomerRepository customerRepository, ITaxConfigurationRepository taxConfigurationRepository)
    {
        _diningSessionRepository = diningSessionRepository;
            _billRepository = billRepository;
            _restaurentTableRepository = restaurentTableRepository;
            _userRepository = userRepository;
            _orderItemRepository = orderItemRepository;
        _hubContext = hubContext;
        _context = context;
        _logger = logger;
        _cartRepository = cartRepository;
        _tokenService = tokenService;
        _encryptionService = encryptionService;
        _customerRepository = customerRepository;
        _taxConfigurationRepository=taxConfigurationRepository;
    }
    public async Task CloseSession(int waiterId,int tableId)
    {
        _logger.LogInformation("Waiter {WaiterId} closing session for table {TableId}", waiterId, tableId);
        var session = await _diningSessionRepository.GetActiveSessionByTableId(tableId);

        if (session == null)
        {
            throw new SessionNotFoundException();
        }
        if (session.WaiterId != waiterId)
        {
            throw new UnauthorizedAccessException("Table is not assigned to the logged-in waiter.");
        }

        var bill = await _billRepository.GetBySessionId(session.Id);

        if (bill == null)
        {
            throw new BillNotFoundException();
        }

        if (bill.PaymentStatus != PaymentStatus.Paid)
        {
            throw new DiningSessionException("Bill must be paid before closing the session.");
        }
        using var transaction = await _context.Database.BeginTransactionAsync();
        var table = await _restaurentTableRepository.Get(session.TableId);
        var pendingOrderItems = await _orderItemRepository.GetActiveOrderItemsBySessionId(session.Id);
        try
        {
            foreach (var orderItem in pendingOrderItems)
            {
                orderItem.Status = OrderItemStatus.Cancelled;
                await _orderItemRepository.Update(orderItem.Id,orderItem);
            }
            await _orderItemRepository.SaveChangesAsync();
            session.Status = DiningSessionStatus.Completed;
            session.EndedAt = DateTime.Now;
            await _diningSessionRepository.Update(session.Id,session);
            await _diningSessionRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("Session {SessionId} closed for table {TableId}", session.Id, tableId);
            var affectedOrderIds = pendingOrderItems.Select(oi => oi.OrderId).Distinct().ToList();
            foreach(var orderId in affectedOrderIds)
            {
                var kitchenUser = await _userRepository.GetByRole(UserRole.KitchenStaff);
                await _hubContext.Clients.User(kitchenUser!.Id.ToString()).SendAsync("ReceiveOrderCancelled",
                    new OrderCancelledNotificationDto
                    {
                        OrderId = orderId,
                        TableNumber = table!.TableNumber,
                        Message ="Dining session closed. Remaining items cancelled."
                    });
            }
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        await _hubContext.Clients.Group($"session-{session.Id}").SendAsync("SessionClosed");
    }
    public async Task<JoinSessionResponseDto> JoinSession(string qrIdentifier,JoinSessionRequestDto request)
    {
        var table = await _restaurentTableRepository.GetByQrIdentifier(qrIdentifier);
        if(table == null)
        {
            throw new TableNotFoundException();
        }
        _logger.LogInformation("Joining session for table {TableNumber}", table.TableNumber);
        var session = await _diningSessionRepository.GetActiveSessionByTableId(table.Id);
        if(session == null)
        {
            throw new Exception("no active dining session found for the table");
        }
        if(session.SessionOtp != request.SessionOtp)
        {
            throw new InvalidSessionOtpException();
        }
        var cart = await _cartRepository.GetByDiningSessionId(session.Id);
        var token = _tokenService.CreateCustomerToken(
            new CustomerTokenRequest
            {
                SessionId = session.Id,
                TableId = table.Id,
                CartId = cart!.Id,
                WaiterId = session.WaiterId
            });
        _logger.LogInformation("Customer joined session {SessionId}", session.Id);
        return new JoinSessionResponseDto
        {
            Token = token,
            SessionOtp=session.SessionOtp
        };
    }
    public async Task<CreateSessionResponseDto> CreateSession(string qrIdentifier,CreateSessionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            throw new ValidationException("Customer name is required.");
        }

        if (request.CustomerName.Length > 15)
        {
            throw new ValidationException("Customer name cannot exceed 15 characters.");
        }

        if (!Regex.IsMatch(request.PhoneNumber, @"^\d{10}$"))
        {
            throw new ValidationException("Phone number must contain exactly 10 digits.");
        }
        
        var table =await _restaurentTableRepository.GetByQrIdentifier(qrIdentifier);

        if(table == null)
        {
            throw new TableNotFoundException();
        }
        if(table.Status != TableStatus.Available)
        {
            throw new TableUnavailableException();
        }
        var activeSession =await _diningSessionRepository.GetActiveSessionByTableId(table.Id);
        if(activeSession != null)
        {
            throw new ActiveDiningSessionExistsException();
        }
        _logger.LogInformation("Creating session for table {TableNumber}", table.Id);
        var mobileHash =_encryptionService.GenerateHash(request.PhoneNumber);
        var encryptedMobile =_encryptionService.Encrypt(request.PhoneNumber);
        var customer = await _customerRepository.GetByPhoneNumberHash(mobileHash);
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (customer == null)
            {
                _logger.LogInformation("creating new customer");
                customer=await _customerRepository.Create(new Customer
                {
                    Name = request.CustomerName,
                    EncryptedMobileNumber = encryptedMobile,
                    MobileNumberHash = mobileHash ,
                    CreatedAt = DateTime.Now
                });
                await _customerRepository.SaveChangesAsync();
            }
            var otp = await GenerateUniqueOtp();
            _logger.LogInformation("session otp is generated");
            _logger.LogInformation("creating new session");
            var session =await _diningSessionRepository.Create(
                    new DiningSession
                    {
                        CustomerId = customer.Id,
                        TableId = table.Id,
                        WaiterId =table.AssignedWaiterId!.Value,
                        SessionOtp = otp,
                        Status = DiningSessionStatus.Active,
                        StartedAt = DateTime.Now
                    });
            await _diningSessionRepository.SaveChangesAsync();
             _logger.LogInformation("creating new cart associated with session {SessionId}", session.Id);
            var cart =await _cartRepository.Create(
                new Cart
                {
                    DiningSessionId =session.Id,
                    CreatedAt =DateTime.Now
                });
            await _cartRepository.SaveChangesAsync();
            _logger.LogInformation("creating bill associated with session {SessionId}",session.Id);
            await _billRepository.Create(
                new Bill
                {
                    BillNumber = await GenerateBillNumber(),
                    DiningSessionId = session.Id,
                    TaxConfigurationId = (await _taxConfigurationRepository.GetActiveConfiguration())!.Id,
                    FoodTotal = 0,
                    CgstAmount = 0,
                    SgstAmount = 0,
                    ServiceChargeAmount = 0,
                    GrandTotal = 0,
                    GeneratedAt = DateTime.Now
                } 
            );
            await _billRepository.SaveChangesAsync();
            _logger.LogInformation("token is generated");
            var token = _tokenService.CreateCustomerToken(
            new CustomerTokenRequest
            {
                SessionId =session.Id,
                TableId = table.Id,
                CartId = cart.Id,
                WaiterId =session.WaiterId
            });
            await transaction.CommitAsync();
            await _hubContext.Clients.User(session.WaiterId.ToString()).SendAsync("SessionCreated", $"Session Created at table {table.TableNumber}");
            _logger.LogInformation("SessionCreated SignalR fired for waiter {WaiterId} on table {TableNumber}", session.WaiterId, table.TableNumber);
            return new CreateSessionResponseDto
            {
                Token = token,
                SessionOtp = otp
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    private async Task<string> GenerateUniqueOtp()
    {
        string otp;
        do
        {
            otp = Random.Shared.Next(1000, 9999).ToString();
        }
        while (
            await _diningSessionRepository.GetActiveSessionByOtp(otp) != null);
        return otp;
    }
    private async Task<string> GenerateBillNumber()
    {
        var todayPart =DateTime.Today.ToString("yyyyMMdd");
        var latestBillNumber = await _billRepository.GetLatestBillNumberToday();
        if (string.IsNullOrEmpty(latestBillNumber))
        {
            return $"{todayPart}001";
        }
        var sequencePart =latestBillNumber[^3..];
        var nextSequence =(int.Parse(sequencePart) + 1).ToString("D3");
        return $"{todayPart}{nextSequence}";
    }
    public async Task<SessionValidationResponseDto>ValidateSession(int sessionId)
    {
        var session =await _diningSessionRepository.Get(sessionId);
        if (session == null)
        {
            return new SessionValidationResponseDto
            {
                IsActive = false
            };
        }
        return new SessionValidationResponseDto
        {
            IsActive = session.Status == DiningSessionStatus.Active,
            TableIdentifier = session.Table?.QrIdentifier
        };
    }
}
