// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using Geta.Optimizely.Sitemaps.Entities;

namespace Geta.Optimizely.Sitemaps.Utils
{
    /// <summary>
    /// Administrators are able to specify specific paths to include (whitelist) or exclude (blacklist) in sitemaps.
    /// This class is used to check this.
    /// </summary>
    public static class UrlFilter
    {
        public static bool IsUrlFiltered(string url, SitemapData sitemapConfig)
        {
            // URL is removed if it fails whitelist or fails blacklist checks
            return !IsAllowedByWhitelist(url, sitemapConfig.PathsToInclude) ||
                   !IsAllowedByBlacklist(url, sitemapConfig.PathsToAvoid);
        }

        private static bool IsAllowedByWhitelist(string url, IList<string> whitelist)
        {
            if (whitelist == null || whitelist.Count == 0)
            {
                // if whitelist is empty, then everything is allowed
                return true; 
            }

            // otherwise - url has to match at least one path
            return whitelist.Any(path => IsMatch(url, path));
        }

        private static bool IsAllowedByBlacklist(string url, IList<string> blacklist)
        {
            if (blacklist == null || blacklist.Count == 0)
            {
                // if blacklist is empty, then everything is allowed
                return true; 
            }

            // otherwise - url can not match any of the paths 
            return blacklist.All(path => !IsMatch(url, path));
        }

        private static bool IsMatch(string url, string path)
        {
            var normalizedPath = NormalizePath(path);
            return url.ToLower().StartsWith(normalizedPath);
        }

        private static string NormalizePath(string path)
        {
            return "/" + path.ToLower().Trim().TrimStart('/').TrimEnd('/') + "/";
        }
    }
}
