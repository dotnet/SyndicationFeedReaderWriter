// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace Microsoft.SyndicationFeed
{
    static class XmlExtentions
    {
        public static ISyndicationAttribute ReadSyndicationAttribute(this XmlReader reader)
        {
            string ns = reader.NamespaceURI;
            string name = reader.Name;

            if (XmlUtils.IsXmlns(name, ns) || XmlUtils.IsXmlSchemaType(name, ns))
            {
                return null;
            }

            return new SyndicationAttribute(name, ns, reader.Value);
        }


        public static void WriteStartSyndicationContent(this XmlWriter writer, ISyndicationContent content, string defaultNs)
        {
            string ns = content.Namespace ?? defaultNs;

            if (ns != null)
            {
                XmlUtils.SplitName(content.Name, out string prefix, out string localName);

                prefix = writer.LookupPrefix(ns) ?? prefix;

                if (prefix != null)
                {
                    writer.WriteStartElement(prefix, localName, ns);
                }
                else
                {
                    writer.WriteStartElement(localName, ns);
                }
            }
            else
            {
                writer.WriteStartElement(content.Name);
            }
        }

        public static void WriteSyndicationAttribute(this XmlWriter writer, ISyndicationAttribute attr)
        {
            XmlUtils.SplitName(attr.Name, out string prefix, out string localName);

            writer.WriteAttribute(prefix, attr.Name, localName, attr.Namespace, attr.Value);
        }

        public static void WriteXmlFragment(this XmlWriter writer, string fragment, string defaultNs)
        {
            using (var reader = XmlUtils.CreateXmlReader(fragment))
            {
                reader.MoveToContent();

                while (!reader.EOF)
                {
                    string ns = !string.IsNullOrEmpty(reader.NamespaceURI) ? reader.NamespaceURI : defaultNs;

                    //
                    // Start Element
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        writer.WriteStartElement(reader.LocalName, ns);

                        if (reader.HasAttributes)
                        {
                            while (reader.MoveToNextAttribute())
                            {
                                writer.WriteAttribute(reader.Prefix, reader.Name, reader.LocalName, ns, reader.Value);
                            }

                            reader.MoveToContent();
                        }

                        if (reader.IsEmptyElement)
                        {
                            writer.WriteEndElement();
                        }

                        reader.Read();
                        continue;
                    }

                    //
                    // End Element
                    if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        writer.WriteEndElement();
                        reader.Read();
                        continue;
                    }

                    //
                    // Copy Content
                    writer.WriteNode(reader, false);
                }
            }
        }

        private static void WriteAttribute(this XmlWriter writer, string prefix, string name, string localName, string ns, string value)
        {
            prefix = prefix ?? writer.LookupPrefix(ns ?? string.Empty);

            if (prefix == string.Empty)
            {
                writer.WriteStartAttribute(name);
            }
            else if (prefix != null)
            {
                writer.WriteStartAttribute(prefix, localName, ns);
            }
            else
            {
                writer.WriteStartAttribute(localName, ns);
            }

            writer.WriteString(value);
            writer.WriteEndAttribute();
        }
    }
}
