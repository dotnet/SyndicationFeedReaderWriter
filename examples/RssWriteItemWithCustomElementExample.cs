﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;


/// <summary>
/// Create a SyndicationItem and add a custom field.
/// </summary>
class RssWriteItemWithCustomElement
{
    public static async Task WriteCustomItem()
    {
        const string ExampleNs = "http://contoso.com/syndication/feed/examples";
        var sw = new StringWriterWithEncoding(Encoding.UTF8);

        using (XmlWriter xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings() { Async = true, Indent = true }))
        {
            var attributes = new List<SyndicationAttribute>()
            {
                new SyndicationAttribute("xmlns:example", ExampleNs)
            };

            var formatter = new RssFormatter(attributes, xmlWriter.Settings);
            var writer = new RssFeedWriter(xmlWriter, attributes, formatter);
              
            // Create item
            var item = new SyndicationItem()
            {
                Title = "Rss Writer Available",
                Description = "The new RSS Writer is now available as a NuGet package!",
                Id = "https://www.nuget.org/packages/Microsoft.SyndicationFeed",
                Published = DateTimeOffset.UtcNow
            };

            item.AddCategory(new SyndicationCategory("Technology"));
            item.AddContributor(new SyndicationPerson("test", "test@mail.com"));

            //
            // Format the item as SyndicationContent
            var content = new SyndicationContent(formatter.CreateContent(item));

            // Add custom fields/attributes
            content.AddField(new SyndicationContent("customElement", ExampleNs, "Custom Value"));

            // Write 
            await writer.Write(content);

            // Done
            xmlWriter.Flush();
        }

        Console.WriteLine(sw.ToString());
    }
    
    class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding _encoding;

        public StringWriterWithEncoding(Encoding encoding)
        {
            this._encoding = encoding;
        }

        public override Encoding Encoding {
            get { return _encoding; }
        }
    }
}
