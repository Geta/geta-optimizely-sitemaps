# Geta Optimizely Sitemaps

![](http://tc.geta.no/app/rest/builds/buildType:(id:GetaPackages_OptimizelySitemaps_00ci),branch:master/statusIcon)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Geta_geta-optimizely-sitemaps&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Geta_geta-optimizely-sitemaps)
[![Platform](https://img.shields.io/badge/Platform-.NET%205-blue.svg?style=flat)](https://docs.microsoft.com/en-us/dotnet/)
[![Platform](https://img.shields.io/badge/Optimizely-%2012-orange.svg?style=flat)](http://world.episerver.com/cms/)

Search engine sitemaps.xml for Optimizely CMS 12 and Commerce 14

## Description

This tool allows you to generate xml sitemaps for search engines to better index your Optimizely sites.

## Features

- sitemap generation as a scheduled job
- filtering pages by virtual directories
- ability to include pages that are in a different branch than the one of the start page
- ability to generate sitemaps for mobile pages
- it also supports multi-site and multi-language environments
- ability to augment URL generation for parameterized pages using QueryStrings

See the [editor guide](docs/editor-guide.md) for more information.

## Installation

The command below will install Sitemaps into your Optimizely project.

```
dotnet add package Geta.Optimizely.Sitemaps
```

The command below will install Sitemaps Commerce support into your Optimizely Commerce project.

```
dotnet add package Geta.Optimizely.Sitemaps.Commerce
```

## Configuration

For the Sitemaps to work, you have to call AddSitemaps extension method in Startup.ConfigureServices method. This method provides a configuration of default values. Below is a code with all possible configuration options:

```csharp
services.AddSitemaps(x =>
{
  x.EnableLanguageDropDownInAdmin = false;
  x.EnableRealtimeCaching = true;
  x.EnableRealtimeSitemap = false;
});
```

You can configure access to the sitemaps configuration tab by adding a custom policy (the default is WebAdmins):

```csharp
services.AddSitemaps(x =>
{
  x.EnableLanguageDropDownInAdmin = false;
  x.EnableRealtimeCaching = true;
  x.EnableRealtimeSitemap = false;
}, p => p.RequireRole(Roles.Administrators));
```

In order to augment Urls for the PersonListPage with the corresponding querystring parameters for said page, please review the (SitemapUriParameterAugmenterService class)[Sandbox/Foundation/Infrastructure/Cms/Services/SitemapUriParameterAugmenterService.cs] within the Foundation project:

```csharp
services.AddSitemaps(options =>
{
    options.UriAugmenterServiceImplementationFactory = sp => sp.GetInstance<SitemapUriParameterAugmenterService>();
});
```

And for the Commerce support add a call to:
```csharp
services.AddSitemapsCommerce();
```

It is also possible to configure the application in `appsettings.json` file. A configuration from the `appsettings.json` will override configuration configured in Startup. Below is an `appsettings.json` configuration example.

```json
"Geta": {
    "Sitemaps": {
        "EnableLanguageDropDownInAdmin":  true
    }
}
```

Also, you have to add Razor pages routing support.

```
app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
});
```

## Usage

### Adding Sitemap Properties to all content pages

Credits to [jarihaa](https://github.com/jarihaa) for [contributing](https://github.com/Geta/SEO.Sitemaps/pull/87) this.

```
[UIHint("SeoSitemap")]
[BackingType(typeof(PropertySEOSitemaps))]
public virtual string SEOSitemaps { get; set; }
```

#### Set default value

```
public override void SetDefaultValues(ContentType contentType)
{
    base.SetDefaultValues(contentType);
    var sitemap = new PropertySEOSitemaps
    {
        Enabled = false
    };
    sitemap.Serialize();
    this.SEOSitemaps = sitemap.ToString();
}
```

### Ignore page types

Implement the `IExcludeFromSitemap` interface to ignore page types in the sitemap.

```
public class OrderConfirmationPage : PageData, IExcludeFromSitemap
```

### Exclude content

If you need more control to exclude content from the sitemap you can make your own implementation of IContentFilter. Make sure to inherit from ContentFilter and call the `ShouldExcludeContent` method of the base class.

```
public class SiteContentFilter : ContentFilter
    {
        public override bool ShouldExcludeContent(IContent content)
        {
            if (base.ShouldExcludeContent(content))
            {
                return true;
            }

            // Custom logic here

            return false;
        }
    }
```

Register in your DI container.

```csharp
services.AddTransient<IContentFilter, SiteContentFilter>();
```

## Limitations

- Each sitemap will contain max 50k entries (according to [sitemaps.org protocol](http://www.sitemaps.org/protocol.html#index)) so if the site in which you are using this plugin contains more active pages then you should split them over multiple sitemaps (by specifying a different root page or include/avoid paths for each).

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md)

## Package maintainer

https://github.com/marisks

## Changelog

[Changelog](CHANGELOG.md)
