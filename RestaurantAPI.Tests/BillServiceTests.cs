using AutoMapper;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;

public class BillServiceTests
{
    private Mock<IDiningSessionRepository>    _sessionRepoMock;
    private Mock<IBillRepository>             _billRepoMock;
    private Mock<ITaxConfigurationRepository> _taxRepoMock;
    private Mock<IOrderRepository>            _orderRepoMock;
    private Mock<IAuditService>               _auditMock;
    private Mock<IMapper>                     _mapperMock;
    private BillService                       _billService;

    [SetUp]
    public void SetUp()
    {
        _sessionRepoMock = new Mock<IDiningSessionRepository>();
        _billRepoMock    = new Mock<IBillRepository>();
        _taxRepoMock     = new Mock<ITaxConfigurationRepository>();
        _orderRepoMock   = new Mock<IOrderRepository>();
        _auditMock       = new Mock<IAuditService>();
        _mapperMock      = new Mock<IMapper>();

        _auditMock
            .Setup(a => a.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuditAction>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _billRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _billRepoMock
            .Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Bill>()))
            .ReturnsAsync((int _, Bill b) => b);

       _billService = new BillService(
            _sessionRepoMock.Object,
            NullLogger<BillService>.Instance,
            _billRepoMock.Object,
            _mapperMock.Object,
            _taxRepoMock.Object,
            _auditMock.Object,
            _orderRepoMock.Object);
    }

    private static Bill PendingBill(int id = 1, int sessionId = 10) => new()
    {
        Id                  = id,
        BillNumber          = "20250101001",
        DiningSessionId     = sessionId,
        TaxConfigurationId  = 1,
        FoodTotal           = 100m,
        CgstAmount          = 5m,
        SgstAmount          = 5m,
        ServiceChargeAmount = 5m,
        GrandTotal          = 115m,
        PaymentStatus       = PaymentStatus.Pending,
        GeneratedAt         = DateTime.UtcNow
    };

    private static TaxConfiguration ActiveTax() => new()
    {
        Id                      = 1,
        CgstPercentage          = 5m,
        SgstPercentage          = 5m,
        ServiceChargePercentage = 5m,
        IsActive                = true
    };

    private IReadOnlyList<Bill> SeedBills()
    {
        var sessions = new[]
        {
            new DiningSession
            {
                Id = 1, TableId = 1, Table = new RestaurantTable { TableNumber = "T1" }
            },
            new DiningSession
            {
                Id = 2, TableId = 2, Table = new RestaurantTable { TableNumber = "T2" }
            },
            new DiningSession
            {
                Id = 3, TableId = 3, Table = new RestaurantTable { TableNumber = "T3" }
            }
        };

        var today = DateTime.Today;
        var bills = new[]
        {
            new Bill
            {
                Id = 1, BillNumber = "BILL-OLD", DiningSessionId = sessions[0].Id,
                DiningSession = sessions[0],
                TaxConfigurationId = 1, FoodTotal = 80m, CgstAmount = 5m,
                SgstAmount = 5m, GrandTotal = 90m,
                GeneratedAt = today.AddDays(-1).AddHours(18)
            },
            new Bill
            {
                Id = 2, BillNumber = "BILL-TODAY-1", DiningSessionId = sessions[1].Id,
                DiningSession = sessions[1],
                TaxConfigurationId = 1, FoodTotal = 100m, CgstAmount = 5m,
                SgstAmount = 5m, GrandTotal = 110m,
                GeneratedAt = today.AddHours(10)
            },
            new Bill
            {
                Id = 3, BillNumber = "SPECIAL-TODAY-2", DiningSessionId = sessions[2].Id,
                DiningSession = sessions[2],
                TaxConfigurationId = 1, FoodTotal = 200m, CgstAmount = 10m,
                SgstAmount = 10m, ServiceChargeAmount = 10m, GrandTotal = 230m,
                GeneratedAt = today.AddHours(12)
            }
        };

        _billRepoMock
            .Setup(r => r.GetBillsQuery())
            .Returns(new TestAsyncEnumerable<Bill>(bills));

        return bills;
    }



    [Test]
    public async Task GetBill_ReturnsBill()
    {
        var bill     = PendingBill();
        var response = new BillResponseDto { BillNumber = bill.BillNumber };

        _sessionRepoMock.Setup(r => r.Get(10)).ReturnsAsync(new DiningSession { Id = 10 });
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(bill);
        _mapperMock.Setup(m => m.Map<BillResponseDto>(bill)).Returns(response);

        var result = await _billService.GetBill(10);

        Assert.That(result.BillNumber, Is.EqualTo(bill.BillNumber));
    }

    [Test]
    public void GetBill_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.Get(10)).ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(() =>_billService.GetBill(10));
    }

    [Test]
    public void GetBill_BillNotFound()
    {
        _sessionRepoMock.Setup(r => r.Get(10)).ReturnsAsync(new DiningSession { Id = 10 });
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync((Bill?)null);

        Assert.ThrowsAsync<BillNotFoundException>(() =>_billService.GetBill(10));
    }



    [Test]
    public async Task MarkBillAsPaid_Success()
    {
        var bill = PendingBill();
        _sessionRepoMock.Setup(r => r.Get(10)).ReturnsAsync(new DiningSession { Id = 10 });
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(bill);
        _mapperMock.Setup(m => m.Map<BillResponseDto>(bill)).Returns(new BillResponseDto());

        await _billService.MarkBillAsPaid(10, PaymentMethod.Cash);

        Assert.Multiple(() =>
        {
            Assert.That(bill.PaymentStatus, Is.EqualTo(PaymentStatus.Paid));
            Assert.That(bill.PaymentMethod, Is.EqualTo(PaymentMethod.Cash));
            Assert.That(bill.PaidAt,        Is.Not.Null);
        });
        _billRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void MarkBillAsPaid_AlreadyPaid()
    {
        var bill = PendingBill();
        bill.PaymentStatus = PaymentStatus.Paid;

        _sessionRepoMock.Setup(r => r.Get(10)).ReturnsAsync(new DiningSession { Id = 10 });
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(bill);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>_billService.MarkBillAsPaid(10, PaymentMethod.Cash));
    }

    [Test]
    public void MarkBillAsPaid_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.Get(10)).ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () =>_billService.MarkBillAsPaid(10, PaymentMethod.Card));
        _billRepoMock.Verify(r => r.GetBySessionId(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public void MarkBillAsPaid_BillNotFound()
    {
        _sessionRepoMock.Setup(r => r.Get(10)).ReturnsAsync(new DiningSession { Id = 10 });
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync((Bill?)null);

        Assert.ThrowsAsync<BillNotFoundException>(
            () =>_billService.MarkBillAsPaid(10, PaymentMethod.Card));
    }


    [Test]
    public void GetPaymentMethods_ReturnsAllMethods()
    {
        var result =_billService.GetPaymentMethods();

        Assert.That(result.Count, Is.EqualTo(Enum.GetValues<PaymentMethod>().Length));
    }

    

    [Test]
    public async Task UpdateServiceCharge_Disable()
    {
        var bill = PendingBill(); // ServiceChargeAmount=5, GrandTotal=115
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(bill);
        _mapperMock.Setup(m => m.Map<BillResponseDto>(bill)).Returns(new BillResponseDto());

        await _billService.UpdateServiceCharge(10, false);

        Assert.Multiple(() =>
        {
            Assert.That(bill.ServiceChargeAmount, Is.EqualTo(0));
            Assert.That(bill.GrandTotal,          Is.EqualTo(110m)); 
        });
        _auditMock.Verify(a => a.LogAsync(
            nameof(Bill), It.IsAny<string>(), AuditAction.Updated,
            It.IsAny<object>(), It.IsAny<object>(), "Service charge disabled"), Times.Once);
    }

    [Test]
    public async Task UpdateServiceCharge_Enable()
    {
        var bill = PendingBill();
        bill.ServiceChargeAmount = 0;   
        bill.GrandTotal          = 110m;

        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(bill);
        _taxRepoMock.Setup(r => r.Get(bill.TaxConfigurationId)).ReturnsAsync(ActiveTax());
        _mapperMock.Setup(m => m.Map<BillResponseDto>(bill)).Returns(new BillResponseDto());

        await _billService.UpdateServiceCharge(10, true);

        // FoodTotal=100 × 5% = 5
        Assert.Multiple(() =>
        {
            Assert.That(bill.ServiceChargeAmount, Is.EqualTo(5m));
            Assert.That(bill.GrandTotal,          Is.EqualTo(115m));
        });
        _auditMock.Verify(a => a.LogAsync(
            nameof(Bill), It.IsAny<string>(), AuditAction.Updated,
            It.IsAny<object>(), It.IsAny<object>(), "Service charge enabled"), Times.Once);
    }

    [Test]
    public void UpdateServiceCharge_TaxConfigMissing()
    {
        var bill = PendingBill();
        bill.ServiceChargeAmount = 0;

        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(bill);
        _taxRepoMock.Setup(r => r.Get(bill.TaxConfigurationId)).ReturnsAsync((TaxConfiguration?)null);

        var ex = Assert.ThrowsAsync<Exception>(() =>_billService.UpdateServiceCharge(10, true));
        Assert.That(ex!.Message, Is.EqualTo("Tax configuration not found"));
    }

    [Test]
    public void UpdateServiceCharge_BillNotFound()
    {
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync((Bill?)null);

        Assert.ThrowsAsync<BillNotFoundException>(
            () =>_billService.UpdateServiceCharge(10, true));
    }

    [Test]
    public void UpdateServiceCharge_AlreadyPaid()
    {
        var bill = PendingBill();
        bill.PaymentStatus = PaymentStatus.Paid;
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(bill);

        var ex = Assert.ThrowsAsync<Exception>(
            () =>_billService.UpdateServiceCharge(10, false));

        Assert.That(ex!.Message, Is.EqualTo("Bill Already Paid"));
        _billRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

 

    [Test]
    public async Task RecalculateBill_Success()
    {
        var bill = PendingBill();
        bill.ServiceChargeAmount = 5m;

        var orders = new List<Order>
        {
            new() { TotalAmount = 200m },
            new() { TotalAmount = 100m }
        };

        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(bill);
        _orderRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(orders);
        _taxRepoMock.Setup(r => r.Get(bill.TaxConfigurationId)).ReturnsAsync(ActiveTax());

        await _billService.RecalculateBill(10);

        
        Assert.Multiple(() =>
        {
            Assert.That(bill.FoodTotal,  Is.EqualTo(300m));
            Assert.That(bill.CgstAmount, Is.EqualTo(15m));
            Assert.That(bill.SgstAmount, Is.EqualTo(15m));
            Assert.That(bill.GrandTotal, Is.EqualTo(345m));
        });
        _billRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void RecalculateBill_TaxConfigMissing()
    {
        var bill   = PendingBill();
        var orders = new List<Order> { new() { TotalAmount = 100m } };

        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(bill);
        _orderRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(orders);
        _taxRepoMock.Setup(r => r.Get(bill.TaxConfigurationId)).ReturnsAsync((TaxConfiguration?)null);

        var ex = Assert.ThrowsAsync<Exception>(() =>_billService.RecalculateBill(10));
        Assert.That(ex!.Message, Is.EqualTo("Tax configuration not found"));
    }

    [Test]
    public void RecalculateBill_BillNotFound()
    {
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync((Bill?)null);

        Assert.ThrowsAsync<BillNotFoundException>(() =>_billService.RecalculateBill(10));
        _orderRepoMock.Verify(r => r.GetBySessionId(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task RecalculateBill_KeepsServiceChargeDisabled()
    {
        var bill = PendingBill();
        bill.ServiceChargeAmount = 0m;

        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(bill);
        _orderRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(
            new List<Order> { new() { TotalAmount = 200m } });
        _taxRepoMock.Setup(r => r.Get(bill.TaxConfigurationId)).ReturnsAsync(ActiveTax());

        await _billService.RecalculateBill(10);

        Assert.Multiple(() =>
        {
            Assert.That(bill.ServiceChargeAmount, Is.Zero);
            Assert.That(bill.GrandTotal, Is.EqualTo(220m));
        });
    }



    [Test]
    public async Task GetBills_FiltersOrdersAndPaginates()
    {
        SeedBills();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var result = await _billService.GetBills(" TODAY ", today, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCount, Is.EqualTo(2));
            Assert.That(result.PageNumber, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(1));
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items.Single().BillNumber, Is.EqualTo("SPECIAL-TODAY-2"));
            Assert.That(result.Items.Single().TableNumber, Is.EqualTo("T3"));
        });
    }

    [TestCase(0, 0, 1, 20)]
    [TestCase(1, 500, 1, 100)]
    public async Task GetBills_NormalizesPagination(
        int pageNumber,
        int pageSize,
        int expectedPageNumber,
        int expectedPageSize)
    {
        SeedBills();

        var result = await _billService.GetBills(string.Empty, null, pageNumber, pageSize);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCount, Is.EqualTo(3));
            Assert.That(result.PageNumber, Is.EqualTo(expectedPageNumber));
            Assert.That(result.PageSize, Is.EqualTo(expectedPageSize));
        });
    }

    [Test]
    public void GetBills_FutureDateThrows()
    {
        _billRepoMock
            .Setup(r => r.GetBillsQuery())
            .Returns(new TestAsyncEnumerable<Bill>(Array.Empty<Bill>()));
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var ex = Assert.ThrowsAsync<Exception>(
            () =>_billService.GetBills(string.Empty, futureDate, 1, 20));

        Assert.That(ex!.Message, Is.EqualTo("Future dates not allowed"));
    }

    

    [Test]
    public async Task GetBillDashboardSummary_ReturnsAllBillTotals()
    {
        SeedBills();

        var result = await _billService.GetBillDashboardSummary(null);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalBills, Is.EqualTo(3));
            Assert.That(result.TotalRevenue, Is.EqualTo(430m));
        });
    }

    [Test]
    public async Task GetBillDashboardSummary_FiltersByDate()
    {
        SeedBills();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var result = await _billService.GetBillDashboardSummary(today);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalBills, Is.EqualTo(2));
            Assert.That(result.TotalRevenue, Is.EqualTo(340m));
        });
    }

    

    [Test]
    public async Task GetBillDetails_ReturnsDetails()
    {
        var bill = PendingBill();
        bill.DiningSession = new DiningSession
        {
            WaiterId = 1,
            Waiter   = new User { Name = "John" },
            Table    = new RestaurantTable { TableNumber = "T1" }
        };
        bill.TaxConfiguration = ActiveTax();

        _billRepoMock.Setup(r => r.GetBillDetails(1)).ReturnsAsync(bill);

        var result = await _billService.GetBillDetails(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.BillNumber,  Is.EqualTo(bill.BillNumber));
            Assert.That(result.TableNumber, Is.EqualTo("T1"));
            Assert.That(result.WaiterName,  Is.EqualTo("John"));
            Assert.That(result.GrandTotal,  Is.EqualTo(bill.GrandTotal));
        });
    }

    [Test]
    public void GetBillDetails_BillNotFound()
    {
        _billRepoMock.Setup(r => r.GetBillDetails(99)).ReturnsAsync((Bill?)null);

        Assert.ThrowsAsync<BillNotFoundException>(() =>_billService.GetBillDetails(99));
    }
}

internal sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) =>
        new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) =>
        inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(
        Expression expression,
        CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments().Single();
        var executionResult = typeof(IQueryProvider)
            .GetMethods()
            .Single(method =>
                method.Name == nameof(IQueryProvider.Execute) &&
                method.IsGenericMethod)
            .MakeGenericMethod(resultType)
            .Invoke(inner, new object[] { expression });

        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal sealed class TestAsyncEnumerable<T> :
    EnumerableQuery<T>,
    IAsyncEnumerable<T>,
    IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    {
    }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    {
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;

    public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return ValueTask.CompletedTask;
    }
}
