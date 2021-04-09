// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using EPiServer.Data;
using EPiServer.Data.Dynamic;
using Geta.SEO.Sitemaps.Entities;

namespace Geta.SEO.Sitemaps.Repositories
{
    public class SitemapLoader : ISitemapLoader
    {
        private static DynamicDataStore SitemapStore => typeof(SitemapData).GetStore();

        public void Delete(Identity id)
        {
            SitemapStore.Delete(id);
        }

        public SitemapData GetSitemapData(Identity id)
        {
            return SitemapStore.Items<SitemapData>().FirstOrDefault(sitemap => sitemap.Id == id);
        }

        public virtual IList<SitemapData> GetAllSitemapData()
        {
            return SitemapStore.Items<SitemapData>().ToList();
        }

        public void Save(SitemapData sitemapData)
        {
            if (sitemapData == null)
            {
                return;
            }

            SitemapStore.Save(sitemapData);
        }
    }
}
