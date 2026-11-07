// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Framework.Localization;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;

namespace Geta.Optimizely.Sitemaps.EditorDescriptors
{
    [EditorDescriptorRegistration(TargetType = typeof(string), UIHint = "SeoSitemap")]
    public class SeoSitemapEditorDescriptor : EditorDescriptor
    {
        private const string TranslationRoot = "/seositemaps/editor/";

        private static readonly Dictionary<string, string> DefaultLabels = new()
        {
            ["enabled"] = "Enabled",
            ["changefrequency"] = "Change frequency",
            ["priority"] = "Priority",
            ["always"] = "Always",
            ["hourly"] = "Hourly",
            ["daily"] = "Daily",
            ["weekly"] = "Weekly",
            ["monthly"] = "Monthly",
            ["yearly"] = "Yearly",
            ["never"] = "Never",
            ["low"] = "Low",
            ["medium"] = "Medium",
            ["mediumhigh"] = "Medium-High",
            ["high"] = "High"
        };

        public SeoSitemapEditorDescriptor()
        {
            ClientEditingClass = "seositemaps/Editor";
        }

        public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            base.ModifyMetadata(metadata, attributes);

            metadata.EditorConfiguration["labels"] = DefaultLabels.ToDictionary(
                x => x.Key,
                x => LocalizationService.Current.GetString(TranslationRoot + x.Key, x.Value));
        }
    }
}
