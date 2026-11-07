# Changelog

All notable changes to this project will be documented in this file.

## [5.0.0]

- Switch from SiteDefinition to the new Application framework
- Remove remaining obsolete warnings
- Add a "Generate sitemaps" button to the admin page [#70](https://github.com/Geta/geta-optimizely-sitemaps/issues/70)
- Ask for confirmation before deleting a sitemap [#69](https://github.com/Geta/geta-optimizely-sitemaps/issues/69)
- Add a sitemap option to exclude changefreq and priority [#117](https://github.com/Geta/geta-optimizely-sitemaps/issues/117)
- Allow priorities in steps of 0.1 in the property editor [#75](https://github.com/Geta/geta-optimizely-sitemaps/issues/75)
- Make the property editor's labels translatable [#76](https://github.com/Geta/geta-optimizely-sitemaps/issues/76)
- Report in the job result when a sitemap has no entries or reached the entry limit [#99](https://github.com/Geta/geta-optimizely-sitemaps/issues/99) [#141](https://github.com/Geta/geta-optimizely-sitemaps/issues/141)
- Fix the job failing with "Value cannot be null" for a sitemap without a host, and not generating the default sitemap it creates [#108](https://github.com/Geta/geta-optimizely-sitemaps/issues/108)
- Fix a sitemap for an unknown host succeeding with 0 entries; it now fails with an error [#99](https://github.com/Geta/geta-optimizely-sitemaps/issues/99)
- Fix the root page ID being applied to the catalog, and ignored for pages, in the "Standard and commerce" format [#93](https://github.com/Geta/geta-optimizely-sitemaps/issues/93)
- Fix hreflang alternates listing language fallback content as translations [#109](https://github.com/Geta/geta-optimizely-sitemaps/issues/109)
- Fix broken links for languages with their own host: those languages now only appear in their own host's sitemap, and hreflang alternates link to that host [#124](https://github.com/Geta/geta-optimizely-sitemaps/issues/124)
- Fix hreflang alternates being reused between sitemaps of different hosts
- Fix saving a sitemap changing its host when the saved URL wasn't in the host list, for example from https to http [#77](https://github.com/Geta/geta-optimizely-sitemaps/issues/77)
- Fix relative URLs being treated as absolute file URLs on Linux [#64](https://github.com/Geta/geta-optimizely-sitemaps/issues/64)
- Fix failing notification requests from the platform navigation on the sitemaps tab [#140](https://github.com/Geta/geta-optimizely-sitemaps/issues/140)
- Fix `this.onChange is not a function` in the property editor, and loading a value marking the page as changed [#52](https://github.com/Geta/geta-optimizely-sitemaps/issues/52)
- Fix the job message ending in a colon when a custom generator reports no error [#147](https://github.com/Geta/geta-optimizely-sitemaps/issues/147)
- Fix a generated sitemap returning 404 when its host was entered with capitals or an explicit default port
- Fix a sitemap without a host being served from sites other than the default site
- Fix the job saving the configuration it loaded at its start over edits made while it ran, and bringing back deleted sitemaps
- Fix one sitemap that can't be generated stopping the job for the others, and the job's result being lost when it's stopped
- Fix the entry limit being exceeded when a page gets several URLs from a URI augmenter, and the job reporting left out content when none was
- Fix the sitemaps tab failing when a sitemap's language has been deleted
- Fix the property editor failing when it's used without SeoSitemapEditorDescriptor

### Upgrading from 4.x

- Requires Optimizely CMS 13.1 or later. A site created from the CMS 13.0 Alloy template has to upgrade `EPiServer.CMS` first.
- Custom generators, content filters and anything else built on the classes below need their constructors and overrides updated.
- Check "Standard and commerce" sitemaps whose root page ID points at a catalog node: change it to a page ID, or -1.

### Breaking changes

- IContentFilter: Signature changed on ShouldExcludeContent (removed SiteDefinition parameter)
- All use of ISiteDefinitionRepository replaced by IApplicationRepository, including the generator constructors
- XmlGenerator: SiteDefinition property replaced by CurrentSite
- XmlGenerator: GetSiteDefinitionFromSiteUri replaced by GetApplicationFromSiteUri, and GetHostDefinition by GetApplicationHost
- CommerceSitemapXmlGenerator: the private reference converter field is now the protected ReferenceConverter field
- HostDefinitionExtensions and UriComparer removed
- The generator constructors no longer take ISynchronizedObjectInstanceCache and IMemoryCache, SitemapCreateJob's no longer takes ISynchronizedObjectInstanceCache, and SitemapCreateJob.SitemapGenerationCacheKey is removed

### Behaviour changes

- A sitemap on a host without a language no longer lists languages that have their own host, unless it was configured for that language
- A root page ID of 0 now means the same as -1: the site's start page, or the catalog root
- In the "Standard and commerce" format, the root page ID now selects the pages and all catalogs are listed. A saved value that pointed at a catalog node stops listing the site's pages without an error, so change it to a page ID or -1
- The Commerce format now also lists the catalog or category selected by the root page ID, not only what's below it
- `SitemapXmlGenerator.LastError` can now be set by derived generators

## [4.0.0]

- Upgraded to Optimizely CMS 13.1.0 (.NET 10)
- Upgraded to Optimizely Commerce 15.0.0
- Removed SonarCloud from CI pipeline
- Fixed SitemapXmlGenerator silent fallback when site definition is not found
- Fixed typo and created const for SitemapGenerationCacheKey
- Updated Geta.Mapping package version
- Code style cleanup: added braces to all control flow statements

## [3.2.1]

### Breaking changes

These were released in a patch version without being listed here.

- ContentFilter: the constructor takes an additional `IOptions<SitemapOptions>` parameter, which breaks derived classes [#118](https://github.com/Geta/geta-optimizely-sitemaps/pull/118)
- SitemapOptions: the `EnableLanguageDropDownInAdmin` option was removed [#134](https://github.com/Geta/geta-optimizely-sitemaps/pull/134)

## [2.0.0]

- Cleanup
- GitHub Workflow for build
- Sonarcloud for code quality checks
- UI improvements

## [1.0.1]

- Cleanup editing in UI [#33](https://github.com/Geta/geta-optimizely-sitemaps/pull/33)
- Add authorization policy [#34](https://github.com/Geta/geta-optimizely-sitemaps/pull/34)
- Fix sitemap routing [#36](https://github.com/Geta/geta-optimizely-sitemaps/pull/36)
- Add missing siteurl on single host [#32](https://github.com/Geta/geta-optimizely-sitemaps/pull/32)

## [1.0.0]

- Initial version
