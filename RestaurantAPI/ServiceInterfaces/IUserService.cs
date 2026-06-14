using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface IUserService
{
    Task<SystemUsersCreatedResponseDto> CreateSystemUsersAsync(CreateSystemUsersRequestDto request);
    Task<WaiterResponseDto> CreateWaiterAsync(CreateWaiterRequestDto request);
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<ProfileResponseDto> GetProfile();
    Task UpdateProfile(UpdateProfileDto request);
    Task ChangePassword(ChangePasswordDto request);
}
