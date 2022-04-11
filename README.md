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

See the [editor guide](docs/editor-guide.md) for more information.

## Latest release

The latest version is available on the Optimizely NuGet feed. You can find it by searching the term Geta.Optimizely.Sitemaps.

## Installation

The command below will install Sitemaps into your Optimizely project.

```
Install-Package Geta.Optimizely.Sitemaps
```

The command below will install Sitemaps Commerce support into your Optimizely Commerce project.

```
Install-Package Geta.Optimizely.Sitemaps.Commerce
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

### Dynamic property for specific pages

You can specify page specific sitemap properties (like change frequency, priority or include/disinclude the specific page in any sitemap) for each Optimizely page by defining a dynamic property with type SEOSitemaps (and the same name):
![Create dynamic property](docs/images/SitemapDynamicPropertyDefine.png?raw=true)

and specify values for the dynamic property:
![Set value for the dynamic property](docs/images/SitemapDynamicPropertyOnPage.PNG?raw=true)

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

## Local development setup

See description in [shared repository](https://github.com/Geta/package-shared/blob/master/README.md#local-development-set-up) regarding how to setup local development environment.

### Docker hostnames

Instead of using the static IP addresses the following hostnames can be used out-of-the-box.

- http://sitemaps.getalocaltest.me
- http://manager-sitemaps.getalocaltest.me

### QuickSilver login

Use the default admin@example.com user for QuickSilver, see [Installation](https://github.com/episerver/Quicksilver).

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md)

## Package maintainer

https://github.com/frederikvig

## Changelog

[Changelog](CHANGELOG.md)
