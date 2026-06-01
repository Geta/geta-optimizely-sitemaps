using System.Reflection;
using EPiServer.Framework.Hosting;
using EPiServer.Web.Hosting;
using Geta.Optimizely.Sitemaps.Commerce;
using Geta.Optimizely.Sitemaps.Web.Services;
using Optimizely.Graph.Cms.Configuration;

namespace Geta.Optimizely.Sitemaps.Web;

public class Startup
{
    private readonly Foundation.Startup _foundationStartup;
    private readonly IConfiguration _configuration;

    public Startup(IWebHostEnvironment webHostingEnvironment, IConfiguration configuration)
    {
        _foundationStartup = new Foundation.Startup(webHostingEnvironment, configuration);
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        _foundationStartup.ConfigureServices(services);

        var graphAppKey = _configuration["Optimizely:ContentGraph:AppKey"];
        if (string.IsNullOrEmpty(graphAppKey))
        {
            var syncClientType = typeof(GraphCmsOptions).Assembly
                .GetType("Optimizely.Graph.Cms.Client.ISyncClient");
            if (syncClientType != null)
            {
                var descriptor = services.FirstOrDefault(d => d.ServiceType == syncClientType);
                if (descriptor != null) services.Remove(descriptor);

                var createMethod = typeof(DispatchProxy).GetMethod(nameof(DispatchProxy.Create))!;
                var proxy = createMethod
                    .MakeGenericMethod(syncClientType, typeof(NoOpSyncClientProxy))
                    .Invoke(null, null)!;

                services.AddSingleton(syncClientType, proxy);
            }
        }

        // Implement the UriAugmenterServiceImplementationFactory in order to enumerate the PersonalListPage querystring parameters.
        services.AddSitemaps(options =>
        {
            options.SetAugmenterService<SitemapUriParameterAugmenterService>();
        });
        services.AddSitemapsCommerce();
        
        var moduleName = typeof(ContainerController).Assembly.GetName().Name;
        var fullPath = Path.GetFullPath($"../{moduleName}/module");

        services.Configure<CompositeFileProviderOptions>(options =>
        {
            options.BasePathFileProviders.Add(new MappingPhysicalFileProvider(
                                                  $"/Optimizely/{moduleName}",
                                                  string.Empty,
                                                  fullPath));
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        _foundationStartup.Configure(app, env);
    }
}
