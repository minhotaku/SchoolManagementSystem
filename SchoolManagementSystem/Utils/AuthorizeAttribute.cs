using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Services.Implementation;
using System;
using System.Linq;

namespace SchoolManagementSystem.Utils
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public AuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;
            var sessionService = SessionService.GetInstance(
                new HttpContextAccessor { HttpContext = httpContext });

            // Check if user is authenticated
            if (!sessionService.IsAuthenticated())
            {
                // User is not authenticated, redirect to login
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // If roles are specified, check if user is in any of these roles
            if (_roles != null && _roles.Length > 0)
            {
                if (!sessionService.IsInAnyRole(_roles))
                {
                    // User is not in any of the required roles, return unauthorized
                    context.Result = new ViewResult { ViewName = "Unauthorized" };
                    return;
                }
            }
        }
    }
}