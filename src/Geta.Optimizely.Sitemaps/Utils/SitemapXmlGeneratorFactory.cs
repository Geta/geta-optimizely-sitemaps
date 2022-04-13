// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using Geta.Optimizely.Sitemaps.Entities;
using Geta.Optimizely.Sitemaps.XML;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.Optimizely.Sitemaps.Utils
{
    public class SitemapXmlGeneratorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SitemapXmlGeneratorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public virtual ISitemapXmlGenerator GetSitemapXmlGenerator(SitemapData sitemapData)
        {
            ISitemapXmlGenerator xmlGenerator;

            switch (sitemapData.SitemapFormat)
            {
                case SitemapFormat.Mobile:
                    xmlGenerator = _serviceProvider.GetRequiredService<IMobileSitemapXmlGenerator>();
                    break;
                case SitemapFormat.Commerce:
                    xmlGenerator = _serviceProvider.GetRequiredService<ICommerceSitemapXmlGenerator>();
                    break;
                case SitemapFormat.StandardAndCommerce:
                    xmlGenerator = _serviceProvider.GetRequiredService<ICommerceAndStandardSitemapXmlGenerator>();
                    break;
                default:
                    xmlGenerator = _serviceProvider.GetRequiredService<IStandardSitemapXmlGenerator>();
                    break;
            }

            xmlGenerator.IsDebugMode = sitemapData.IncludeDebugInfo;

            return xmlGenerator;
        }
    }
}
