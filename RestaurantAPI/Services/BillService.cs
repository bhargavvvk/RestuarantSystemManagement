using AutoMapper;
using Microsoft.AspNetCore.SignalR;
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
    private readonly IHubContext<NotificationHub> _hubContext;

    public BillService(IDiningSessionRepository diningSessionRepository, ILogger<BillService> logger, IBillRepository billRepository, IMapper mapper,
    ITaxConfigurationRepository taxConfigurationRepository,IAuditService auditService, IOrderRepository orderRepository,IHubContext<NotificationHub> hubContext)
    {
        _diningSessionRepository = diningSessionRepository;
        _logger = logger;
        _billRepository = billRepository;
        _mapper = mapper;
        _taxConfigurationRepository = taxConfigurationRepository;
        _auditService = auditService;
        _orderRepository = orderRepository;
        _hubContext = hubContext;
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
        var dto = _mapper.Map<BillResponseDto>(bill);
        dto.CustomSplitsJson = bill.CustomSplitsJson;
        return dto;
    }
    public async Task<BillResponseDto> MarkBillAsPaid(int sessionId,PaymentMethod paymentMethod)
    {
        _logger.LogInformation("Marking bill as paid for session {SessionId} with payment method {PaymentMethod}", sessionId, paymentMethod);
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

        ClearCustomSplits(sessionId);

        _logger.LogInformation("Bill {BillId} marked as paid for session {SessionId}", bill.Id, sessionId);
        await _hubContext.Clients.Group($"session-{sessionId}").SendAsync("BillStatusChanged");
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
        _logger.LogInformation("Updating service charge for session {SessionId} (include={IncludeServiceCharge})", sessionId, includeServiceCharge);
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
        await _hubContext.Clients.Group($"session-{sessionId}").SendAsync("BillStatusChanged");
        _logger.LogInformation("Service charge updated for session {SessionId}. Grand total: {GrandTotal}", sessionId, bill.GrandTotal);
        return _mapper.Map<BillResponseDto>(bill);
    }
    public async Task RecalculateBill(int sessionId)
    {
        _logger.LogInformation("Recalculating bill for session {SessionId}", sessionId);
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
        _logger.LogInformation("Bill recalculated for session {SessionId}. Grand total: {GrandTotal}", sessionId, bill.GrandTotal);
    }
    public async Task<PagedResponseDto<BillRegistryDto>>GetBills(string search,DateOnly? date,int pageNumber,int pageSize)
    {
        _logger.LogInformation("Fetching bills registry (search={Search}, date={Date}, page={Page})", search, date, pageNumber);
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
        var rawBills = await query.Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var bills = rawBills.Select(b => {
            var tables = new List<string> { b.DiningSession!.Table!.TableNumber };
            if (b.DiningSession.DiningSessionTables != null && b.DiningSession.DiningSessionTables.Any())
            {
                tables.AddRange(b.DiningSession.DiningSessionTables.Select(dst => dst.Table!.TableNumber));
            }
            
            return new BillRegistryDto
            {
                BillId = b.Id,
                BillNumber = b.BillNumber,
                TableNumber = string.Join(", ", tables.Distinct()),
                GeneratedAt = b.GeneratedAt,
                GrandTotal = b.GrandTotal
            };
        }).ToList();

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
        _logger.LogInformation("Fetching bill dashboard summary for date {Date}", date);
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
        _logger.LogInformation("Fetching bill details for bill {BillId}", billId);
        var bill =await _billRepository.GetBillDetails(billId);
        if (bill == null)
        {
            throw new BillNotFoundException();
        }
        var tables = new List<string> { bill.DiningSession!.Table!.TableNumber };
        if (bill.DiningSession.DiningSessionTables != null && bill.DiningSession.DiningSessionTables.Any())
        {
            tables.AddRange(bill.DiningSession.DiningSessionTables.Select(dst => dst.Table!.TableNumber));
        }

        return new BillDetailsDto
        {
            BillNumber = bill.BillNumber,
            GeneratedAt = bill.GeneratedAt,
            TableNumber = string.Join(", ", tables.Distinct()),
            WaiterId =bill.DiningSession.WaiterId,
            WaiterName =bill.DiningSession.Waiter!.Name,
            PaymentMethod = bill.PaymentMethod?.ToString(),
            PaymentStatus = bill.PaymentStatus.ToString(),
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

    public async Task<SplitBillResponseDto> GetSplitBill(int sessionId)
    {
        _logger.LogInformation("Calculating split bill for session {SessionId}", sessionId);
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

        var taxConfiguration = await _taxConfigurationRepository.Get(bill.TaxConfigurationId);
        if (taxConfiguration == null)
        {
            throw new Exception("Tax configuration not found");
        }

        var orders = await _orderRepository.GetBySessionId(sessionId);

        var response = new SplitBillResponseDto
        {
            FoodTotal = bill.FoodTotal,
            CgstPercentage = taxConfiguration.CgstPercentage,
            SgstPercentage = taxConfiguration.SgstPercentage,
            ServiceChargePercentage = taxConfiguration.ServiceChargePercentage,
            GrandTotal = bill.GrandTotal,
            CustomSplitsJson = bill.CustomSplitsJson
        };

        bool includeServiceCharge = bill.ServiceChargeAmount > 0;

        foreach (var order in orders)
        {
            var orderFoodTotal = order.TotalAmount;
            var orderCgst = orderFoodTotal * taxConfiguration.CgstPercentage / 100;
            var orderSgst = orderFoodTotal * taxConfiguration.SgstPercentage / 100;
            var orderServiceCharge = includeServiceCharge ? (orderFoodTotal * taxConfiguration.ServiceChargePercentage / 100) : 0;
            var orderGrandTotal = orderFoodTotal + orderCgst + orderSgst + orderServiceCharge;

            var orderSplit = new OrderSplitOptionDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                FoodTotal = orderFoodTotal,
                CgstAmount = orderCgst,
                SgstAmount = orderSgst,
                ServiceChargeAmount = orderServiceCharge,
                GrandTotal = orderGrandTotal,
                Items = order.OrderItems?.Select(oi => new OrderItemResponseDto
                {
                    OrderItemId = oi.Id,
                    ItemName = oi.ItemName,
                    ItemPrice = oi.ItemPrice,
                    Quantity = oi.Quantity,
                    Status = oi.Status
                }).ToList() ?? new List<OrderItemResponseDto>()
            };

            response.OrderSplits.Add(orderSplit);
        }

        if (orders != null)
        {
            foreach (var order in orders)
            {
                if (order.OrderItems != null)
                {
                    foreach (var item in order.OrderItems)
                    {
                        var itemFoodTotal = item.ItemPrice * item.Quantity;
                        var itemCgst = itemFoodTotal * taxConfiguration.CgstPercentage / 100;
                        var itemSgst = itemFoodTotal * taxConfiguration.SgstPercentage / 100;
                        var itemServiceCharge = includeServiceCharge ? (itemFoodTotal * taxConfiguration.ServiceChargePercentage / 100) : 0;
                        var itemGrandTotal = itemFoodTotal + itemCgst + itemSgst + itemServiceCharge;

                        var itemSplit = new ItemSplitOptionDto
                        {
                            OrderItemId = item.Id,
                            ItemName = item.ItemName,
                            Quantity = item.Quantity,
                            ItemPrice = item.ItemPrice,
                            FoodTotal = itemFoodTotal,
                            CgstAmount = itemCgst,
                            SgstAmount = itemSgst,
                            ServiceChargeAmount = itemServiceCharge,
                            GrandTotal = itemGrandTotal
                        };

                        response.ItemSplits.Add(itemSplit);
                    }
                }
            }
        }

        // ---- Table-wise splits ----
        // Collect all order items across all orders, flattened
        var allOrderItems = orders
            .Where(o => o.OrderItems != null)
            .SelectMany(o => o.OrderItems!)
            .ToList();

        // Build a lookup from tableId -> table number
        var tableNumberLookup = new Dictionary<int, string>();
        // Primary table
        if (session.Table != null)
            tableNumberLookup[session.TableId] = session.Table.TableNumber;
        // Linked tables
        if (session.DiningSessionTables != null)
        {
            foreach (var dst in session.DiningSessionTables)
            {
                if (dst.Table != null)
                    tableNumberLookup[dst.TableId] = dst.Table.TableNumber;
            }
        }

        // Group by TableId (null goes to primary table)
        var grouped = allOrderItems
            .GroupBy(oi => oi.TableId ?? session.TableId);

        foreach (var group in grouped)
        {
            var tblId = group.Key;
            var tblNumber = tableNumberLookup.TryGetValue(tblId, out var tn) ? tn : $"Table {tblId}";
            var tblFoodTotal = group.Sum(oi => oi.ItemPrice * oi.Quantity);
            var tblCgst = tblFoodTotal * taxConfiguration.CgstPercentage / 100;
            var tblSgst = tblFoodTotal * taxConfiguration.SgstPercentage / 100;
            var tblServiceCharge = includeServiceCharge ? (tblFoodTotal * taxConfiguration.ServiceChargePercentage / 100) : 0;
            var tblTotal = tblFoodTotal + tblCgst + tblSgst + tblServiceCharge;

            var tableSplit = new TableSplitOptionDto
            {
                TableId = tblId,
                TableNumber = tblNumber,
                FoodSubtotal = tblFoodTotal,
                CgstAmount = tblCgst,
                SgstAmount = tblSgst,
                ServiceChargeAmount = tblServiceCharge,
                TableTotal = tblTotal,
                Items = group.Select(oi => new TableSplitItemDto
                {
                    ItemName = oi.ItemName,
                    Quantity = oi.Quantity,
                    ItemPrice = oi.ItemPrice,
                    LineTotal = oi.ItemPrice * oi.Quantity
                }).ToList()
            };

            response.TableSplits.Add(tableSplit);
        }

        return response;
    }

    public async Task SaveCustomSplits(int sessionId, string customSplitsJson)
    {
        _logger.LogInformation("Saving custom splits for session {SessionId}", sessionId);
        var bill = await _billRepository.GetBySessionId(sessionId);
        if (bill == null) throw new BillNotFoundException();

        bill.CustomSplitsJson = customSplitsJson;
        await _billRepository.Update(bill.Id, bill);
        await _billRepository.SaveChangesAsync();

        await _hubContext.Clients.Group($"session-{sessionId}").SendAsync("BillStatusChanged");
    }

    public void ClearCustomSplits(int sessionId)
    {
        // No-op or we can clear DB if necessary, but typically closing session clears it or we just don't care.
    }

    public async Task<TableBillDto> GetTableBill(int sessionId, int tableId)
    {
        _logger.LogInformation("Getting table bill for session {SessionId}, table {TableId}", sessionId, tableId);
        var session = await _diningSessionRepository.Get(sessionId);
        if (session == null) throw new SessionNotFoundException();

        var bill = await _billRepository.GetBySessionId(sessionId);
        if (bill == null) throw new BillNotFoundException();

        var taxConfig = bill.TaxConfiguration ?? await _taxConfigurationRepository.GetActiveConfiguration()
            ?? throw new Exception("Tax configuration not found");

        // Determine linked tables count (1 = no group order, >1 = group order)
        var linkedTableIds = session.DiningSessionTables?.Select(dst => dst.TableId).ToList() ?? new List<int>();
        var isGroupOrder = linkedTableIds.Count > 0;

        // Build table number lookup
        var tableNumberLookup = new Dictionary<int, string>();
        if (session.Table != null)
            tableNumberLookup[session.TableId] = session.Table.TableNumber;
        foreach (var dst in session.DiningSessionTables ?? Enumerable.Empty<RestaurantAPI.Models.DiningSessionTable>())
        {
            if (dst.Table != null)
                tableNumberLookup[dst.TableId] = dst.Table.TableNumber;
        }

        var tableNumber = tableNumberLookup.TryGetValue(tableId, out var tn) ? tn : $"Table {tableId}";

        var orders = await _orderRepository.GetBySessionId(sessionId);
        var includeServiceCharge = taxConfig.ServiceChargePercentage > 0;

        // Filter to active (non-cancelled) items for this specific table
        // Items with null TableId fall back to the session's primary table
        var myItems = orders
            .Where(o => o.OrderItems != null)
            .SelectMany(o => o.OrderItems!)
            .Where(oi => oi.Status != OrderItemStatus.Cancelled)
            .Where(oi => (oi.TableId ?? session.TableId) == tableId)
            .ToList();

        var myFoodTotal = myItems.Sum(oi => oi.ItemPrice * oi.Quantity);
        var myCgst = myFoodTotal * taxConfig.CgstPercentage / 100;
        var mySgst = myFoodTotal * taxConfig.SgstPercentage / 100;
        var myServiceCharge = includeServiceCharge ? myFoodTotal * taxConfig.ServiceChargePercentage / 100 : 0;
        var myTotal = myFoodTotal + myCgst + mySgst + myServiceCharge;

        return new TableBillDto
        {
            BillNumber = bill.BillNumber,
            TableId = tableId,
            TableNumber = tableNumber,
            MyTableFoodTotal = myFoodTotal,
            CgstPercentage = taxConfig.CgstPercentage,
            CgstAmount = myCgst,
            SgstPercentage = taxConfig.SgstPercentage,
            SgstAmount = mySgst,
            ServiceChargePercentage = taxConfig.ServiceChargePercentage,
            ServiceChargeAmount = myServiceCharge,
            MyTableTotal = myTotal,
            SessionGrandTotal = bill.GrandTotal,
            IsGroupOrder = isGroupOrder,
            LinkedTablesCount = linkedTableIds.Count + 1, // +1 for primary table
            PaymentStatus = (int)bill.PaymentStatus,
            GeneratedAt = bill.GeneratedAt,
            CustomSplitsJson = bill.CustomSplitsJson
        };
    }
}
