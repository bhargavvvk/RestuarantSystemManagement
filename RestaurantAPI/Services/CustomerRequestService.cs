using Microsoft.AspNetCore.SignalR;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class CustomerRequestService:ICustomerRequestService
{
    private readonly ICustomerRequestRepository _customerRequestRepository;
    private readonly IRestaurentTableRepository _restaurentTableRepository;
    private readonly IDiningSessionRepository _diningSessionRepository;
    private readonly ILogger<ICustomerRequestService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

    public CustomerRequestService(ICustomerRequestRepository customerRequestRepository, IRestaurentTableRepository restaurentTableRepository,
    IDiningSessionRepository diningSessionRepository,ILogger<ICustomerRequestService> logger,IHubContext<NotificationHub> hubContext)
    {
        _customerRequestRepository = customerRequestRepository;
        _restaurentTableRepository = restaurentTableRepository;
        _diningSessionRepository = diningSessionRepository;
        _logger = logger;
        _hubContext=hubContext;
    }
    public async Task CompleteRequest(int waiterId, int requestId)
    {
        var request =await _customerRequestRepository.GetRequestWithSession(requestId);
        if(request == null)
        {
            throw new CustomerRequestNotFoundException();
        }
        if (request.DiningSession!.Status == DiningSessionStatus.Completed)
        {
            throw new InvalidOperationException("Cannot complete request on a completed session");
        }
        if(request.DiningSession!.WaiterId!= waiterId)
        {
            throw new UnauthorizedAccessException();
        }
        if(request.Status== CustomerRequestStatus.Completed)
        {
            return;
        }
        request.Status =CustomerRequestStatus.Completed;
        request.CompletedAt = DateTime.Now;await _customerRequestRepository.Update(request.Id, request);
        await _customerRequestRepository.SaveChangesAsync();
    }
    public async Task<ICollection<CustomerRequestResponseDto>>GetActiveRequests(int waiterId)
    {
        var requests =await _customerRequestRepository.GetActiveRequestsByWaiterId(waiterId);
        return requests.Select(r =>new CustomerRequestResponseDto
                {
                    RequestId = r.Id,
                    TableNumber =r.DiningSession!.Table!.TableNumber,
                    RequestType =r.RequestType,
                    RequestedAt =r.RequestedAt
                })
            .ToList();
    }
    public async Task<string> CreateRequest(int sessionId, CreateCustomerRequestDto request)
    {
        var session = await _diningSessionRepository.Get(sessionId);
        if(session == null)
        {
            throw new SessionNotFoundException();
        }
        var table =await _restaurentTableRepository.Get(session.TableId);
        if(table == null)
        {
            throw new TableNotFoundException();
        }
        if (!Enum.IsDefined(typeof(CustomerRequestType),request.RequestType))
        {
            throw new RequestTypeException("Invalid RequestType");
        }
        var existingRequest =await _customerRequestRepository.GetPendingRequest(sessionId,request.RequestType);
        if(existingRequest != null)
        {
            return "Request already submitted";
        }
        var notification =new CustomerRequestNotificationDto
        {
            TableNumber = table.TableNumber,
            RequestType =request.RequestType.ToString()
        };
        _logger.LogInformation("Creating customerequest");
        await _customerRequestRepository.Create(new CustomerRequest
            {
                DiningSessionId = sessionId,
                RequestType = request.RequestType,
                RequestedAt = DateTime.Now
            });
        await _customerRequestRepository.SaveChangesAsync();
        _logger.LogInformation("Notifying waiter {WaiterId} about new request", session.WaiterId);
        await _hubContext.Clients.User(session.WaiterId.ToString()).SendAsync("ReceiveCustomerRequest",notification);
        return "Request created successfully";
    }
}
