using Geta.Optimizely.Sitemaps.Commerce;
using Geta.Optimizely.Sitemaps.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        _foundationStartup.Configure(app, env);
    }
}
