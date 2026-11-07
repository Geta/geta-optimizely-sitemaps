// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EPiServer.Scheduler;
using Geta.Optimizely.Sitemaps.Entities;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Utils;
using Geta.Optimizely.Sitemaps.XML;

namespace Geta.Optimizely.Sitemaps
{
    [ScheduledJob(GUID = "EC74D2A3-9D77-4265-B4FF-A1935E3C3110", DisplayName = "Generate search engine sitemaps")]
    public class SitemapCreateJob : ScheduledJobBase
    {
        private readonly ISitemapRepository _sitemapRepository;
        private readonly SitemapXmlGeneratorFactory _sitemapXmlGeneratorFactory;
        private ISitemapXmlGenerator _currentGenerator;

        private bool _stopSignaled;

        public SitemapCreateJob(
            ISitemapRepository sitemapRepository,
            SitemapXmlGeneratorFactory sitemapXmlGeneratorFactory)
        {
            IsStoppable = true;

            _sitemapRepository = sitemapRepository;
            _sitemapXmlGeneratorFactory = sitemapXmlGeneratorFactory;
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
                sitemapConfigs = _sitemapRepository.GetAllSitemapData();
            }

            // create xml sitemap for each configuration
            foreach (var sitemapConfig in sitemapConfigs)
            {
                if (_stopSignaled)
                {
                    break;
                }

                OnStatusChanged($"Generating {_sitemapRepository.GetSitemapUrl(sitemapConfig)}.");
                results.Add(GenerateSitemaps(sitemapConfig, message));
            }

            if (_stopSignaled)
            {
                return $"Stop of job was called.<br/>{message}";
            }

            if (results.Any(x => !x))
            {
                throw new Exception($"Job executed with errors.<br/>{message}");
            }

            return $"Job successfully executed.<br/>{message}";
        }

        private bool GenerateSitemaps(SitemapData sitemapConfig, StringBuilder message)
        {
            var success = false;
            string resultText;

            // One sitemap that can't be generated, such as a Commerce format without AddSitemapsCommerce, must not stop the others.
            try
            {
                _currentGenerator = _sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapConfig);
                success = _currentGenerator.Generate(sitemapConfig, true, out var entryCount);
                resultText = success ? GetSuccessText(sitemapConfig, entryCount) : GetErrorText(_currentGenerator.LastError);
            }
            catch (Exception e)
            {
                resultText = GetErrorText(e.Message);
            }

            // Generating can have filled in the site URL.
            message.Append($"<br/>{_sitemapRepository.GetSitemapUrl(sitemapConfig)}: {resultText}");

            return success;
        }

        private static string GetSuccessText(SitemapData sitemapConfig, int entryCount)
        {
            var text = $"Success - {entryCount} entries included";

            if (sitemapConfig.ExceedsMaximumEntryCount)
            {
                return $"{text}. This is the maximum for one sitemap, so the remaining content was left out. Split it over several sitemaps by root page or paths.";
            }

            return entryCount == 0
                ? $"{text}. Check the sitemap's root page, paths, language and host. Only published content that the Everyone role can read is included."
                : text;
        }

        private static string GetErrorText(string lastError)
        {
            const string error = "An error occurred while generating sitemap";

            return string.IsNullOrEmpty(lastError) ? error : $"{error}: {lastError}";
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
