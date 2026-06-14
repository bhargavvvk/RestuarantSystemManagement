using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly RestaurantContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public UserService(IUserRepository userRepository, IEncryptionService encryptionService, ITokenService tokenService, IAuditService auditService, RestaurantContext context,
    IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _encryptionService = encryptionService;
        _tokenService = tokenService;
        _auditService = auditService;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }
   public async Task<SystemUsersCreatedResponseDto> CreateSystemUsersAsync(CreateSystemUsersRequestDto request)
    {
        var adminExists = await _userRepository.GetByUsernameAsync("admin");

        var kitchenExists = await _userRepository.GetByUsernameAsync("kitchen");

        if (adminExists != null || kitchenExists != null)
            throw new Exception("System users already exist.");

        var admin = new User
        {
            Username = "admin",
            Name = "System Admin",
            Role = UserRole.Admin,
            IsActive = true,
            EncryptedMobileNumber =_encryptionService.Encrypt(request.AdminMobileNumber),
            MobileNumberHash =_encryptionService.GenerateHash(request.AdminMobileNumber)};
        using (var hmac = new HMACSHA256())
        {
            admin.PasswordHash =hmac.ComputeHash(Encoding.UTF8.GetBytes("admin1234"));
            admin.HashKey = hmac.Key;
        }
        var kitchen = new User
        {
            Username = "kitchen",
            Name = "Kitchen Staff",
            Role = UserRole.KitchenStaff,
            IsActive = true,
            EncryptedMobileNumber =_encryptionService.Encrypt(request.KitchenMobileNumber),
            MobileNumberHash =_encryptionService.GenerateHash(request.KitchenMobileNumber)
        };
        using (var hmac = new HMACSHA256())
        {
            kitchen.PasswordHash =hmac.ComputeHash(Encoding.UTF8.GetBytes("kitchen1234"));
            kitchen.HashKey = hmac.Key;
        }
        await _userRepository.Create(admin);
        await _userRepository.Create(kitchen);
        await _userRepository.SaveChangesAsync();
        return new SystemUsersCreatedResponseDto
        {
            Message = "System users created successfully."
        };
    }
    public async Task<WaiterResponseDto> CreateWaiterAsync(CreateWaiterRequestDto request)
    {
        var existingUser =await _userRepository.GetByUsernameAsync(request.Username);
        if (existingUser != null)
            throw new DuplicateEntityException($"Username {request.Username} already exists.");
         if (!Regex.IsMatch(request.MobileNumber, @"^\d{10}$"))
        {
            throw new ValidationException("Phone number must contain exactly 10 digits.");
        }
        var mobileHash =_encryptionService.GenerateHash(request.MobileNumber);

        var existingMobile =await _userRepository.GetByMobileHashAsync(mobileHash);

        if (existingMobile != null)
            throw new DuplicateEntityException("Mobile number already exists.");

        var waiter = new User
        {
            Username = request.Username,
            Name = request.Name.Trim(),
            Role = UserRole.Waiter,
            IsActive = true,
            IsDeleted = false,
            EncryptedMobileNumber =_encryptionService.Encrypt(request.MobileNumber),
            MobileNumberHash = mobileHash
        };
        var generatedPassword =request.Username[..Math.Min(4,request.Username.Length)] +"1234";
        using var hmac =new HMACSHA256();
        waiter.PasswordHash =hmac.ComputeHash(Encoding.UTF8.GetBytes(generatedPassword));
        waiter.HashKey =hmac.Key;
        await using var transaction =await _context.Database.BeginTransactionAsync();
        try
        {
            var createdWaiter =await _userRepository.Create(waiter);
            await _userRepository.SaveChangesAsync();
            await _auditService.LogAsync(
                nameof(User),
                createdWaiter.Id.ToString(),
                AuditAction.Created,
                null,
                new
                {
                    createdWaiter.Username,
                    createdWaiter.Name,
                    createdWaiter.Role,
                    createdWaiter.IsActive
                },
                $"Waiter {createdWaiter.Name} created");

            await _userRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            return new WaiterResponseDto
            {
                Id = createdWaiter.Id,
                Username =createdWaiter.Username,
                Name =createdWaiter.Name
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        if (string.IsNullOrEmpty(request.Username) ||
            string.IsNullOrEmpty(request.Password))
        {
            throw new ValidationException("Username and password are required.");
        }
        var user =await _userRepository.GetByUsernameAsync(request.Username);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid username or password");

        using var hmac =new HMACSHA256(user.HashKey);
        var computedHash =hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Password));
        if (!computedHash.SequenceEqual(user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password");
        }
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is inactive");
        }
        var tokenRequest = new EmployeeTokenRequest
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role.ToString()
        };
        var token =_tokenService.CreateEmployeeToken(tokenRequest);
        return new LoginResponseDto
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            Name = user.Name,
            Role = user.Role.ToString()
        };
    }
    public async Task<ProfileResponseDto>GetProfile()
    {
        var userId =int.Parse(_httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user =await _userRepository.Get(userId);
        if (user == null)
        {
            throw new UserNotFoundException();
        }
        return new ProfileResponseDto
        {
            Id = user.Id,
            Username =user.Username,
            Name =user.Name,
            MobileNumber =_encryptionService.Decrypt(user.EncryptedMobileNumber),
            Role =user.Role,
            IsActive =user.IsActive
        };
    }
    public async Task UpdateProfile(UpdateProfileDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new Exception("Name is required");
        }

        if (string.IsNullOrWhiteSpace(request.MobileNumber))
        {
            throw new Exception("Mobile number is required");
        }
         if (!Regex.IsMatch(request.MobileNumber, @"^\d{10}$"))
        {
            throw new ValidationException("Phone number must contain exactly 10 digits.");
        }

        var userId =int.Parse(_httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user =await _userRepository.Get(userId);

        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var mobileHash =_encryptionService.GenerateHash(request.MobileNumber);

        var existingUser =await _userRepository.GetByMobileHashAsync(mobileHash);

        if (existingUser != null && existingUser.Id != user.Id)
        {
            throw new DuplicateEntityException("Mobile number already exists");
        }
        var oldValues = new
        {
            user.Name,MobileNumber =_encryptionService.Decrypt(user.EncryptedMobileNumber)
        };
        user.Name =request.Name.Trim();
        user.EncryptedMobileNumber =_encryptionService.Encrypt(request.MobileNumber);
        user.MobileNumberHash =mobileHash;
        await using var transaction =await _context.Database.BeginTransactionAsync();
        try
        {
            await _userRepository.SaveChangesAsync();
            await _auditService.LogAsync(nameof(User),user.Id.ToString(),AuditAction.Updated,oldValues,
                new
                {
                    user.Name,
                    request.MobileNumber
                },
                "Profile updated");
            await _userRepository.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task ChangePassword(ChangePasswordDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            throw new Exception("Current password is required");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new Exception("New password is required");
        }

        if (request.NewPassword.Length < 6)
        {
            throw new Exception("Password must be at least 6 characters");
        }

        var userId =int.Parse(_httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user =await _userRepository.Get(userId);

        if (user == null)
        {
            throw new UserNotFoundException();
        }

        using var verifyHmac =new HMACSHA256(user.HashKey);

        var currentPasswordHash =verifyHmac.ComputeHash(Encoding.UTF8.GetBytes(request.CurrentPassword));
        if (!currentPasswordHash.SequenceEqual(user.PasswordHash))
        {
            throw new Exception("Current password is incorrect");
        }

        using var newHmac =new HMACSHA256();

        user.PasswordHash =newHmac.ComputeHash(Encoding.UTF8.GetBytes(request.NewPassword));

        user.HashKey =newHmac.Key;

        await using var transaction =await _context.Database.BeginTransactionAsync();

        try
        {
            await _userRepository.SaveChangesAsync();
            await _auditService.LogAsync(
                nameof(User),
                user.Id.ToString(),
                AuditAction.Updated,
                null,
                null,
                "Password changed");

            await _userRepository.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
