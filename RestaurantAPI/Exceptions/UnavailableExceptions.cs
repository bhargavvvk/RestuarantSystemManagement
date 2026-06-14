namespace RestaurantAPI.Exceptions;

public abstract class UnavailableException : Exception
{
    protected UnavailableException(string message)
        : base(message)
    {
    }
}

public class TableUnavailableException : UnavailableException
{
    public TableUnavailableException()
        : base($"Table is unavailable.")
    {
    }
}

public class MenuItemUnavailableException : UnavailableException
{
    public MenuItemUnavailableException()
        : base($"Menu item is unavailable.")
    {
    }
}
