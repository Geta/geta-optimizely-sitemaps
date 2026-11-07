// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using EPiServer.Applications;

namespace Geta.Optimizely.Sitemaps.Utils
{
    internal static class ApplicationHostAuthorityExtensions
    {
        /// <summary>
        /// A host keeps its authority as it was typed, while a URL's authority is lowercase and leaves out the scheme's default port.
        /// </summary>
        public static bool HasAuthority(this ApplicationHost host, string authority)
        {
            return string.Equals(authority, host.Authority, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(authority, host.Url?.Authority, StringComparison.OrdinalIgnoreCase);
        }
    }
}
