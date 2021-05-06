using EPiServer.ServiceLocation;
using EPiServer.Shell.Security;
using EPiServer.Templates.Alloy.Mvc.Extensions;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AlloyMvcTemplates.Infrastructure
{
    public class AdministratorRegistrationPageMiddleware
    {
        private readonly RequestDelegate _next;

        private static bool _isFirstRequest = true;
        private const string RegisterUrl = "/Register";

        public static bool? IsEnabled { get; set; }

        public AdministratorRegistrationPageMiddleware(RequestDelegate next)
        {
            _next = next;
        }


        public async Task InvokeAsync(HttpContext context)
        {
            if (!_isFirstRequest)
            {
                await _next(context);
                return;
            }

            _isFirstRequest = false;

            if (!context.IsLocalRequest() || context.Request.Path != "/")
            {

                await _next(context);
                return;
            }

            if (!IsEnabled.HasValue)
            {
                IsEnabled = await UserDatabaseIsEmpty();
            }

            if (IsEnabled.Value)
            {
                context.Response.Redirect(RegisterUrl);
            }

            await _next(context);
        }


        private async Task<bool> UserDatabaseIsEmpty()
        {
            var provider = ServiceLocator.Current.GetInstance<UIUserProvider>();
            await foreach(var res in provider.GetAllUsersAsync(0, 1))
            {
                return false;
            }
            return true;
        }
    }
}
