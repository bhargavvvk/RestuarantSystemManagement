namespace RestaurantAPI.ServiceInterfaces;

public interface IEncryptionService
{
    string Encrypt(string value);
    string GenerateHash(string value);
    string Decrypt(string encryptedValue);
}
