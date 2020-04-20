// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Microsoft.SyndicationFeed.Rss
{
    public class RssParser : ISyndicationFeedParser
    {
        public ISyndicationCategory ParseCategory(string value)
        {
            ISyndicationContent content = ParseContent(value);

            if (content.Name != RssElementNames.Category || 
                content.Namespace != RssConstants.Rss20Namespace)
            {
                throw new FormatException("Invalid Rss category");
            }
            
            return CreateCategory(content);
        }

        public ISyndicationItem ParseItem(string value)
        {
            ISyndicationContent content = ParseContent(value);

            if (content.Name != RssElementNames.Item || 
                content.Namespace != RssConstants.Rss20Namespace)
            {
                throw new FormatException("Invalid Rss item");
            }

            return CreateItem(content);
        }

        public ISyndicationLink ParseLink(string value)
        {
            ISyndicationContent content = ParseContent(value);

            if (content.Name != RssElementNames.Link || 
                content.Namespace != RssConstants.Rss20Namespace)
            {
                throw new FormatException("Invalid Rss link");
            }

            return CreateLink(content);
        }

        public ISyndicationPerson ParsePerson(string value)
        {
            ISyndicationContent content = ParseContent(value);

            if ((content.Name != RssElementNames.Author && 
                 content.Name != RssElementNames.ManagingEditor) ||
                content.Namespace != RssConstants.Rss20Namespace)
            {
                throw new FormatException("Invalid Rss Person");
            }

            return CreatePerson(content);
        }

        public ISyndicationImage ParseImage(string value)
        {
            ISyndicationContent content = ParseContent(value);

            if (content.Name != RssElementNames.Image ||
                content.Namespace != RssConstants.Rss20Namespace)
            {
                throw new FormatException("Invalid Rss Image");
            }

            return CreateImage(content);
        }

        public ISyndicationContent ParseContent(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            using (XmlReader reader = XmlUtils.CreateXmlReader(value))
            {
                reader.MoveToContent();

                return ReadSyndicationContent(reader);
            }
        }

        public virtual bool TryParseValue<T>(string value, out T result)
        {
            return Converter.TryParseValue<T>(value, out result);
        }

        public virtual ISyndicationItem CreateItem(ISyndicationContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var item = new SyndicationItem();

            foreach (var field in content.Fields)
            {
                if (field.Namespace != RssConstants.Rss20Namespace)
                {
                    continue;
                }

                switch (field.Name)
                {
                    //
                    // Title
                    case RssElementNames.Title:
                        item.Title = field.Value;
                        break;

                    //
                    // Link
                    case RssElementNames.Link:
                        item.AddLink(CreateLink(field));
                        break;

                    // Description
                    case RssElementNames.Description:
                        item.Description = field.Value;
                        break;

                    //
                    // Author
                    case RssElementNames.Author:
                        item.AddContributor(CreatePerson(field));
                        break;

                    //
                    // Category
                    case RssElementNames.Category:
                        item.AddCategory(CreateCategory(field));
                        break;

                    //
                    // Links
                    case RssElementNames.Comments:
                    case RssElementNames.Enclosure:
                    case RssElementNames.Source:
                        item.AddLink(CreateLink(field));
                        break;

                    //
                    // Guid
                    case RssElementNames.Guid:
                        item.Id = field.Value;

                        // isPermaLink
                        string isPermaLinkAttr = field.Attributes.GetRss(RssConstants.IsPermaLink);

                        if ((isPermaLinkAttr == null || (TryParseValue(isPermaLinkAttr, out bool isPermalink) && isPermalink)) &&
                            TryParseValue(field.Value, out Uri permaLink))
                        {
                            item.AddLink(new SyndicationLink(permaLink, RssLinkTypes.Guid));
                        }

                        break;

                    //
                    // PubDate
                    case RssElementNames.PubDate:
                        if (TryParseValue(field.Value, out DateTimeOffset dt))
                        {
                            item.Published = dt;
                        }
                        break;

                    default:
                        break;
                }
            }

            return item;
        }

        public virtual ISyndicationLink CreateLink(ISyndicationContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            //
            // Title
            string title = content.Value;

            //
            // Url
            Uri uri = null;
            string url = content.Attributes.GetRss("url");

            if (url != null)
            {
                if (!TryParseValue(url, out uri))
                {
                    throw new FormatException("Invalid url attribute");
                }
            }
            else
            {
                if (!TryParseValue(content.Value, out uri))
                {
                    throw new FormatException("Invalid url");
                }

                title = null;
            }

            //
            // Length
            long length = 0;
            TryParseValue(content.Attributes.GetRss("length"), out length);

            //
            // Type
            string type = content.Attributes.GetRss("type");
            
            //
            // rel
            string rel = (content.Name == RssElementNames.Link) ? RssLinkTypes.Alternate : content.Name;

            return new SyndicationLink(uri, rel)
            {
                Title = title,
                Length = length,
                MediaType = type
            };
        }

        public virtual ISyndicationPerson CreatePerson(ISyndicationContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (string.IsNullOrEmpty(content.Value))
            {
                throw new ArgumentNullException("Content value is required");
            }

            //
            // Handle real name parsing
            // Ex: <author>abc@def.com (John Doe)</author>

            string email = content.Value;
            string name = null;

            int nameStart = content.Value.IndexOf('(');

            if (nameStart != -1)
            {
                int end = content.Value.IndexOf(')');

                if (end == -1 || end - nameStart - 1 < 0)
                {
                    throw new FormatException("Invalid Rss person");
                }

                email = content.Value.Substring(0, nameStart).Trim();

                name = content.Value.Substring(nameStart + 1, end - nameStart - 1);
            }

            return new SyndicationPerson(name, email, content.Name);
        }

        public virtual ISyndicationImage CreateImage(ISyndicationContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            string title = null;
            string description = null;
            Uri url = null;
            ISyndicationLink link = null;

            foreach (var field in content.Fields)
            {
                if (field.Namespace != RssConstants.Rss20Namespace)
                {
                    continue;
                }

                switch (field.Name)
                {
                    //
                    // Title
                    case RssElementNames.Title:
                        title = field.Value;
                        break;
                        
                    //
                    // Url
                    case RssElementNames.Url:
                        if (!TryParseValue(field.Value, out url))
                        {
                            throw new FormatException($"Invalid image url '{field.Value}'");
                        }
                        break;

                    //
                    // Link
                    case RssElementNames.Link:
                        link = CreateLink(field);
                        break;
                        
                    //
                    // Description
                    case RssElementNames.Description:
                        description = field.Value;
                        break;

                    default:
                        break;
                }
            }
  
            if (url == null)
            {
                throw new FormatException("Image url not found");
            }

            return new SyndicationImage(url, RssElementNames.Image)
            {
                Title = title,
                Description = description,
                Link = link
            };
        }

        public virtual ISyndicationCategory CreateCategory(ISyndicationContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (content.Value == null)
            {
                throw new FormatException("Invalid Rss category name");
            }

            return new SyndicationCategory(content.Value) {
                Scheme = content.Attributes.GetRss(RssConstants.Domain)
            };
        }

        private static ISyndicationContent ReadSyndicationContent(XmlReader reader)
        {
            var content = new SyndicationContent(reader.LocalName, reader.NamespaceURI, null);

            //
            // Attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    ISyndicationAttribute attr = reader.ReadSyndicationAttribute();

                    if (attr != null)
                    {
                        content.AddAttribute(attr);
                    }
                }

                reader.MoveToContent();
            }

            //
            // Body (special case to read XHTML bodies as a value)
            if (reader.LocalName == RssElementNames.Body)
            {
                // Read the OuterXml to make sure the namespace is in scope for nested elements
                content.Value = reader.ReadOuterXml();

                // Trim the outer <body> element
                int startIndex = content.Value.IndexOf('>') + 1;
                int endIndex = content.Value.LastIndexOf('<');
                content.Value = content.Value.Substring(startIndex, endIndex - startIndex);

                return content;
            }

            //
            // Content
            if (!reader.IsEmptyElement)
            {
                reader.ReadStartElement();

                //
                // Value
                if (reader.HasValue)
                {
                    content.Value = reader.ReadContentAsString();
                }
                //
                // Children
                else
                {
                    while (reader.IsStartElement())
                    {
                        content.AddField(ReadSyndicationContent(reader));
                    }
                }

                reader.ReadEndElement(); // end
            }
            else
            {
                reader.Skip();
            }

            return content;
        }
    }

    static class RssAttributeExtentions
    {
        public static string GetRss(this IEnumerable<ISyndicationAttribute> attributes, string name)
        {
            return attributes.FirstOrDefault(a => a.Name == name && 
                                            (a.Namespace == RssConstants.Rss20Namespace || a.Namespace == null))?.Value;
        }
    }
}