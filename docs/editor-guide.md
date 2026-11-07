# Editor guide

## Configuration

Add a new sitemap definition and fill values for sitemap host and other fields:

- Path to include - only pages that have external url in the specified virtual path will be included in the sitemap
- Path to avoid - pages that have external url in the specified virtual path will not be included in the sitemap. If _Path to include_ specified this will be ignored.
- Root page id - the specified page and it's descendants will be listed in the sitemap. Leave -1 to list all pages of the site. For the Commerce format this is the content ID of a catalog or category instead, the number before `__CatalogContent` in its content link (a catalog's is negative, such as -2147483646), and -1 lists all catalogs. For the Standard and commerce format it selects the pages, and all catalogs are always listed.
- Debug info - if checked sitemap will contain info about page id, language and name as a comment for each entry
- Exclude change frequency and priority - if checked sitemap entries will not contain these elements, which Google ignores
- Format - standard, mobile (to specify [mobile content](http://support.google.com/webmasters/bin/answer.py?hl=en&answer=34648)), commerce, or standard and commerce
- Check 'Enable simple address support' to use the simple address (if used on the page) in the sitemap

![Add a sitemap](images/SitemapAdd.png?raw=true)

In case of multiple sites you choose for which site to host this sitemap:

![Add a sitemap multiple site](images/SitemapAddMultiSite.png?raw=true)

Each sitemap configuration must have a unique host name:

![Configure sitemaps](images/SitemapConfigure.png?raw=true)

## Scheduled job

When configuration done use the "Generate sitemaps" button, or go to the scheduled job "Generate search engine sitemaps" and run/schedule it to run in the necessary frequency. After the scheduled job has been run successfully you can view the sitemap(-s) by either opening the configured sitemap host or clicking "View" next to the sitemap configuration.

![Scheduled job](images/sitemap-scheduled-job.PNG)

## Sitemap property

Per page (needs to be defined on page type) the editor can control if the page should be included in the sitemap, the frequency and priority.

![Scheduled job](images/sitemap-property.PNG)
