using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaxiDispatch.Modules.Membership.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
        {
            return;
        }

        var user = context.HttpContext.Items["User"];
        if (user == null)
        {
            var lockout = false;
            var inactive = false;
            try
            {
                lockout = (bool)context.HttpContext.Items["LockedOut"];
            }
            catch (NullReferenceException)
            {
                // expected on first run
            }

            try
            {
                inactive = (bool)context.HttpContext.Items["Inactive"];
            }
            catch (NullReferenceException)
            {
                // expected when not set
            }

            if (lockout)
            {
                context.Result = new JsonResult(new
                {
                    message = "User Locked"
                })
                {
                    StatusCode = StatusCodes.Status423Locked
                };
            }
            else if (inactive)
            {
                context.Result = new JsonResult(new
                {
                    message = "User Inactive"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
            else
            {
                context.Result = new JsonResult(new
                {
                    message = "Unauthorized"
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
        }
    }
}
