using EPiServer.DependencyInjection;
using EPiServer.Shell.Modules;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Geta.SEO.Sitemaps.Configuration;
using Microsoft.Extensions.Configuration;

namespace Geta.SEO.Sitemaps.Admin
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSeoSitemaps(this IServiceCollection services)
        {
            return AddSeoSitemaps(services, o => { });
        }

        public static IServiceCollection AddSeoSitemaps(
            this IServiceCollection services,
            Action<SitemapOptions> setupAction)
        {
            AddModule(services);

            services.AddOptions<SitemapOptions>().Configure<IConfiguration>((options, configuration) =>
            {
                setupAction(options);
                configuration.GetSection("Geta:Sitemaps").Bind(options);
            });

            return services;
        }

        private static void AddModule(IServiceCollection services)
        {
            services.AddCmsUI();
            services.Configure<ProtectedModuleOptions>(
                pm =>
                {
                    if (!pm.Items.Any(i => i.Name.Equals(Constants.ModuleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        pm.Items.Add(new ModuleDetails {Name = Constants.ModuleName});
                    }
                });
        }
    }
}