using System.Net;
using Newtonsoft.Json;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            // Call the next middleware in the pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            // Handle the exception
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Set the default response
        var code = HttpStatusCode.InternalServerError; // 500 if unexpected
        var result = JsonConvert.SerializeObject(new
        {
            error = "An unexpected error occurred.",
            detail = exception.Message // You can customize the level of detail here
        });

        // Customize based on exception type (optional)
        if (exception is ArgumentException) code = HttpStatusCode.BadRequest;
        else if (exception is InvalidOperationException) code = HttpStatusCode.BadRequest;

        // Set the response details
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        // Return the JSON response
        return context.Response.WriteAsync(result);
    }
}
