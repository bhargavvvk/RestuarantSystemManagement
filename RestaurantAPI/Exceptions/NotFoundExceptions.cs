using Microsoft.AspNetCore.Http.HttpResults;

namespace RestaurantAPI.Exceptions;


public abstract class NotFoundException : Exception
{
    protected NotFoundException(string message)
        : base(message)
    {
    }
}

public class TableNotFoundException : NotFoundException
{
    public TableNotFoundException()
        : base($"Table  was not found.")
    {
    }
}

public class MenuItemNotFoundException : NotFoundException
{
    public MenuItemNotFoundException()
        : base($"Menu item was not found.")
    {
    }
}

public  class CartItemNotFoundException : NotFoundException
{
    public CartItemNotFoundException()
        : base($"Cart item was not found.")
    {
    }
}

public class SessionNotFoundException : NotFoundException
{
    public SessionNotFoundException()
        : base($"Session was not found.")
    {
    }
}

public class BillNotFoundException : NotFoundException
{
    public BillNotFoundException():base("Bill Not found")
    {

    }
}

public class CartNotFoundException : NotFoundException
{
    public CartNotFoundException() : base("Cart Not found")
    {
    }
}

public class OrderItemNotFoundException : NotFoundException
{
    public OrderItemNotFoundException() : base("OrderItem not found")
    {

    }
    public OrderItemNotFoundException(string message) : base(message)
    {

    }
}
public class CustomerRequestNotFoundException : NotFoundException
{
    public CustomerRequestNotFoundException() : base("Customer Request Not found")
    {

    }
}
public class OrderNotFoundException: NotFoundException
{
    public OrderNotFoundException() : base("Order Not found")
    {

    }
}

public class WaiterNotFoundException : NotFoundException
{
    public WaiterNotFoundException(string message):base(message)
    {

    }
}

public class CategoryNotFoundException : Exception
{
    public CategoryNotFoundException(string message) : base(message)
    {

    }
}
public class InventoryItemNotFoundException : Exception
{

    public InventoryItemNotFoundException(string message) : base(message)
    {

    }

}
public class UserNotFoundException : Exception
{
    public UserNotFoundException(string message) : base(message)
    {

    }
    public UserNotFoundException() : base("User not found")
    {

    }
}