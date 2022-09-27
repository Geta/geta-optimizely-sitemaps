using System;
using System.Collections.Generic;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace Geta.Optimizely.Sitemaps.Services
{
    [ServiceConfiguration(typeof(IUriAugmenterService))]
    public class DefaultUriAugmenterService : IUriAugmenterService
    {
        public IEnumerable<Uri> GetAugmentUri(IContent content, CurrentLanguageContent languageContentInfo, Uri fullUri)
        {
            return new Uri[] { fullUri };
        }
    }
}
