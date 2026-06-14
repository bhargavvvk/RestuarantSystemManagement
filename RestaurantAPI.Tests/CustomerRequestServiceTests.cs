using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RestaurantAPI;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;

[TestFixture]
public class CustomerRequestServiceTests
{
    private Mock<ICustomerRequestRepository>    _requestRepoMock;
    private Mock<IRestaurentTableRepository>    _tableRepoMock;
    private Mock<IDiningSessionRepository>      _sessionRepoMock;
    private Mock<IHubContext<NotificationHub>>  _hubMock;
    private CustomerRequestService _customerRequestService;

    private const int SessionId   = 10;
    private const int WaiterId    = 5;
    private const int TableId     = 20;
    private const int RequestId   = 30;

    [SetUp]
    public void SetUp()
    {
        _requestRepoMock = new Mock<ICustomerRequestRepository>();
        _tableRepoMock   = new Mock<IRestaurentTableRepository>();
        _sessionRepoMock = new Mock<IDiningSessionRepository>();

       
        var clientsMock = new Mock<IHubClients>();
        var proxyMock   = new Mock<IClientProxy>();
        proxyMock
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(proxyMock.Object);
        _hubMock = new Mock<IHubContext<NotificationHub>>();
        _hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        _requestRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _requestRepoMock
            .Setup(r => r.Create(It.IsAny<CustomerRequest>()))
            .Returns<CustomerRequest>(r => Task.FromResult(r));
        _requestRepoMock
            .Setup(r => r.Update(It.IsAny<int>(), It.IsAny<CustomerRequest>()))
            .ReturnsAsync((int _, CustomerRequest r) => r);

         _customerRequestService = new CustomerRequestService(
            _requestRepoMock.Object,
            _tableRepoMock.Object,
            _sessionRepoMock.Object,
            NullLogger<ICustomerRequestService>.Instance,
            _hubMock.Object);
    }

   

    private static DiningSession ActiveSession() => new()
    {
        Id       = SessionId,
        TableId  = TableId,
        WaiterId = WaiterId,
        Status   = DiningSessionStatus.Active
    };

    private static RestaurantTable SomeTable() => new()
    {
        Id          = TableId,
        TableNumber = "T1"
    };

    private static CustomerRequest PendingRequest(int id = RequestId) => new()
    {
        Id              = id,
        DiningSessionId = SessionId,
        RequestType     = CustomerRequestType.CallWaiter,
        Status          = CustomerRequestStatus.Pending,
        DiningSession   = new DiningSession
        {
            Id       = SessionId,
            WaiterId = WaiterId,
            Status   = DiningSessionStatus.Active,
            Table    = SomeTable()
        }
    };

    

    [Test]
    public async Task CreateRequest_Success()
    {
        _sessionRepoMock.Setup(r => r.Get(SessionId)).ReturnsAsync(ActiveSession());
        _tableRepoMock.Setup(r => r.Get(TableId)).ReturnsAsync(SomeTable());
        _requestRepoMock
            .Setup(r => r.GetPendingRequest(SessionId, CustomerRequestType.CallWaiter))
            .ReturnsAsync((CustomerRequest?)null);

        var result = await  _customerRequestService.CreateRequest(SessionId, new CreateCustomerRequestDto
        {
            RequestType = CustomerRequestType.CallWaiter
        });

        Assert.That(result, Is.EqualTo("Request created successfully"));
        _requestRepoMock.Verify(r => r.Create(It.IsAny<CustomerRequest>()), Times.Once);
        _requestRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void CreateRequest_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.Get(SessionId)).ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () =>  _customerRequestService.CreateRequest(SessionId, new CreateCustomerRequestDto
            {
                RequestType = CustomerRequestType.CallWaiter
            }));
    }

    [Test]
    public void CreateRequest_TableNotFound()
    {
        _sessionRepoMock.Setup(r => r.Get(SessionId)).ReturnsAsync(ActiveSession());
        _tableRepoMock.Setup(r => r.Get(TableId)).ReturnsAsync((RestaurantTable?)null);

        Assert.ThrowsAsync<TableNotFoundException>(
            () =>  _customerRequestService.CreateRequest(SessionId, new CreateCustomerRequestDto
            {
                RequestType = CustomerRequestType.CallWaiter
            }));
    }

    [Test]
    public void CreateRequest_InvalidRequestType()
    {
        _sessionRepoMock.Setup(r => r.Get(SessionId)).ReturnsAsync(ActiveSession());
        _tableRepoMock.Setup(r => r.Get(TableId)).ReturnsAsync(SomeTable());

        Assert.ThrowsAsync<RequestTypeException>(
            () =>  _customerRequestService.CreateRequest(SessionId, new CreateCustomerRequestDto
            {
                RequestType = (CustomerRequestType)999 // invalid enum value
            }));
    }

    [Test]
    public async Task CreateRequest_PendingExists()
    {
        _sessionRepoMock.Setup(r => r.Get(SessionId)).ReturnsAsync(ActiveSession());
        _tableRepoMock.Setup(r => r.Get(TableId)).ReturnsAsync(SomeTable());
        _requestRepoMock
            .Setup(r => r.GetPendingRequest(SessionId, CustomerRequestType.CallWaiter))
            .ReturnsAsync(PendingRequest());

        var result = await  _customerRequestService.CreateRequest(SessionId, new CreateCustomerRequestDto
        {
            RequestType = CustomerRequestType.CallWaiter
        });

        Assert.That(result, Is.EqualTo("Request already submitted"));
        _requestRepoMock.Verify(r => r.Create(It.IsAny<CustomerRequest>()), Times.Never);
    }

   

    [Test]
    public async Task GetActiveRequests_Sucess()
    {
        var requests = new List<CustomerRequest> { PendingRequest() };
        _requestRepoMock.Setup(r => r.GetActiveRequestsByWaiterId(WaiterId)).ReturnsAsync(requests);

        var result = await  _customerRequestService.GetActiveRequests(WaiterId);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.First().RequestId,    Is.EqualTo(RequestId));
        Assert.That(result.First().TableNumber,  Is.EqualTo("T1"));
        Assert.That(result.First().RequestType,  Is.EqualTo(CustomerRequestType.CallWaiter));
    }

    [Test]
    public async Task GetActiveRequests_EmptyCollection()
    {
        _requestRepoMock
            .Setup(r => r.GetActiveRequestsByWaiterId(WaiterId))
            .ReturnsAsync(new List<CustomerRequest>());

        var result = await  _customerRequestService.GetActiveRequests(WaiterId);

        Assert.That(result.Count, Is.EqualTo(0));
    }

    

    [Test]
    public async Task CompleteRequest()
    {
        var request = PendingRequest();
        _requestRepoMock.Setup(r => r.GetRequestWithSession(RequestId)).ReturnsAsync(request);

        await  _customerRequestService.CompleteRequest(WaiterId, RequestId);

        Assert.That(request.Status, Is.EqualTo(CustomerRequestStatus.Completed));
        _requestRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void CompleteRequest_RequestNotFound()
    {
        _requestRepoMock.Setup(r => r.GetRequestWithSession(RequestId))
                        .ReturnsAsync((CustomerRequest?)null);

        Assert.ThrowsAsync<CustomerRequestNotFoundException>(
            () =>  _customerRequestService.CompleteRequest(WaiterId, RequestId));
    }

    [Test]
    public void CompleteRequest_SessionCompleted()
    {
        var request = PendingRequest();
        request.DiningSession!.Status = DiningSessionStatus.Completed;

        _requestRepoMock.Setup(r => r.GetRequestWithSession(RequestId)).ReturnsAsync(request);

        Assert.ThrowsAsync<InvalidOperationException>(
            () =>  _customerRequestService.CompleteRequest(WaiterId, RequestId));
    }

    [Test]
    public void CompleteRequest_UnauthorizedWaiter()
    {
        var request = PendingRequest();
        request.DiningSession!.WaiterId = 999; // different waiter

        _requestRepoMock.Setup(r => r.GetRequestWithSession(RequestId)).ReturnsAsync(request);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () =>  _customerRequestService.CompleteRequest(WaiterId, RequestId));
    }

    [Test]
    public async Task CompleteRequest_AlreadyCompleted()
    {
        var request = PendingRequest();
        request.Status = CustomerRequestStatus.Completed;

        _requestRepoMock.Setup(r => r.GetRequestWithSession(RequestId)).ReturnsAsync(request);

        await  _customerRequestService.CompleteRequest(WaiterId, RequestId);

        // Returns early — no save should happen
        _requestRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
