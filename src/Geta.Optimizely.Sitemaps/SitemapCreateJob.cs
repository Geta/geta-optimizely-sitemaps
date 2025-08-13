// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EPiServer;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using Geta.Optimizely.Sitemaps.Entities;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Utils;
using Geta.Optimizely.Sitemaps.XML;

namespace Geta.Optimizely.Sitemaps
{
    [ScheduledPlugIn(GUID = "EC74D2A3-9D77-4265-B4FF-A1935E3C3110", DisplayName = "Generate search engine sitemaps")]
    public class SitemapCreateJob : ScheduledJobBase
    {
        private readonly ISitemapRepository _sitemapRepository;
        private readonly SitemapXmlGeneratorFactory _sitemapXmlGeneratorFactory;
        private ISitemapXmlGenerator _currentGenerator;

        private bool _stopSignaled;

        public SitemapCreateJob()
        {
            IsStoppable = true;

            this._sitemapRepository = ServiceLocator.Current.GetInstance<ISitemapRepository>();
            this._sitemapXmlGeneratorFactory = ServiceLocator.Current.GetInstance<SitemapXmlGeneratorFactory>();
        }

        public override string Execute()
        {
            var results = new List<bool>();
            OnStatusChanged("Starting generation of sitemaps");
            var message = new StringBuilder();

            var sitemapConfigs = _sitemapRepository.GetAllSitemapData();

            // if no configuration present create one with default values
            if (sitemapConfigs.Count == 0)
            {
                _sitemapRepository.Save(CreateDefaultConfig());
            }

            CacheManager.Insert("SitemapGenerationKey", DateTime.Now.Ticks);

            // create xml sitemap for each configuration
            foreach (var sitemapConfig in sitemapConfigs)
            {
                if (_stopSignaled)
                {
                    CacheManager.Remove("SitemapGenerationKey");
                    return "Stop of job was called.";
                }

                OnStatusChanged($"Generating {sitemapConfig.SiteUrl}{_sitemapRepository.GetHostWithLanguage(sitemapConfig)}.");
                results.Add(GenerateSitemaps(sitemapConfig, message));
            }

            CacheManager.Remove("SitemapGenerationKey");

            if (_stopSignaled)
            {
                return "Stop of job was called.";
            }

            if (results.Any(x => !x))
            {
                throw new Exception($"Job executed with errors.<br/>{message}");
            }

            return $"Job successfully executed.<br/>{message}";
        }

        private bool GenerateSitemaps(SitemapData sitemapConfig, StringBuilder message)
        {
            _currentGenerator = _sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapConfig);
            var success = _currentGenerator.Generate(sitemapConfig, true, out var entryCount);

            var sitemapDisplayName = $"{sitemapConfig.SiteUrl}{_sitemapRepository.GetHostWithLanguage(sitemapConfig)}";
            var resultText = success ? $"Success - {entryCount} entries included" : "An error occured while generating sitemap";

            message.Append($"<br/>{sitemapDisplayName}: {resultText}");

            return success;
        }

        private static SitemapData CreateDefaultConfig()
        {
            var blankConfig = new SitemapData
            {
                Host = "sitemap.xml",
                IncludeDebugInfo = false,
                SitemapFormat = SitemapFormat.Standard
            };

            return blankConfig;
        }

        public override void Stop()
        {
            _stopSignaled = true;

            if (_currentGenerator != null)
            {
                _currentGenerator.Stop();
            }

            base.Stop();
        }
    }
}
