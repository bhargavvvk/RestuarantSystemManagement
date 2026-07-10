using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface IBillService
{
    Task<BillResponseDto> GetBill(int sessionId);
    Task<BillResponseDto> MarkBillAsPaid(int sessionId, PaymentMethod paymentMethod);
    ICollection<LookupDto> GetPaymentMethods();
    Task<BillResponseDto> UpdateServiceCharge(int tableId,bool includeServiceCharge);
    Task RecalculateBill(int sessionId);
    Task<PagedResponseDto<BillRegistryDto>>GetBills(string search,DateOnly? date,int pageNumber,int pageSize);
    Task<BillDashboardSummaryDto>GetBillDashboardSummary(DateOnly? date);
    Task<BillDetailsDto> GetBillDetails(int billId);
    Task<SplitBillResponseDto> GetSplitBill(int sessionId);
    Task SaveCustomSplits(int sessionId, string customSplitsJson);
    /// <summary>Returns a bill summary scoped to a specific table within a group-order session.</summary>
    Task<TableBillDto> GetTableBill(int sessionId, int tableId);
}
