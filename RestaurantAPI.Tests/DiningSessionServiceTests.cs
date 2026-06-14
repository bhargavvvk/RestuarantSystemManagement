using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RestaurantAPI;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;


public class DiningSessionServiceTests
{
    private Mock<IDiningSessionRepository>    _sessionRepoMock;
    private Mock<IBillRepository>             _billRepoMock;
    private Mock<IRestaurentTableRepository>  _tableRepoMock;
    private Mock<IUserRepository>             _userRepoMock;
    private Mock<IOrderItemRepository>        _orderItemRepoMock;
    private Mock<ICartRepository>             _cartRepoMock;
    private Mock<ITokenService>               _tokenMock;
    private Mock<IEncryptionService>          _encryptionMock;
    private Mock<ICustomerRepository>         _customerRepoMock;
    private Mock<ITaxConfigurationRepository> _taxRepoMock;
    private Mock<IHubContext<NotificationHub>> _hubMock;
    private RestaurantContext                 _context;
    private DiningSessionService             _diningSessionService;

    private const string Qr        = "qr-abc";
    private const string ValidPhone = "9876543210";
    private const string Otp        = "1234";
    private const int    TableId    = 1;
    private const int    SessionId  = 10;
    private const int    WaiterId   = 5;
    private const int    CartId     = 20;
    private const int    CustomerId = 30;

    [SetUp]
    public void SetUp()
    {
        _sessionRepoMock   = new Mock<IDiningSessionRepository>();
        _billRepoMock      = new Mock<IBillRepository>();
        _tableRepoMock     = new Mock<IRestaurentTableRepository>();
        _userRepoMock      = new Mock<IUserRepository>();
        _orderItemRepoMock = new Mock<IOrderItemRepository>();
        _cartRepoMock      = new Mock<ICartRepository>();
        _tokenMock         = new Mock<ITokenService>();
        _encryptionMock    = new Mock<IEncryptionService>();
        _customerRepoMock  = new Mock<ICustomerRepository>();
        _taxRepoMock       = new Mock<ITaxConfigurationRepository>();

        
        var clientsMock = new Mock<IHubClients>();
        var proxyMock   = new Mock<IClientProxy>();
        proxyMock.Setup(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(proxyMock.Object);
        _hubMock = new Mock<IHubContext<NotificationHub>>();
        _hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var options = new DbContextOptionsBuilder<RestaurantContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _context = new RestaurantContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        
        _encryptionMock.Setup(e => e.GenerateHash(It.IsAny<string>())).Returns<string>(v => $"HASH_{v}");
        _encryptionMock.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(v => $"ENC_{v}");

       
        _tokenMock.Setup(t => t.CreateCustomerToken(It.IsAny<CustomerTokenRequest>())).Returns("test-token");

       
        _sessionRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _cartRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _billRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _customerRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _orderItemRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

      
        _sessionRepoMock.Setup(r => r.GetActiveSessionByOtp(It.IsAny<string>()))
                        .ReturnsAsync((DiningSession?)null);

        
        _billRepoMock.Setup(r => r.GetLatestBillNumberToday()).ReturnsAsync((string?)null);

       _diningSessionService = new DiningSessionService(
            _sessionRepoMock.Object,
            _billRepoMock.Object,
            _tableRepoMock.Object,
            _userRepoMock.Object,
            _orderItemRepoMock.Object,
            _hubMock.Object,
            _context,
            NullLogger<IDiningSessionService>.Instance,
            _cartRepoMock.Object,
            _tokenMock.Object,
            _encryptionMock.Object,
            _customerRepoMock.Object,
            _taxRepoMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    private static RestaurantTable AvailableTable() => new()
    {
        Id               = TableId,
        QrIdentifier     = Qr,
        TableNumber      = "T1",
        Status           = TableStatus.Available,
        AssignedWaiterId = WaiterId
    };

    private static DiningSession ActiveSession() => new()
    {
        Id       = SessionId,
        TableId  = TableId,
        WaiterId = WaiterId,
        Status   = DiningSessionStatus.Active,
        SessionOtp = Otp,
        Cart     = new Cart { Id = CartId, DiningSessionId = SessionId }
    };

    private void SetUpCreateSessionHappyPath()
    {
        _tableRepoMock.Setup(r => r.GetByQrIdentifier(Qr)).ReturnsAsync(AvailableTable());
        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync((DiningSession?)null);
        _customerRepoMock.Setup(r => r.GetByPhoneNumberHash($"HASH_{ValidPhone}")).ReturnsAsync((Customer?)null);

        var customer = new Customer { Id = CustomerId, Name = "Alice" };
        _customerRepoMock.Setup(r => r.Create(It.IsAny<Customer>())).ReturnsAsync(customer);

        var session = ActiveSession();
        _sessionRepoMock.Setup(r => r.Create(It.IsAny<DiningSession>())).ReturnsAsync(session);

        var cart = new Cart { Id = CartId };
        _cartRepoMock.Setup(r => r.Create(It.IsAny<Cart>())).ReturnsAsync(cart);
        _billRepoMock.Setup(r => r.Create(It.IsAny<Bill>())).Returns<Bill>(b => Task.FromResult(b));

        var tax = new TaxConfiguration { Id = 1, CgstPercentage = 5, SgstPercentage = 5, ServiceChargePercentage = 5, IsActive = true };
        _taxRepoMock.Setup(r => r.GetActiveConfiguration()).ReturnsAsync(tax);
    }

   
    [Test]
    public async Task CreateSession_Success()
    {
        SetUpCreateSessionHappyPath();

        var result = await _diningSessionService.CreateSession(Qr, new CreateSessionRequestDto
        {
            CustomerName = "Alice",
            PhoneNumber  = ValidPhone
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Token,      Is.EqualTo("test-token"));
            Assert.That(result.SessionOtp, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public void CreateSession_PhoneNumberInvalid()
    {
        Assert.ThrowsAsync<ValidationException>(
            () =>_diningSessionService.CreateSession(Qr, new CreateSessionRequestDto
            {
                CustomerName = "Alice",
                PhoneNumber  = "123" 
            }));
    }

    [Test]
    public void CreateSession_TableNotFound()
    {
        _tableRepoMock.Setup(r => r.GetByQrIdentifier(Qr)).ReturnsAsync((RestaurantTable?)null);

        Assert.ThrowsAsync<TableNotFoundException>(
            () =>_diningSessionService.CreateSession(Qr, new CreateSessionRequestDto
            {
                CustomerName = "Alice",
                PhoneNumber  = ValidPhone
            }));
    }

    [Test]
    public void CreateSession_TableUnavailable()
    {
        var table = AvailableTable();
        table.Status = TableStatus.Unavailable;
        _tableRepoMock.Setup(r => r.GetByQrIdentifier(Qr)).ReturnsAsync(table);

        Assert.ThrowsAsync<TableUnavailableException>(
            () =>_diningSessionService.CreateSession(Qr, new CreateSessionRequestDto
            {
                CustomerName = "Alice",
                PhoneNumber  = ValidPhone
            }));
    }

    [Test]
    public void CreateSession_ActiveSessionExists()
    {
        _tableRepoMock.Setup(r => r.GetByQrIdentifier(Qr)).ReturnsAsync(AvailableTable());
        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(ActiveSession());

        Assert.ThrowsAsync<ActiveDiningSessionExistsException>(
            () =>_diningSessionService.CreateSession(Qr, new CreateSessionRequestDto
            {
                CustomerName = "Alice",
                PhoneNumber  = ValidPhone
            }));
    }

   
    [Test]
    public async Task JoinSession_Sucess()
    {
        var session = ActiveSession();
        _tableRepoMock.Setup(r => r.GetByQrIdentifier(Qr)).ReturnsAsync(AvailableTable());
        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);
        _cartRepoMock.Setup(r => r.GetByDiningSessionId(SessionId)).ReturnsAsync(session.Cart);

        var result = await _diningSessionService.JoinSession(Qr, new JoinSessionRequestDto { SessionOtp = Otp });

        Assert.That(result.Token, Is.EqualTo("test-token"));
    }

    [Test]
    public void JoinSession_TableNotFound()
    {
        _tableRepoMock.Setup(r => r.GetByQrIdentifier(Qr)).ReturnsAsync((RestaurantTable?)null);

        Assert.ThrowsAsync<TableNotFoundException>(
            () =>_diningSessionService.JoinSession(Qr, new JoinSessionRequestDto { SessionOtp = Otp }));
    }

    [Test]
    public void JoinSession_NoActiveSession()
    {
        _tableRepoMock.Setup(r => r.GetByQrIdentifier(Qr)).ReturnsAsync(AvailableTable());
        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<Exception>(
            () =>_diningSessionService.JoinSession(Qr, new JoinSessionRequestDto { SessionOtp = Otp }));
    }

    [Test]
    public void JoinSession_OtpInvalid()
    {
        _tableRepoMock.Setup(r => r.GetByQrIdentifier(Qr)).ReturnsAsync(AvailableTable());
        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(ActiveSession());

        Assert.ThrowsAsync<InvalidSessionOtpException>(
            () =>_diningSessionService.JoinSession(Qr, new JoinSessionRequestDto { SessionOtp = "9999" })); // wrong OTP
    }

    

    [Test]
    public async Task CloseSession_Success()
    {
        var session = ActiveSession();
        var bill    = new Bill { Id = 1, DiningSessionId = SessionId, PaymentStatus = PaymentStatus.Paid };

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);
        _billRepoMock.Setup(r => r.GetBySessionId(SessionId)).ReturnsAsync(bill);
        _tableRepoMock.Setup(r => r.Get(TableId)).ReturnsAsync(AvailableTable());
        _orderItemRepoMock.Setup(r => r.GetActiveOrderItemsBySessionId(SessionId))
                          .ReturnsAsync(new List<OrderItem>());
        _sessionRepoMock.Setup(r => r.Update(SessionId, It.IsAny<DiningSession>()))
                        .ReturnsAsync(session);

        await _diningSessionService.CloseSession(WaiterId, TableId);

        Assert.That(session.Status, Is.EqualTo(DiningSessionStatus.Completed));
        _sessionRepoMock.Verify(r => r.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Test]
    public void CloseSession_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId))
                        .ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () =>_diningSessionService.CloseSession(WaiterId, TableId));
    }

    [Test]
    public void CloseSession_UnauthorizedWaiter()
    {
        var session = ActiveSession();
        session.WaiterId = 999; // different waiter

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () =>_diningSessionService.CloseSession(WaiterId, TableId));
    }

    [Test]
    public void CloseSession_BillNotFound()
    {
        var session = ActiveSession();
        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);
        _billRepoMock.Setup(r => r.GetBySessionId(SessionId)).ReturnsAsync((Bill?)null);

        Assert.ThrowsAsync<BillNotFoundException>(
            () =>_diningSessionService.CloseSession(WaiterId, TableId));
    }

    [Test]
    public void CloseSession_BillNotPaid()
    {
        var session = ActiveSession();
        var bill    = new Bill { Id = 1, DiningSessionId = SessionId, PaymentStatus = PaymentStatus.Pending };

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);
        _billRepoMock.Setup(r => r.GetBySessionId(SessionId)).ReturnsAsync(bill);

        Assert.ThrowsAsync<DiningSessionException>(
            () =>_diningSessionService.CloseSession(WaiterId, TableId));
    }
}
