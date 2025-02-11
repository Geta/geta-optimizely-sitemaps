using EPiServer.Framework.Hosting;
using EPiServer.Web.Hosting;
using Geta.Optimizely.Sitemaps.Commerce;
using Geta.Optimizely.Sitemaps.Web.Services;

namespace Geta.Optimizely.Sitemaps.Web;

public class Startup
{
    private readonly Foundation.Startup _foundationStartup;

    public Startup(IWebHostEnvironment webHostingEnvironment, IConfiguration configuration)
    {
        _foundationStartup = new Foundation.Startup(webHostingEnvironment, configuration);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        _foundationStartup.ConfigureServices(services);
        // Implement the UriAugmenterServiceImplementationFactory in order to enumerate the PersonalListPage querystring parameters.
        services.AddSitemaps(options =>
        {
            options.SetAugmenterService<SitemapUriParameterAugmenterService>();
        });
        services.AddSitemapsCommerce();
        
        var moduleName = typeof(ContainerController).Assembly.GetName().Name;
        var fullPath = Path.GetFullPath($"..\\{moduleName}\\module");

        services.Configure<CompositeFileProviderOptions>(options =>
        {
            options.BasePathFileProviders.Add(new MappingPhysicalFileProvider(
                                                  $"/EPiServer/{moduleName}",
                                                  string.Empty,
                                                  fullPath));
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        _foundationStartup.Configure(app, env);
    }
}
