using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Authentication;
using System.Text.Json;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
namespace RestaurantAPI.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    public ExceptionMiddleware(RequestDelegate next,ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
             await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing the request.");
             await HandleException(context, ex);
        }
    }
    private static async Task HandleException(HttpContext context,Exception exception)
        {
            context.Response.ContentType ="application/json";

            var response =new ErrorResponseDto();

            switch (exception)
            {
                case TableNotFoundException:
                case MenuItemNotFoundException:
                case CartItemNotFoundException:
                case SessionNotFoundException:

                    response.StatusCode = 404;
                    response.Message = exception.Message;
                    break;

            case MenuItemUnavailableException:
            case TableUnavailableException:
            case ValidationException:
            case CartException:
            case MenuException:
            case DuplicateEntityException:
            case RequestTypeException:

                response.StatusCode = 400;
                    response.Message = exception.Message;
                    break;

            case UnauthorizedAccessException:
            case InvalidSessionOtpException:

                response.StatusCode = 403;
                    response.Message = exception.Message;
                    break;

                default:

                    response.StatusCode = 500;
                    response.Message =exception.Message;
                    break;
            }

            context.Response.StatusCode =
                response.StatusCode;

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
}
