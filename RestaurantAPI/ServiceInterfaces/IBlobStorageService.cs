namespace RestaurantAPI.ServiceInterfaces;

public interface IBlobStorageService
{
    Task<string> UploadMenuImageAsync(IFormFile file);
}
