using System;
using System.Collections.Generic;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace Geta.Optimizely.Sitemaps.Services
{
    [ServiceConfiguration(typeof(IUriAugmenterService))]
    public class NullUriAugmenterService : IUriAugmenterService
    {
        public IEnumerable<Uri> AugmentUri(IContent content, CurrentLanguageContent languageContentInfo, Uri fullUri)
        {
            return new Uri[] { fullUri };
        }
    }
}
