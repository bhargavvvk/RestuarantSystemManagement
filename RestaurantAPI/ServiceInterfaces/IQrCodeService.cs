namespace RestaurantAPI.ServiceInterfaces;

public interface IQrCodeService
{
    Task<byte[]> GenerateTableQr(int tableId);
}
