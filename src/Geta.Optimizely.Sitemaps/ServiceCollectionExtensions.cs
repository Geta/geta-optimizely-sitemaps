using System;
using System.Linq;
using EPiServer.Authorization;
using EPiServer.Shell.Modules;
using Geta.Mapping;
using Geta.Optimizely.Sitemaps.Configuration;
using Geta.Optimizely.Sitemaps.Entities;
using Geta.Optimizely.Sitemaps.Models;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Services;
using Geta.Optimizely.Sitemaps.Utils;
using Geta.Optimizely.Sitemaps.XML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.Optimizely.Sitemaps
{
    public static class ServiceCollectionExtensions
    {
        private static readonly Action<AuthorizationPolicyBuilder> DefaultPolicy = p => p.RequireRole(Roles.WebAdmins);

        public static IServiceCollection AddSitemaps(this IServiceCollection services,
            Func<IServiceProvider, IUriAugmenterService> uriAugmenterService = null)
        {
            return AddSitemaps(services, _ => { }, DefaultPolicy, uriAugmenterService);
        }

        public static IServiceCollection AddSitemaps(
            this IServiceCollection services,
            Action<SitemapOptions> setupAction,
            Func<IServiceProvider, IUriAugmenterService> uriAugmenterService = null)
        {
            return AddSitemaps(services, setupAction, DefaultPolicy, uriAugmenterService);
        }

        public static IServiceCollection AddSitemaps(
            this IServiceCollection services,
            Action<SitemapOptions> setupAction,
            Action<AuthorizationPolicyBuilder> configurePolicy,
            Func<IServiceProvider, IUriAugmenterService> uriAugmenterService = null)
        {
            AddModule(services);

            services.AddSingleton<SitemapXmlGeneratorFactory>();
            services.AddSingleton<ISitemapLoader, SitemapLoader>();
            services.AddSingleton<ISitemapRepository, SitemapRepository>();
            services.AddSingleton<IContentFilter, ContentFilter>();
            services.AddTransient<IMobileSitemapXmlGenerator, MobileSitemapXmlGenerator>();
            services.AddTransient<IStandardSitemapXmlGenerator, StandardSitemapXmlGenerator>();
            services.AddTransient(typeof(IMapper<SitemapViewModel, SitemapData>), typeof(SitemapViewModel.MapperToEntity));
            services.AddTransient(typeof(ICreateFrom<SitemapData, SitemapViewModel>), typeof(SitemapViewModel.MapperFromEntity));

            services.AddOptions<SitemapOptions>().Configure<IConfiguration>((options, configuration) =>
            {
                setupAction(options);
                configuration.GetSection("Geta:Sitemaps").Bind(options);
            });
            
            if (uriAugmenterService != null)
            {
                services.AddSingleton(typeof(IUriAugmenterService), uriAugmenterService);
            }
            else
            {
                services.AddSingleton<IUriAugmenterService, DefaultUriAugmenterService>();
            }

            services.AddAuthorization(options =>
            {
                options.AddPolicy(Constants.PolicyName, configurePolicy);
            });

            return services;
        }

        private static void AddModule(IServiceCollection services)
        {
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
