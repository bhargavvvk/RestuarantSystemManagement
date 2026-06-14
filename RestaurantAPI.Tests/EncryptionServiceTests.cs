using Microsoft.Extensions.Configuration;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;


public class EncryptionServiceTests
{
    private IConfiguration _config;

    [SetUp]
    public void SetUp()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "12345678901234567890123456789012",
                ["Encryption:IV"]  = "1234567890123456"
            })
            .Build();
        
    }

    [Test]
    public void GenerateHash_ShouldReturnSameHash_ForSameInput()
    {
        var service = new EncryptionService(_config);

        var hash1 = service.GenerateHash("9876543210");
        var hash2 = service.GenerateHash("9876543210");

        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void GenerateHash_ShouldReturnDifferentHash_ForDifferentInputs()
    {
        var service = new EncryptionService(_config);

        var hash1 = service.GenerateHash("1111111111");
        var hash2 = service.GenerateHash("2222222222");

        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void Encrypt_ShouldReturnDifferentValue()
    {
        var service = new EncryptionService(_config);

        var encrypted = service.Encrypt("9876543210");

        Assert.That(encrypted, Is.Not.EqualTo("9876543210"));
    }

    [Test]
    public void EncryptDecrypt_ShouldReturnOriginalValue()
    {
        var service = new EncryptionService(_config);

        var original  = "9876543210";
        var encrypted = service.Encrypt(original);
        var decrypted = service.Decrypt(encrypted);

        Assert.That(decrypted, Is.EqualTo(original));
    }
}
