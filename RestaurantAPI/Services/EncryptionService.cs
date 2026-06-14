using System.Security.Cryptography;
using System.Text;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class EncryptionService:IEncryptionService
{
    private readonly string _key;
    private readonly string _iv;
    public EncryptionService(IConfiguration configuration)
    {
        _key = configuration["Encryption:Key"]!;
        _iv = configuration["Encryption:IV"]!;
    }
    public string GenerateHash(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
    public string Encrypt(string value)
    {
        using var aes = Aes.Create();

        aes.Key = Encoding.UTF8.GetBytes(_key);
        aes.IV = Encoding.UTF8.GetBytes(_iv);

        var encryptor = aes.CreateEncryptor();

        using var ms = new MemoryStream();

        using var cs = new CryptoStream(
            ms,
            encryptor,
            CryptoStreamMode.Write);

        using var sw = new StreamWriter(cs);

        sw.Write(value);

        sw.Close();

        return Convert.ToBase64String(ms.ToArray());
    }
    public string Decrypt(string encryptedValue)
    {
        using var aes = Aes.Create();

        aes.Key = Encoding.UTF8.GetBytes(_key);
        aes.IV = Encoding.UTF8.GetBytes(_iv);

        var decryptor = aes.CreateDecryptor();

        var cipherBytes =Convert.FromBase64String(encryptedValue);

        using var ms =
            new MemoryStream(cipherBytes);

        using var cs =
            new CryptoStream(
                ms,
                decryptor,
                CryptoStreamMode.Read);

        using var sr =new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
