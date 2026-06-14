using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;


public class UserServiceTests
{
    private Mock<IUserRepository>       _userRepoMock;  
    private Mock<IEncryptionService>    _encryptionMock;
    private Mock<ITokenService>         _tokenMock;
    private Mock<IAuditService>         _auditMock;
    private Mock<IHttpContextAccessor>  _httpContextAccessorMock;
    private RestaurantContext _context;
    private UserService _userService;
    [SetUp]
    public void SetUp()
    {
        _userRepoMock   = new Mock<IUserRepository>();
        _encryptionMock = new Mock<IEncryptionService>();
        _tokenMock      = new Mock<ITokenService>();
        _auditMock      = new Mock<IAuditService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        var options = new DbContextOptionsBuilder<RestaurantContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _context = new RestaurantContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _encryptionMock
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns<string>(v => $"ENC_{v}");

        _encryptionMock
            .Setup(e => e.GenerateHash(It.IsAny<string>()))
            .Returns<string>(v => $"HASH_{v}");

        _auditMock
            .Setup(a => a.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuditAction>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _userService= new UserService(
            _userRepoMock.Object,  
            _encryptionMock.Object, 
            _tokenMock.Object,    
            _auditMock.Object,     
            _context,
            _httpContextAccessorMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Test]
    public async Task CreateSystemUsers_success()
    {
        _userRepoMock.Setup(r => r.GetByUsernameAsync("admin"))
                     .ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByUsernameAsync("kitchen"))
                     .ReturnsAsync((User?)null);

        _userRepoMock.Setup(r => r.Create(It.IsAny<User>()))
                     .Returns<User>(u => Task.FromResult(u));
        _userRepoMock.Setup(r => r.SaveChangesAsync())
                     .ReturnsAsync(0);

        var request = new CreateSystemUsersRequestDto
        {
            AdminMobileNumber   = "9000000001",
            KitchenMobileNumber = "9000000002"
        };

        var result = await _userService.CreateSystemUsersAsync(request);

        // Assert
        Assert.That(result.Message, Is.EqualTo("System users created successfully."));

      
        _userRepoMock.Verify(r => r.Create(It.Is<User>(u => u.Username == "admin")),   Times.Once);
        _userRepoMock.Verify(r => r.Create(It.Is<User>(u => u.Username == "kitchen")), Times.Once);
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }


    [Test]
    public async Task CreateWaiterAsync_Success()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByUsernameAsync("john"))
                     .ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByMobileHashAsync("HASH_9111111111"))
                     .ReturnsAsync((User?)null);

       
        _userRepoMock
            .Setup(r => r.Create(It.IsAny<User>()))
            .Returns<User>(u =>
            {
                u.Id = 10; 
                return Task.FromResult(u);
            });

        _userRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        var request = new CreateWaiterRequestDto
        {
            Username     = "john",
            Name         = "John Doe",
            MobileNumber = "9111111111"
        };


        var result = await _userService.CreateWaiterAsync(request);

       
        Assert.That(result.Id,       Is.EqualTo(10));
        Assert.That(result.Username, Is.EqualTo("john"));
        Assert.That(result.Name,     Is.EqualTo("John Doe"));

        _userRepoMock.Verify(r => r.Create(It.IsAny<User>()), Times.Once);
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2)); 

        _auditMock.Verify(a => a.LogAsync(
            nameof(User),
            "10",
            AuditAction.Created,
            null,
            It.IsAny<object>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Test]
    public void CreateWaiterAsync_UsernameExists()
    {
      
        _userRepoMock.Setup(r => r.GetByUsernameAsync("john"))
                     .ReturnsAsync(new User { Username = "john" });

        var request = new CreateWaiterRequestDto
        {
            Username     = "john",
            Name         = "John Doe",
            MobileNumber = "9111111111"
        };

        var ex = Assert.ThrowsAsync<DuplicateEntityException>(() => _userService.CreateWaiterAsync(request));
        Assert.That(ex!.Message, Does.Contain("john"));
    }

    [Test]
    public void CreateWaiterAsync_MobileExists()
    {
       
        _userRepoMock.Setup(r => r.GetByUsernameAsync("john"))
                     .ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByMobileHashAsync("HASH_9111111111"))
                     .ReturnsAsync(new User { MobileNumberHash = "HASH_9111111111" });

        var request = new CreateWaiterRequestDto
        {
            Username     = "john",
            Name         = "John Doe",
            MobileNumber = "9111111111"
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<DuplicateEntityException>(() => _userService.CreateWaiterAsync(request));
        Assert.That(ex!.Message, Does.Contain("Mobile number already exists."));
    }

  

    [Test]
    public async Task LoginAsync_Success()
    {
      
        const string password = "admin1234";
        byte[] hashKey, passwordHash;

        using (var hmac = new HMACSHA256())
        {
            hashKey      = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        var user = new User
        {
            Id           = 1,
            Username     = "admin",
            Name         = "System Admin",
            Role         = UserRole.Admin,
            IsActive     = true,
            PasswordHash = passwordHash,
            HashKey      = hashKey
        };

        _userRepoMock.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);
        _tokenMock.Setup(t => t.CreateEmployeeToken(It.IsAny<EmployeeTokenRequest>()))
                  .Returns("jwt_token_value");

        var request = new LoginRequestDto { Username = "admin", Password = password };

        // Act
        var result = await _userService.LoginAsync(request);

        // Assert
        Assert.That(result.Token,    Is.EqualTo("jwt_token_value"));
        Assert.That(result.UserId,   Is.EqualTo(1));
        Assert.That(result.Username, Is.EqualTo("admin"));
        Assert.That(result.Role,     Is.EqualTo("Admin"));

        _tokenMock.Verify(
            t => t.CreateEmployeeToken(It.IsAny<EmployeeTokenRequest>()),
            Times.Once);
    }

    [Test]
    public void LoginAsync_EmptyCredentials()
    {
      
        var request = new LoginRequestDto { Username = "", Password = "admin1234" };

        
        var ex = Assert.ThrowsAsync<ValidationException>(() => _userService.LoginAsync(request));
        Assert.That(ex!.Message, Does.Contain("required"));
    }

    [Test]
    public void LoginAsync_EmptyPassword_ThrowsValidationException()
    {
        var request = new LoginRequestDto { Username = "admin", Password = "" };

        var ex = Assert.ThrowsAsync<ValidationException>(() => _userService.LoginAsync(request));
        Assert.That(ex!.Message, Does.Contain("required"));
    }

    [Test]
    public void LoginAsync_UserNotFound()
    {
        
        _userRepoMock.Setup(r => r.GetByUsernameAsync("ghost"))
                     .ReturnsAsync((User?)null);

        var request = new LoginRequestDto { Username = "ghost", Password = "somepassword" };

       
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(() => _userService.LoginAsync(request));
        Assert.That(ex!.Message, Is.EqualTo("Invalid username or password"));
    }

    [Test]
    public void LoginAsync_WrongPassword()
    {
      
        byte[] hashKey, passwordHash;

        using (var hmac = new HMACSHA256())
        {
            hashKey      = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes("correctpassword"));
        }

        var user = new User
        {
            Id           = 1,
            Username     = "admin",
            IsActive     = true,
            PasswordHash = passwordHash,
            HashKey      = hashKey
        };

        _userRepoMock.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);

        var request = new LoginRequestDto { Username = "admin", Password = "wrongpassword" };

       
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(() => _userService.LoginAsync(request));
        Assert.That(ex!.Message, Is.EqualTo("Invalid username or password"));
    }

    [Test]
    public void LoginAsync_InactiveUser()
    {
        
        const string password = "admin1234";
        byte[] hashKey, passwordHash;

        using (var hmac = new HMACSHA256())
        {
            hashKey      = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        var user = new User
        {
            Id           = 1,
            Username     = "admin",
            IsActive     = false, 
            PasswordHash = passwordHash,
            HashKey      = hashKey
        };

        _userRepoMock.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);

        var request = new LoginRequestDto { Username = "admin", Password = password };

       
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(() => _userService.LoginAsync(request));
        Assert.That(ex!.Message, Is.EqualTo("User account is inactive"));

      
        _tokenMock.Verify(t => t.CreateEmployeeToken(It.IsAny<EmployeeTokenRequest>()), Times.Never);
    }

    private void SetUpHttpContext(int userId)
    {
        var claims    = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity  = new System.Security.Claims.ClaimsIdentity(claims);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.User).Returns(principal);

        _httpContextAccessorMock
            .Setup(a => a.HttpContext)
            .Returns(httpContextMock.Object);
    }

    private User BuildUser(int id = 1, string password = "password123") 
    {
        using var hmac = new HMACSHA256();
        return new User
        {
            Id                    = id,
            Username              = "john",
            Name                  = "John Doe",
            Role                  = UserRole.Waiter,
            IsActive              = true,
            EncryptedMobileNumber = "ENC_9111111111",
            MobileNumberHash      = "HASH_9111111111",
            PasswordHash          = hmac.ComputeHash(Encoding.UTF8.GetBytes(password)),
            HashKey               = hmac.Key
        };
    }


    [Test]
    public async Task GetProfile()
    {
        var user = BuildUser();
        SetUpHttpContext(user.Id);
        _userRepoMock.Setup(r => r.Get(user.Id)).ReturnsAsync(user);
        _encryptionMock.Setup(e => e.Decrypt(user.EncryptedMobileNumber)).Returns("9111111111");

        var result = await _userService.GetProfile();

        Assert.Multiple(() =>
        {
            Assert.That(result.Id,           Is.EqualTo(user.Id));
            Assert.That(result.Username,     Is.EqualTo(user.Username));
            Assert.That(result.Name,         Is.EqualTo(user.Name));
            Assert.That(result.MobileNumber, Is.EqualTo("9111111111"));
            Assert.That(result.Role,         Is.EqualTo(UserRole.Waiter));
        });
    }

    [Test]
    public void GetProfile_ShouldThrow_WhenUserNotFound()
    {
        SetUpHttpContext(99);
        _userRepoMock.Setup(r => r.Get(99)).ReturnsAsync((User?)null);

        Assert.ThrowsAsync<UserNotFoundException>(
            () => _userService.GetProfile());
    }


    [Test]
    public async Task UpdateProfile()
    {
        var user = BuildUser();
        SetUpHttpContext(user.Id);
        _userRepoMock.Setup(r => r.Get(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.GetByMobileHashAsync("HASH_9999999999")).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        await _userService.UpdateProfile(new UpdateProfileDto
        {
            Name         = "Updated Name",
            MobileNumber = "9999999999"
        });

        Assert.That(user.Name, Is.EqualTo("Updated Name"));
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
    }

    [Test]
    public void UpdateProfile_NameIsEmpty()
    {
        var ex = Assert.ThrowsAsync<Exception>(
            () => _userService.UpdateProfile(new UpdateProfileDto { Name = "", MobileNumber = "9111111111" }));

        Assert.That(ex!.Message, Is.EqualTo("Name is required"));
    }

    [Test]
    public void UpdateProfile_MobileInvalid()
    {
        var ex = Assert.ThrowsAsync<ValidationException>(
            () => _userService.UpdateProfile(new UpdateProfileDto { Name = "John", MobileNumber = "123" }));

        Assert.That(ex!.Message, Is.EqualTo("Phone number must contain exactly 10 digits."));
    }

    [Test]
    public void UpdateProfile_ShouldThrow_WhenMobileAlreadyTaken()
    {
        var user     = BuildUser();
        var otherUser = BuildUser(id: 2);
        SetUpHttpContext(user.Id);
        _userRepoMock.Setup(r => r.Get(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.GetByMobileHashAsync("HASH_9999999999")).ReturnsAsync(otherUser);

        Assert.ThrowsAsync<DuplicateEntityException>(
            () => _userService.UpdateProfile(new UpdateProfileDto { Name = "John", MobileNumber = "9999999999" }));
    }


    [Test]
    public async Task ChangePassword()
    {
        const string current = "password123";
        const string newPwd  = "newpassword1";

        var user = BuildUser(password: current);
        SetUpHttpContext(user.Id);
        _userRepoMock.Setup(r => r.Get(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        var oldHash = user.PasswordHash.ToArray();

        await _userService.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = current,
            NewPassword     = newPwd
        });

        Assert.That(user.PasswordHash, Is.Not.EqualTo(oldHash));
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
    }

    [Test]
    public void ChangePassword_CurrentPasswordWrong()
    {
        var user = BuildUser(password: "correctpassword");
        SetUpHttpContext(user.Id);
        _userRepoMock.Setup(r => r.Get(user.Id)).ReturnsAsync(user);

        var ex = Assert.ThrowsAsync<Exception>(
            () => _userService.ChangePassword(new ChangePasswordDto
            {
                CurrentPassword = "wrongpassword",
                NewPassword     = "newpassword1"
            }));

        Assert.That(ex!.Message, Is.EqualTo("Current password is incorrect"));
    }

    [Test]
    public void ChangePassword_NewPasswordTooShort()
    {
        var ex = Assert.ThrowsAsync<Exception>(
            () => _userService.ChangePassword(new ChangePasswordDto
            {
                CurrentPassword = "anything",
                NewPassword     = "abc"
            }));

        Assert.That(ex!.Message, Is.EqualTo("Password must be at least 6 characters"));
    }
}
