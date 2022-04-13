// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

/*
 * Code below originally comes from https://www.coderesort.com/p/epicode/wiki/SearchEngineSitemaps
 * Author: Jacob Khan
 */

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using EPiServer.Core;
using EPiServer.PlugIn;

namespace Geta.Optimizely.Sitemaps.SpecializedProperties
{
    [PropertyDefinitionTypePlugIn(DisplayName = "SEOSitemaps")]
    public class PropertySEOSitemaps : PropertyString
    {
        public const string PropertyName = "SEOSitemaps";
        public string ChangeFreq { get; set; } = "weekly";
        public bool Enabled { get; set; } = true;
        public string Priority { get; set; } = "0.5";

        [XmlIgnore]
        protected override string String
        {
            get => base.String;

            set
            {
                Deserialize(value);
                base.String = value;
            }
        }

        public void Deserialize(string xml)
        {
            var s = new StringReader(xml);
            var reader = new XmlTextReader(s);

            reader.ReadStartElement(PropertyName);

            Enabled = bool.Parse(reader.ReadElementString("enabled"));
            ChangeFreq = reader.ReadElementString("changefreq");
            Priority = reader.ReadElementString("priority");

            reader.ReadEndElement();

            reader.Close();
        }

        public void Serialize()
        {
            var s = new StringWriter();
            var writer = new XmlTextWriter(s);

            writer.WriteStartElement(PropertyName);

            writer.WriteElementString("enabled", Enabled.ToString());
            writer.WriteElementString("changefreq", ChangeFreq);
            writer.WriteElementString("priority", Priority);

            writer.WriteEndElement();

            writer.Flush();
            writer.Close();

            String = s.GetStringBuilder().ToString();
        }
    }
}
