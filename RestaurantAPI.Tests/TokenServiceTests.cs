using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.Tests;


public class TokenServiceTests
{
    private IConfiguration _config;

    [SetUp]
    public void SetUp()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:Key"]             = "ThisIsAVeryLongSecretKeyForJwtTesting12345",
                ["JWT:Issuer"]          = "TestIssuer",
                ["JWT:DurationInHours"] = "8"
            })
            .Build();
    }

    [Test]
    public void CreateEmployeeToken_ShouldReturnToken()
    {
        var service = new TokenService(_config);

        var token = service.CreateEmployeeToken(new EmployeeTokenRequest
        {
            UserId   = 1,
            Username = "admin",
            Role     = "Admin"
        });

        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }


    [Test]
    public void CreateCustomerToken_ShouldReturnToken()
    {
        var service = new TokenService(_config);

        var token = service.CreateCustomerToken(new CustomerTokenRequest
        {
            SessionId = 1,
            TableId   = 2,
            CartId    = 3,
            WaiterId  = 4
        });

        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }
}
