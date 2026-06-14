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
}
