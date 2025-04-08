using Microsoft.AspNetCore.Http;
using SchoolManagementSystem.Services.Implementation;
using System.Threading.Tasks;

namespace SchoolManagementSystem.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sessionService = SessionService.GetInstance(new HttpContextAccessor { HttpContext = context });

            // Set user claims for the current request if authenticated
            if (sessionService.IsAuthenticated())
            {
                // Make user information available for logging and other middleware
                context.Items["UserId"] = sessionService.GetUserId();
                context.Items["Username"] = sessionService.GetUsername();
                context.Items["UserRole"] = sessionService.GetUserRole();
            }

            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }
}