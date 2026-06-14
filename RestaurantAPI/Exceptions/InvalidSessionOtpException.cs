namespace RestaurantAPI.Exceptions;

public class InvalidSessionOtpException:Exception
{
    public InvalidSessionOtpException()
        : base("Invalid session OTP.")
    {
    }
    public InvalidSessionOtpException(string message):base(message)
    {
    }
}
