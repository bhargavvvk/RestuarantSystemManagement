namespace RestaurantAPI.Services.AI.Contracts;

public interface IToolDispatcher
{
    Task<string> ExecuteAsync(string toolName,string arguments);
}
