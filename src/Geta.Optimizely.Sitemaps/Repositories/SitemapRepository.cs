// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Data;
using EPiServer.DataAbstraction;
using Geta.Optimizely.Sitemaps.Entities;

namespace Geta.Optimizely.Sitemaps.Repositories
{
    public class SitemapRepository : ISitemapRepository
    {
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly IApplicationResolver _applicationResolver;
        private readonly ISitemapLoader _sitemapLoader;


        public SitemapRepository(
            ILanguageBranchRepository languageBranchRepository,
            IApplicationResolver applicationResolver,
            ISitemapLoader sitemapLoader)
        {
            _languageBranchRepository = languageBranchRepository ?? throw new ArgumentNullException(nameof(languageBranchRepository));
            _applicationResolver = applicationResolver ?? throw new ArgumentNullException(nameof(applicationResolver));
            _sitemapLoader = sitemapLoader ?? throw new ArgumentNullException(nameof(sitemapLoader));
        }

        public void Delete(Identity id)
        {
            _sitemapLoader.Delete(id);
        }

        public SitemapData GetSitemapData(Identity id)
        {
            return _sitemapLoader.GetSitemapData(id);
        }

        public SitemapData GetSitemapData(string requestUrl)
        {
            var url = new Url(requestUrl);

            // contains the sitemap URL, for example en/sitemap.xml
            var host = url.Path.TrimStart('/').ToLowerInvariant();

            // First attempt to get the site based on just the host.
            // If that fails, try to include the port and fallback to default if none is found.
            var app = _applicationResolver.GetByHostname(url.Host, false).Application ??
                _applicationResolver.GetByHostname($"{url.Host}:{url.Port}", true).Application;

            if (app is not IRoutableApplication site)
            {
                return null;
            }

            var sitemapData = GetAllSitemapData()?.Where(x =>
                GetHostWithLanguage(x) == host &&
                (x.SiteUrl == null || site.Hosts.Any(h => h.Authority == new Url(x.SiteUrl).Authority))).ToList();

            if (sitemapData?.Count == 1)
            {
                return sitemapData.FirstOrDefault();
            }

            // Could happen that we found multiple sitemaps when for each host in the SiteDefinition a Sitemap is created.
            // In that case, use the requestURL to get the correct SiteMapData
            return sitemapData?.FirstOrDefault(x => new Url(x.SiteUrl).Authority == url.Authority);
        }

        public string GetSitemapUrl(SitemapData sitemapData)
        {
            return string.Format("{0}{1}", sitemapData.SiteUrl, GetHostWithLanguage(sitemapData));
        }

        /// <summary>
        /// Returns host with language.
        /// For example en/sitemap.xml
        /// </summary>
        /// <param name="sitemapData"></param>
        /// <returns></returns>
        public string GetHostWithLanguage(SitemapData sitemapData)
        {
            if (string.IsNullOrWhiteSpace(sitemapData.Language))
            {
                return sitemapData.Host.ToLowerInvariant();
            }

            var languageBranch = _languageBranchRepository.Load(sitemapData.Language);

            if (languageBranch != null)
            {
                return $"{languageBranch.URLSegment}/{sitemapData.Host}".ToLowerInvariant();
            }
            return sitemapData.Host.ToLowerInvariant();
        }

        public IList<SitemapData> GetAllSitemapData()
        {
            return _sitemapLoader.GetAllSitemapData();
        }

        public void Save(SitemapData sitemapData)
        {
            _sitemapLoader.Save(sitemapData);
        }
    }
}
