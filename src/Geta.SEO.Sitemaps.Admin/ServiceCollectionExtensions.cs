using EPiServer.DependencyInjection;
using EPiServer.Shell.Modules;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Geta.SEO.Sitemaps.Admin
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSeoSitemaps(this IServiceCollection services)
        {
            services.AddCmsUI();
            services.Configure<ProtectedModuleOptions>(
                pm =>
                {
                    if (!pm.Items.Any(i => i.Name.Equals(Constants.ModuleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        pm.Items.Add(new ModuleDetails { Name = Constants.ModuleName});
                    }
                });

            return services;
        }
    }
}
