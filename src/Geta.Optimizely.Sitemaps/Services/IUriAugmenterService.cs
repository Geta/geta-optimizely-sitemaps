using System;
using System.Collections.Generic;
using EPiServer.Core;

namespace Geta.Optimizely.Sitemaps.Services
{
    public interface IUriAugmenterService
    {
        /// <summary>
        /// Allows sitemap implementer an easy facility to take a simple url and expand it in a number of ways, includig parameterizing it with QueryStrings.
        /// </summary>
        /// <param name="content">Original content of page URL being created</param>
        /// <param name="languageContentInfo">Language for URI</param>
        /// <param name="originUri">Origin URI to be included in sitemap</param>
        /// <returns>Must include origin to be included in sitemap</returns>
        IEnumerable<Uri> GetAugmentUris(IContent content, CurrentLanguageContent languageContentInfo, Uri originUri);
    }
}
