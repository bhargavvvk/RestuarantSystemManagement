namespace RestaurantAPI.Exceptions;

public class ActiveDiningSessionExistsException:Exception
{
    public ActiveDiningSessionExistsException()
        : base("An active dining session already exists for this table.")
    {
    }
    public ActiveDiningSessionExistsException(string message)
        : base(message)
    {
    }
}
