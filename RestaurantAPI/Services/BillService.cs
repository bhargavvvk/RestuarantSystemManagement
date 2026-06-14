using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.Repositories;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class BillService : IBillService
{
    private readonly IDiningSessionRepository _diningSessionRepository;
    private readonly IBillRepository _billRepository;
    private readonly ILogger<BillService> _logger;
    private readonly IMapper _mapper;
    private readonly ITaxConfigurationRepository _taxConfigurationRepository;
    private readonly IAuditService _auditService;
    private readonly IOrderRepository _orderRepository;
    public BillService(IDiningSessionRepository diningSessionRepository, ILogger<BillService> logger, IBillRepository billRepository, IMapper mapper,
    ITaxConfigurationRepository taxConfigurationRepository,IAuditService auditService, IOrderRepository orderRepository)
    {
        _diningSessionRepository = diningSessionRepository;
        _logger = logger;
        _billRepository = billRepository;
        _mapper = mapper;
        _taxConfigurationRepository = taxConfigurationRepository;
        _auditService = auditService;
        _orderRepository=orderRepository;

    }
    public async Task<BillResponseDto> GetBill(int sessionId)
    {
        var session =await _diningSessionRepository.Get(sessionId);

        if(session == null)
        {
            throw new SessionNotFoundException();
        }
        var bill = await _billRepository.GetBySessionId(sessionId);
        _logger.LogInformation("Bill is getting retrieved");
        if(bill == null)
        {
            throw new BillNotFoundException();
        }
        return _mapper.Map<BillResponseDto>(bill);
    }
    public async Task<BillResponseDto> MarkBillAsPaid(int sessionId,PaymentMethod paymentMethod)
    {
        var session = await _diningSessionRepository.Get(sessionId);

        if (session == null)
        {
            throw new SessionNotFoundException();
        }

        var bill = await _billRepository.GetBySessionId(sessionId);

        if (bill == null)
        {
            throw new BillNotFoundException();
        }

        if (bill.PaymentStatus == PaymentStatus.Paid)
        {
            throw new UnauthorizedAccessException("Bill is already paid.");
        }

        bill.PaymentStatus = PaymentStatus.Paid;

        bill.PaymentMethod = paymentMethod;

        bill.PaidAt = DateTime.Now;

        await _billRepository.Update(bill.Id,bill);

        await _billRepository.SaveChangesAsync();

        return _mapper.Map<BillResponseDto>(bill);
    }
    public ICollection<LookupDto>GetPaymentMethods()
    {
        return Enum.GetValues<PaymentMethod>()
            .Select(pm => new LookupDto
            {
                Value = (int)pm,
                Name = pm.ToString()
            })
            .ToList();
    }
    public async Task<BillResponseDto> UpdateServiceCharge(int sessionId,bool includeServiceCharge)
    {
        var bill =await _billRepository.GetBySessionId(sessionId);
        if (bill == null)
        {
            throw new BillNotFoundException();
        }
        if (bill?.PaymentStatus == PaymentStatus.Paid)
        {
            throw new Exception("Bill Already Paid");
        }
        var oldValues = new
        {
            bill!.ServiceChargeAmount,
            bill.GrandTotal
        };
        if (!includeServiceCharge)
        {
            bill.GrandTotal -= bill.ServiceChargeAmount;
            bill.ServiceChargeAmount = 0;
        }
        else
        {
            if (bill.ServiceChargeAmount == 0)
            {
                var taxConfiguration =await _taxConfigurationRepository.Get(bill.TaxConfigurationId);

                if (taxConfiguration == null)
                {
                    throw new Exception(
                        "Tax configuration not found");
                }

                var serviceCharge =bill.FoodTotal *taxConfiguration.ServiceChargePercentage / 100;
                bill.ServiceChargeAmount =serviceCharge;
                bill.GrandTotal += serviceCharge;
            }
        }

        await _auditService.LogAsync(nameof(Bill),bill.Id.ToString(),AuditAction.Updated,oldValues,
            new
            {
                bill.ServiceChargeAmount,
                bill.GrandTotal
            },
            includeServiceCharge
                ? "Service charge enabled"
                : "Service charge disabled");

        await _billRepository.SaveChangesAsync();

        return _mapper.Map<BillResponseDto>(bill);
    }
    public async Task RecalculateBill(int sessionId)
    {
        var bill =await _billRepository.GetBySessionId(sessionId);
        if (bill == null)
        {
            throw new BillNotFoundException();
        }
        var orders =await _orderRepository.GetBySessionId(sessionId);
        decimal foodTotal =orders.Sum(o => o.TotalAmount);
        bill.FoodTotal = foodTotal;
        var taxConfiguration =await _taxConfigurationRepository.Get(bill.TaxConfigurationId);
        if (taxConfiguration == null)
        {
            throw new Exception("Tax configuration not found");
        }
        bill.CgstAmount =foodTotal *taxConfiguration.CgstPercentage / 100;
        bill.SgstAmount =foodTotal *taxConfiguration.SgstPercentage / 100;
        bool serviceChargeEnabled =bill.ServiceChargeAmount > 0;
        bill.ServiceChargeAmount =
            serviceChargeEnabled
                ? foodTotal *
                    taxConfiguration.ServiceChargePercentage / 100
                : 0;

        bill.GrandTotal =foodTotal +bill.CgstAmount +bill.SgstAmount +bill.ServiceChargeAmount;
        await _billRepository.SaveChangesAsync();
    }
    public async Task<PagedResponseDto<BillRegistryDto>>GetBills(string search,DateOnly? date,int pageNumber,int pageSize)
    {
        var query =_billRepository.GetBillsQuery();if(pageNumber < 1)
        {
            pageNumber = 1;
        }
        if(date > DateOnly.FromDateTime(DateTime.Now))
        {
            throw new Exception("Future dates not allowed");
        }
        if(pageSize <= 0)
        {
            pageSize = 20;
        }
        if (date.HasValue)
        {
            query = query.Where(b =>DateOnly.FromDateTime(b.GeneratedAt) == date.Value);
        }
        pageSize = Math.Min(pageSize, 100);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch =search.Trim();
            query = query.Where(b =>b.BillNumber.Contains(normalizedSearch));
        }
        query = query.OrderByDescending(b => b.GeneratedAt);
        var totalCount =await query.CountAsync();
        var bills = await query.Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BillRegistryDto
            {
                BillId = b.Id,
                BillNumber = b.BillNumber,
                TableNumber =
                    b.DiningSession!.Table!
                        .TableNumber,
                GeneratedAt = b.GeneratedAt,
                GrandTotal = b.GrandTotal
            })
            .ToListAsync();

        return new PagedResponseDto<BillRegistryDto>
        {
            Items = bills,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
    public async Task<BillDashboardSummaryDto>GetBillDashboardSummary(DateOnly? date)
    {
        var query =_billRepository.GetBillsQuery();
        if (date.HasValue)
        {
            query = query.Where(b =>DateOnly.FromDateTime(b.GeneratedAt) == date.Value);
        }
        return new BillDashboardSummaryDto
        {
            TotalBills =await query.CountAsync(),
            TotalRevenue =await query.SumAsync(b => b.GrandTotal)
        };
    }
    public async Task<BillDetailsDto>GetBillDetails(int billId)
    {
        var bill =await _billRepository.GetBillDetails(billId);
        if (bill == null)
        {
            throw new BillNotFoundException();
        }
        return new BillDetailsDto
        {
            BillNumber = bill.BillNumber,
            GeneratedAt = bill.GeneratedAt,
            TableNumber =bill.DiningSession!.Table!.TableNumber,
            WaiterId =bill.DiningSession.WaiterId,
            WaiterName =bill.DiningSession.Waiter!.Name,
            PaymentMethod =bill!.PaymentMethod,
            PaymentStatus =bill.PaymentStatus,
            FoodTotal =bill.FoodTotal,
            CgstPercentage =bill.TaxConfiguration!.CgstPercentage,
            CgstAmount =bill.CgstAmount,
            SgstPercentage =bill.TaxConfiguration!.SgstPercentage,
            SgstAmount =bill.SgstAmount,
            ServiceChargePercentage =bill.TaxConfiguration.ServiceChargePercentage,
            ServiceChargeAmount =bill.ServiceChargeAmount,
            GrandTotal =bill.GrandTotal
        };
    }
}
