﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Microsoft.SyndicationFeed.Atom
{
    public class AtomFormatter : ISyndicationFeedFormatter
    {
        private readonly XmlWriter _writer;
        private readonly StringBuilder _buffer;

        public AtomFormatter()
            : this(null, null)
        {
        }

        public AtomFormatter(IEnumerable<ISyndicationAttribute> knownAttributes, XmlWriterSettings settings)
        {
            _buffer = new StringBuilder();
            _writer = XmlUtils.CreateXmlWriter(settings?.Clone() ?? new XmlWriterSettings(), 
                                               EnsureAtomNs(knownAttributes ?? Enumerable.Empty<ISyndicationAttribute>()), 
                                               _buffer);
        }

        public bool UseCDATA { get; set; }

        public string Format(ISyndicationContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            try
            {
                WriteSyndicationContent(content);

                _writer.Flush();

                return _buffer.ToString();
            }
            finally
            {
                _buffer.Clear();
            }
        }

        public string Format(ISyndicationCategory category)
        {
            return Format(CreateContent(category));
        }
        
        public string Format(ISyndicationImage image)
        {
            return Format(CreateContent(image));
        }

        public string Format(ISyndicationPerson person)
        {
            return Format(CreateContent(person));
        }

        public string Format(ISyndicationItem item)
        {
            return Format(CreateContent(item));
        }

        public string Format(IAtomEntry entry)
        {
            return Format(CreateContent(entry));
        }

        public string Format(ISyndicationLink link)
        {
            return Format(CreateContent(link));
        }

        public virtual string FormatValue<T>(T value)
        {
            if (value == null)
            {
                return null;
            }

            Type type = typeof(T);

            //
            // DateTimeOffset
            if (type == typeof(DateTimeOffset))
            {
                return DateTimeUtils.ToRfc3339String((DateTimeOffset)(object)value);
            }

            //
            // DateTime
            if (type == typeof(DateTime))
            {
                return DateTimeUtils.ToRfc3339String(new DateTimeOffset((DateTime)(object)value));
            }

            return value.ToString();
        }

        public virtual ISyndicationContent CreateContent(ISyndicationLink link)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (link.Uri == null)
            {
                throw new ArgumentNullException("Uri");
            }

            switch (link.RelationshipType)
            {
                case AtomLinkTypes.Content:
                    return CreateFromContentLink(link);

                case AtomLinkTypes.Source:
                    return CreateFromSourceLink(link);

                default:
                    return CreateFromLink(link);
            }
        }

        public virtual ISyndicationContent CreateContent(ISyndicationCategory category)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            if (string.IsNullOrEmpty(category.Name))
            {
                throw new ArgumentNullException("Name");
            }

            var result = new SyndicationContent(AtomElementNames.Category);

            //
            // term
            result.AddAttribute(new SyndicationAttribute(AtomConstants.Term, category.Name));

            //
            // scheme
            if (!string.IsNullOrEmpty(category.Scheme))
            {
                result.AddAttribute(new SyndicationAttribute(AtomConstants.Scheme, category.Scheme));
            }

            //
            // label
            if (!string.IsNullOrEmpty(category.Label))
            {
                result.AddAttribute(new SyndicationAttribute(AtomConstants.Label, category.Label));
            }

            return result;
        }

        public virtual ISyndicationContent CreateContent(ISyndicationPerson person)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            if (string.IsNullOrEmpty(person.Name))
            {
                throw new ArgumentNullException("Name");
            }

            string contributorType = person.RelationshipType ?? AtomContributorTypes.Author;

            if (contributorType != AtomContributorTypes.Author &&
                contributorType != AtomContributorTypes.Contributor)
            {
                throw new ArgumentException("RelationshipType");
            }

            var result = new SyndicationContent(contributorType);

            //
            // name
            result.AddField(new SyndicationContent(AtomElementNames.Name, person.Name));

            //
            // email
            if (!string.IsNullOrEmpty(person.Email))
            {
                result.AddField(new SyndicationContent(AtomElementNames.Email, person.Email));
            }

            //
            // uri
            if (person.Uri != null)
            {
                result.AddField(new SyndicationContent(AtomElementNames.Uri, FormatValue(person.Uri)));
            }

            return result;
        }

        public virtual ISyndicationContent CreateContent(ISyndicationImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            if (image.Url == null)
            {
                throw new ArgumentNullException("Url");
            }

            return new SyndicationContent(!string.IsNullOrEmpty(image.RelationshipType) ? image.RelationshipType : AtomImageTypes.Icon, 
                                          FormatValue(image.Url));
        }

        public virtual ISyndicationContent CreateContent(ISyndicationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (string.IsNullOrEmpty(item.Id))
            {
                throw new ArgumentNullException("Id");
            }

            if (string.IsNullOrEmpty(item.Title))
            {
                throw new ArgumentNullException("Title");
            }

            if (item.LastUpdated == default(DateTimeOffset))
            {
                throw new ArgumentException("LastUpdated");
            }

            var result = new SyndicationContent(AtomElementNames.Entry);

            //
            // id
            result.AddField(new SyndicationContent(AtomElementNames.Id, item.Id));

            //
            // title
            result.AddField(new SyndicationContent(AtomElementNames.Title, item.Title));

            //
            // updated
            result.AddField(new SyndicationContent(AtomElementNames.Updated, FormatValue(item.LastUpdated)));

            //
            // published
            if (item.Published != default(DateTimeOffset))
            {
                result.AddField(new SyndicationContent(AtomElementNames.Published, FormatValue(item.Published)));
            }

            //
            // link
            bool hasContentLink = false;
            bool hasAlternateLink = false;

            if (item.Links != null)
            {
                foreach (var link in item.Links)
                {
                    if (link.RelationshipType == AtomLinkTypes.Content)
                    {
                        if (hasContentLink)
                        {
                            throw new ArgumentNullException("Multiple content links are not allowed");
                        }

                        hasContentLink = true;
                    }
                    else if (link.RelationshipType == null || link.RelationshipType == AtomLinkTypes.Alternate)
                    {
                        hasAlternateLink = true;
                    }

                    result.AddField(CreateContent(link));
                }
            }

            if (item.Contributors != null)
            {
                foreach (var c in item.Contributors)
                {
                    result.AddField(CreateContent(c));
                }
            }

            //
            // category
            if (item.Categories != null)
            {
                foreach (var category in item.Categories)
                {
                    result.AddField(CreateContent(category));
                }
            }

            IAtomEntry entry = item as IAtomEntry;

            //
            // content
            if (!string.IsNullOrEmpty(item.Description))
            {
                if (hasContentLink)
                {
                    throw new ArgumentException("Description and content link are not allowed simultaneously");
                }

                var content = new SyndicationContent(AtomElementNames.Content, item.Description);

                //
                // type
                if (entry != null &&
                    !(string.IsNullOrEmpty(entry.ContentType) || entry.ContentType.Equals(AtomConstants.PlainTextContentType, StringComparison.OrdinalIgnoreCase)))
                {
                    content.AddAttribute(new SyndicationAttribute(AtomConstants.Type, entry.ContentType));
                }

                result.AddField(content);
            }
            else
            {
                if (!(hasContentLink || hasAlternateLink))
                {
                    throw new ArgumentException("Description or alternate link is required");
                }
            }

            if (entry != null)
            {
                //
                // summary
                if (!string.IsNullOrEmpty(entry.Summary))
                {
                    result.AddField(new SyndicationContent(AtomElementNames.Summary, entry.Summary));
                }

                //
                // rights
                if (!string.IsNullOrEmpty(entry.Rights))
                {
                    result.AddField(new SyndicationContent(AtomElementNames.Rights, entry.Rights));
                }
            }

            return result;
        }

        private ISyndicationContent CreateFromLink(ISyndicationLink link)
        {
            //
            // link
            var result = new SyndicationContent(AtomElementNames.Link);

            //
            // title
            if (!string.IsNullOrEmpty(link.Title))
            {
                result.AddAttribute(new SyndicationAttribute(AtomElementNames.Title, link.Title));
            }

            //
            // href
            result.AddAttribute(new SyndicationAttribute(AtomConstants.Href, FormatValue(link.Uri)));

            //
            // rel
            if (!string.IsNullOrEmpty(link.RelationshipType))
            {
                result.AddAttribute(new SyndicationAttribute(AtomConstants.Rel, link.RelationshipType));
            }

            //
            // type
            if (!string.IsNullOrEmpty(link.MediaType))
            {
                result.AddAttribute(new SyndicationAttribute(AtomConstants.Type, link.MediaType));
            }

            //
            // length
            if (link.Length > 0)
            {
                result.AddAttribute(new SyndicationAttribute(AtomConstants.Length, FormatValue(link.Length)));
            }

            return result;
        }

        private ISyndicationContent CreateFromContentLink(ISyndicationLink link)
        {
            //
            // content
            var result = new SyndicationContent(AtomElementNames.Content);

            //
            // src
            result.AddAttribute(new SyndicationAttribute(AtomConstants.Source, FormatValue(link.Uri)));

            //
            // type
            if (!string.IsNullOrEmpty(link.MediaType))
            {
                result.AddAttribute(new SyndicationAttribute(AtomConstants.Type, link.MediaType));
            }

            return result;
        }

        private ISyndicationContent CreateFromSourceLink(ISyndicationLink link)
        {
            //
            // source
            var result = new SyndicationContent(AtomElementNames.Source);

            //
            // title
            if (!string.IsNullOrEmpty(link.Title))
            {
                result.AddField(new SyndicationContent(AtomElementNames.Title, link.Title));
            }

            //
            // link
            result.AddField(CreateFromLink(new SyndicationLink(link.Uri)
            {
                MediaType = link.MediaType,
                Length = link.Length
            }));

            //
            // updated
            if (link.LastUpdated != default(DateTimeOffset))
            {
                result.AddField(new SyndicationContent(AtomElementNames.Updated, FormatValue(link.LastUpdated)));
            }

            return result;
        }

        private void WriteSyndicationContent(ISyndicationContent content)
        {
            string type = null;

            //
            // Write Start
            _writer.WriteStartSyndicationContent(content, AtomConstants.Atom10Namespace);

            //
            // Write attributes
            if (content.Attributes != null)
            {
                foreach (var a in content.Attributes)
                {
                    if (type == null && a.Name == AtomConstants.Type)
                    {
                        type = a.Value;
                    }

                    _writer.WriteSyndicationAttribute(a);
                }
            }

            //
            // Write value
            if (content.Value != null)
            {
                //
                // Xhtml
                if (XmlUtils.IsXhtmlMediaType(type) && content.IsAtom())
                {
                    _writer.WriteStartElement("div", AtomConstants.XhtmlNamespace);
                    _writer.WriteXmlFragment(content.Value, AtomConstants.XhtmlNamespace);
                    _writer.WriteEndElement();
                }
                //
                // Xml (applies to <content>)
                else if (XmlUtils.IsXmlMediaType(type) && content.IsAtom(AtomElementNames.Content))
                {
                    _writer.WriteXmlFragment(content.Value, string.Empty);
                }
                //
                // Text/Html
                else
                {
                    _writer.WriteString(content.Value, UseCDATA);
                }
            }
            //
            // Write Fields
            else
            {
                if (content.Fields != null)
                {
                    foreach (var field in content.Fields)
                    {
                        WriteSyndicationContent(field);
                    }
                }
            }

            //
            // Write End
            _writer.WriteEndElement();
        }

        private static IEnumerable<ISyndicationAttribute> EnsureAtomNs(IEnumerable<ISyndicationAttribute> attributes)
        {
            //
            // Insert Atom namespace if it doesn't already exist
            if (!attributes.Any(a => a.Name.StartsWith("xmlns") && 
                a.Value == AtomConstants.Atom10Namespace))
            {
                var list = new List<ISyndicationAttribute>(attributes);
                list.Insert(0, new SyndicationAttribute("xmlns", AtomConstants.Atom10Namespace));

                attributes = list;
            }

            return attributes;
        }
    }
}
