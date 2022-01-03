// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

/// <summary>
/// Create an RSS 2.0 feed
/// </summary>
class CreateSimpleRssFeed
{
    public static async Task WriteFeed()
    {
        var sw = new StringWriterWithEncoding(Encoding.UTF8);

        using (XmlWriter xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings() { Async = true , Indent = true }))
        {
            var writer = new RssFeedWriter(xmlWriter);

            //
            // Add Title
            await writer.WriteTitle("Example of RssFeedWriter");

            //
            // Add Description
            await writer.WriteDescription("Hello World, RSS 2.0!");

            //
            // Add Link
            await writer.Write(new SyndicationLink(new Uri("https://github.com/dotnet/SyndicationFeedReaderWriter")));

            //
            // Add managing editor
            await writer.Write(new SyndicationPerson("managingeditor", "managingeditor@contoso.com", RssContributorTypes.ManagingEditor));

            //
            // Add publish date
            await writer.WritePubDate(DateTimeOffset.UtcNow);

            //
            // Add custom element
            var customElement = new SyndicationContent("customElement");

            customElement.AddAttribute(new SyndicationAttribute("attr1", "true"));
            customElement.AddField(new SyndicationContent("Company", "Contoso"));

            await writer.Write(customElement);

            //
            // Add Items
            for (int i = 0; i < 5; ++i)
            {
                var item = new SyndicationItem()
                {
                    Id = "https://www.nuget.org/packages/Microsoft.SyndicationFeed.ReaderWriter",
                    Title = $"Item #{i + 1}",
                    Description = "The new Microsoft.SyndicationFeed.ReaderWriter is now available as a NuGet package!",
                    Published = DateTimeOffset.UtcNow
                };

                item.AddLink(new SyndicationLink(new Uri("https://github.com/dotnet/SyndicationFeedReaderWriter")));
                item.AddCategory(new SyndicationCategory("Technology"));
                item.AddContributor(new SyndicationPerson("user", "user@contoso.com"));

                await writer.Write(item);
            }

            //
            // Done
            xmlWriter.Flush();
        }

        //
        // Ouput the feed
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
