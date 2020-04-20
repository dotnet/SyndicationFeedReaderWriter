﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.SyndicationFeed.Rss;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace Microsoft.SyndicationFeed.Tests.Rss
{
    public class RssWriter
    {
        sealed class StringWriterWithEncoding : StringWriter
        {
            private readonly Encoding _encoding;

            public StringWriterWithEncoding(Encoding encoding)
            {
                this._encoding = encoding;
            }

            public override Encoding Encoding
            {
                get { return _encoding; }
            }
        }

        [Fact]
        public async Task WriteCategory()
        {
            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            var cat1 = new SyndicationCategory("Test Category 1") {
                Scheme = "http://example.com/test"
            };

            var cat2 = new SyndicationCategory("Test Category 2");

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                var writer = new RssFeedWriter(xmlWriter);

                await writer.Write(cat1);
                await writer.Write(cat2);
                await writer.Flush();
            }

            string res = sw.ToString();
            Assert.True(res == $"<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel><category domain=\"{cat1.Scheme}\">{cat1.Name}</category><category>{cat2.Name}</category></channel></rss>");
        }

        [Fact]
        public async Task WritePerson()
        {
            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            using (var xmlWriter = XmlWriter.Create(sw))
            {                
                var writer = new RssFeedWriter(xmlWriter);

                await writer.Write(new SyndicationPerson("John Doe", "author@email.com"));
                await writer.Write(new SyndicationPerson("John Smith", "mEditor@email.com", RssContributorTypes.ManagingEditor));

                await writer.Flush();
            }

            string res = sw.ToString();
            Assert.True(res == "<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel><author>author@email.com (John Doe)</author><managingEditor>mEditor@email.com (John Smith)</managingEditor></channel></rss>");
        }

        [Fact]
        public async Task WriteImage()
        {
            Uri uri = new Uri("http://testuriforlink.com");

            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                var writer = new RssFeedWriter(xmlWriter);

                await writer.Write(new SyndicationImage(uri)
                {
                    Title = "Testing image title",
                    Description = "testing image description",
                    Link = new SyndicationLink(uri)
                });

                await writer.Flush();
            }

            string res = sw.ToString();
            Assert.True(res == $"<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel><image><url>{uri}</url><title>Testing image title</title><link>{uri}</link><description>testing image description</description></image></channel></rss>");
        }

        [Fact]
        public async Task WriteLink_onlyUrl()
        {
            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                var writer = new RssFeedWriter(xmlWriter);
                
                await writer.Write(new SyndicationLink(new Uri("http://testuriforlink.com")));
                await writer.Flush();
            }

            string res = sw.ToString();
            Assert.True(res == "<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel><link>http://testuriforlink.com/</link></channel></rss>");
        }

        [Fact]
        public async Task WriteLink_allElements()
        {
            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            var link = new SyndicationLink(new Uri("http://testuriforlink.com"))
            {
                Title = "Test title",
                Length = 123,
                MediaType = "mp3/video"
            };

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                var writer = new RssFeedWriter(xmlWriter);

                await writer.Write(link);
                await writer.Flush();
            }

            string res = sw.ToString();
            Assert.True(res == $"<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel><link url=\"{link.Uri}\" type=\"{link.MediaType}\" length=\"{link.Length}\">{link.Title}</link></channel></rss>");
        }

             
        [Fact]
        public async Task WriteItem()
        {
            var url = new Uri("https://contoso.com/");

            // 
            // Construct item
            var item = new SyndicationItem()
            {
                Id = "https://contoso.com/28af09b3-86c7-4dd6-b56f-58aaa17cff62",
                Title = "First item on ItemWriter",
                Description = "Brief description of an item",
                Published = DateTimeOffset.UtcNow
            };

            item.AddLink(new SyndicationLink(url));
            item.AddLink(new SyndicationLink(url, RssLinkTypes.Enclosure)
            {
                Title = "https://contoso.com/",
                Length = 4123,
                MediaType = "audio/mpeg"
            });
            item.AddLink(new SyndicationLink(url, RssLinkTypes.Comments));
            item.AddLink(new SyndicationLink(url, RssLinkTypes.Source)
            {
                Title = "Anonymous Blog"
            });

            item.AddLink(new SyndicationLink(new Uri(item.Id), RssLinkTypes.Guid));

            item.AddContributor(new SyndicationPerson("John Doe", "person@email.com"));

            item.AddCategory(new SyndicationCategory("Test Category"));

            //
            // Write
            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                var writer = new RssFeedWriter(xmlWriter);

                await writer.Write(item);
                await writer.Flush();
            }

            string res = sw.ToString();
            Assert.True(res == $"<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel><item><title>First item on ItemWriter</title><link>{url}</link><enclosure url=\"{url}\" length=\"4123\" type=\"audio/mpeg\" /><comments>{url}</comments><source url=\"{url}\">Anonymous Blog</source><guid>{item.Id}</guid><description>Brief description of an item</description><author>person@email.com (John Doe)</author><category>Test Category</category><pubDate>{item.Published.ToRfc1123()}</pubDate></item></channel></rss>", res);
        }

        [Fact]
        public async Task WriteContent()
        {
            ISyndicationContent content = null;

            //
            // Read
            using (var xmlReader = XmlReader.Create(@"..\..\..\TestFeeds\CustomXml.xml"))
            {
                RssFeedReader reader = new RssFeedReader(xmlReader);
                content = await reader.ReadContent();
            }

            //
            // Write
            StringBuilder sb = new StringBuilder();

            using (var xmlWriter = XmlWriter.Create(sb))
            {
                var writer = new RssFeedWriter(xmlWriter);

                await writer.Write(content);
                await writer.Flush();
            }

            string res = sb.ToString();
            Assert.True(res == "<?xml version=\"1.0\" encoding=\"utf-16\"?><rss version=\"2.0\"><channel><NewItem><enclosure url=\"http://www.scripting.com/mp3s/weatherReportSuite.mp3\" length=\"12216320\" type=\"audio/mpeg\" /><title>Lorem ipsum 2017-07-06T20:25:00+00:00</title><description>Exercitation sit dolore mollit et est eiusmod veniam aute officia veniam ipsum.</description><link>http://example.com/test/1499372700</link><guid isPermaLink=\"true\">http://example.com/test/1499372700</guid><pubDate>Thu, 06 Jul 2017 20:25:00 GMT</pubDate></NewItem></channel></rss>");
        }

        [Fact]
        public async Task WriteValue()
        {
            var sb = new StringBuilder();

            using (var xmlWriter = XmlWriter.Create(sb))
            {
                var writer = new RssFeedWriter(xmlWriter);

                await writer.WriteValue("CustomTag", "Custom Content");
                await writer.Flush();
            }

            var res = sb.ToString();
            Assert.True(res == "<?xml version=\"1.0\" encoding=\"utf-16\"?><rss version=\"2.0\"><channel><CustomTag>Custom Content</CustomTag></channel></rss>");
        }

        [Fact]
        public async Task WriteCDATAValue()
        {
            var sb = new StringBuilder();

            using (var xmlWriter = XmlWriter.Create(sb))
            {
                var writer = new RssFeedWriter(xmlWriter, null, new RssFormatter() { UseCDATA = true });

                await writer.WriteTitle("<h1>HTML Title</h1>");
                await writer.Flush();
            }

            var res = sb.ToString();
            Assert.True(res == "<?xml version=\"1.0\" encoding=\"utf-16\"?><rss version=\"2.0\"><channel><title><![CDATA[<h1>HTML Title</h1>]]></title></channel></rss>");
        }

        [Fact]
        public async Task Echo()
        {
            string res = null;
            using (var xmlReader = XmlReader.Create(@"..\..\..\TestFeeds\rss20-2items.xml", new XmlReaderSettings() { Async = true }))
            {
                var reader = new RssFeedReader(xmlReader);

                var sw = new StringWriterWithEncoding(Encoding.UTF8);

                using (var xmlWriter = XmlWriter.Create(sw))
                {
                    var attributes = new SyndicationAttribute[] { new SyndicationAttribute("xmlns:a10", "http://www.w3.org/2005/Atom") };
                    var writer = new RssFeedWriter(xmlWriter, attributes);

                    while (await reader.Read())
                    {
                        switch (reader.ElementType)
                        {
                            case SyndicationElementType.Item:
                                await writer.Write(await reader.ReadItem());
                                break;

                            case SyndicationElementType.Person:
                                await writer.Write(await reader.ReadPerson());
                                break;

                            case SyndicationElementType.Image:
                                await writer.Write(await reader.ReadImage());
                                break;

                            default:
                                await writer.Write(await reader.ReadContent());
                                break;
                        }
                    }

                    await writer.Flush();
                }

                res = sw.ToString();
                Assert.True(res == "<?xml version=\"1.0\" encoding=\"utf-8\"?><rss xmlns:a10=\"http://www.w3.org/2005/Atom\" version=\"2.0\"><channel><title asd=\"123\">Lorem ipsum feed for an interval of 1 minutes</title><description>This is a constantly updating lorem ipsum feed</description><link length=\"123\" type=\"testType\">http://example.com/</link><image><url>http://2.bp.blogspot.com/-NA5Jb-64eUg/URx8CSdcj_I/AAAAAAAAAUo/eCx0irI0rq0/s1600/bg_Microsoft_logo3-20120824073001907469-620x349.jpg</url><title>Microsoft News</title><link>http://www.microsoft.com/news</link><description>Test description</description></image><generator>RSS for Node</generator><lastBuildDate>Thu, 06 Jul 2017 20:25:17 GMT</lastBuildDate><managingEditor>John Smith</managingEditor><pubDate>Thu, 06 Jul 2017 20:25:00 GMT</pubDate><copyright>Michael Bertolacci, licensed under a Creative Commons Attribution 3.0 Unported License.</copyright><ttl>60</ttl><item><title>Lorem ipsum 2017-07-06T20:25:00+00:00</title><enclosure url=\"http://www.scripting.com/mp3s/weatherReportSuite.mp3\" length=\"12216320\" type=\"audio/mpeg\" /><link>http://example.com/test/1499372700</link><guid>http://example.com/test/1499372700</guid><description>Exercitation sit dolore mollit et est eiusmod veniam aute officia veniam ipsum.</description><author>John Smith</author><pubDate>Thu, 06 Jul 2017 20:25:00 GMT</pubDate><a10:updated>Wed, 06 Dec 2017 20:25:00 GMT</a10:updated></item><item><title>Lorem ipsum 2017-07-06T20:24:00+00:00</title><link>http://example.com/test/1499372640</link><guid>http://example.com/test/1499372640</guid><enclosure url=\"http://www.scripting.com/mp3s/weatherReportSuite.mp3\" length=\"12216320\" type=\"audio/mpeg\" /><description>Do ipsum dolore veniam minim est cillum aliqua ea.</description><author>John Smith</author><pubDate>Thu, 06 Jul 2017 20:24:00 GMT</pubDate><a10:updated>Wed, 06 Dec 2017 20:25:00 GMT</a10:updated></item></channel></rss>");
            }

            await RssReader.TestReadFeedElements(XmlReader.Create(new StringReader(res)));
        }

        [Fact]
        public async Task CompareContents()
        {
            string filePath = @"..\..\..\TestFeeds\internetRssFeed.xml";
            string res = null;

            using (var xmlReader = XmlReader.Create(filePath, new XmlReaderSettings() { Async = true }))
            {
                var reader = new RssFeedReader(xmlReader);

                var sw = new StringWriterWithEncoding(Encoding.UTF8);

                using (var xmlWriter = XmlWriter.Create(sw))
                {
                    var writer = new RssFeedWriter(xmlWriter);

                    while (await reader.Read())
                    {
                        switch (reader.ElementType)
                        {
                            case SyndicationElementType.Item:
                                await writer.Write(await reader.ReadItem());
                                break;

                            case SyndicationElementType.Person:
                                await writer.Write(await reader.ReadPerson());
                                break;

                            case SyndicationElementType.Image:
                                await writer.Write(await reader.ReadImage());
                                break;

                            default:
                                await writer.Write(await reader.ReadContent());
                                break;
                        }
                    }

                    await writer.Flush();
                }

                res = sw.ToString();
            }

            await CompareFeeds(new RssFeedReader(XmlReader.Create(filePath)), 
                               new RssFeedReader(XmlReader.Create(new StringReader(res))));
            
        }

        private async Task CompareFeeds(ISyndicationFeedReader f1, ISyndicationFeedReader f2)
        {
            while (await f1.Read() && await f2.Read())
            {
                Assert.True(f1.ElementType == f2.ElementType);

                switch (f1.ElementType)
                {
                    case SyndicationElementType.Item:
                        CompareItem(await f1.ReadItem(), await f2.ReadItem());
                        break;

                    case SyndicationElementType.Person:
                        ComparePerson(await f1.ReadPerson(), await f2.ReadPerson());
                        break;

                    case SyndicationElementType.Image:
                        CompareImage(await f1.ReadImage(), await f2.ReadImage());
                        break;

                    default:
                        CompareContent(await f1.ReadContent(), await f2.ReadContent());
                        break;
                }
            }
        }

        [Fact]
        public async Task WriteNamespaces()
        {
            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            using (XmlWriter xmlWriter = XmlWriter.Create(sw))
            {
                var writer = new RssFeedWriter(xmlWriter, 
                                                 new SyndicationAttribute[] { new SyndicationAttribute("xmlns:content", "http://contoso.com/")});

                await writer.Write(new SyndicationContent("hello", "http://contoso.com/", "world"));
                await writer.Write(new SyndicationContent("world", "http://contoso.com/", "hello"));

                await writer.Flush();
            }

            string res = sw.ToString();
            Assert.True(res == "<?xml version=\"1.0\" encoding=\"utf-8\"?><rss xmlns:content=\"http://contoso.com/\" version=\"2.0\"><channel><content:hello>world</content:hello><content:world>hello</content:world></channel></rss>");
        }

        [Fact]
        public async Task WriteCloud()
        {
            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            using (XmlWriter xmlWriter = XmlWriter.Create(sw))
            {
                var writer = new RssFeedWriter(xmlWriter);

                await writer.WriteCloud(new Uri("http://podcast.contoso.com/rpc"), "xmlStorageSystem.rssPleaseNotify", "xml-rpc");

                await writer.Flush();
            }

            string res = sw.ToString();
            Assert.True(res == "<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel><cloud domain=\"podcast.contoso.com\" port=\"80\" path=\"/rpc\" registerProcedure=\"xmlStorageSystem.rssPleaseNotify\" protocol=\"xml-rpc\" /></channel></rss>");
        }

        [Fact]
        public async Task WriteSkipDays()
        {
            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            using (XmlWriter xmlWriter = XmlWriter.Create(sw))
            {
                var writer = new RssFeedWriter(xmlWriter);

                await writer.WriteSkipDays(new DayOfWeek[] { DayOfWeek.Friday, DayOfWeek.Monday });

                await writer.Flush();
            }

            string res = sw.ToString();
            Assert.True(res == "<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel><skipDays><day>Friday</day><day>Monday</day></skipDays></channel></rss>");
        }

        [Fact]
        public async Task WriteSkipHours()
        {
            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            using (XmlWriter xmlWriter = XmlWriter.Create(sw))
            {
                var writer = new RssFeedWriter(xmlWriter);

                await writer.WriteSkipHours(new byte[] { 0, 4, 1, 11, 23, 20 });

                await writer.Flush();
            }

            string res = sw.ToString();
            Assert.True(res == "<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel><skipHours><hour>0</hour><hour>4</hour><hour>1</hour><hour>11</hour><hour>23</hour><hour>20</hour></skipHours></channel></rss>");
        }

        void ComparePerson(ISyndicationPerson person1, ISyndicationPerson person2)
        {
            Assert.True(person1.Email == person2.Email);
            Assert.True(person1.RelationshipType == person2.RelationshipType);
        }

        void CompareImage(ISyndicationImage image1, ISyndicationImage image2)
        {
            Assert.True(image1.RelationshipType == image2.RelationshipType);
            Assert.True(image1.Url == image2.Url);
            Assert.True(image1.Link.Uri.ToString() == image2.Link.Uri.ToString());
            Assert.True(image1.Description == image2.Description);
        }

        void CompareItem(ISyndicationItem item1, ISyndicationItem item2)
        {
            Assert.True(item1.Id == item2.Id);
            Assert.True(item1.Title == item2.Title);
            Assert.True(item1.LastUpdated == item2.LastUpdated);

        }

        void CompareContent(ISyndicationContent content1, ISyndicationContent content2)
        {
            Assert.True(content1.Name == content2.Name);

            //Compare attributes
            foreach (var a in content1.Attributes)
            {
                var a2 = content2.Attributes.Single(att => att.Name == a.Name);
                Assert.True(a.Name == a2.Name);
                Assert.True(a.Namespace == a2.Namespace);
                Assert.True(a.Value == a2.Value);
            }

            //Compare fields
            foreach (var f in content1.Fields)
            {
                var f2 = content2.Fields.Single(field => field.Name == f.Name && field.Value == f.Value);
                CompareContent(f, f2);
            }

            Assert.True(content1.Value == content2.Value);
        }

        [Fact]
        public async Task FormatterWriterWithNamespaces()
        {
            const string ExampleNs = "http://contoso.com/syndication/feed/examples";
            var sw = new StringWriter();

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                var attributes = new SyndicationAttribute[] { new SyndicationAttribute("xmlns:example", ExampleNs) };

                var formatter = new RssFormatter(attributes, xmlWriter.Settings);
                var writer = new RssFeedWriter(xmlWriter, attributes, formatter);

                // Create item
                var item = new SyndicationItem()
                {
                    Title = "Rss Writer Available",
                    Description = "The new RSS Writer is now open source!",
                    Id = "https://github.com/dotnet/wcf/tree/lab/lab/src/Microsoft.SyndicationFeed/src",
                    Published = DateTimeOffset.UtcNow
                };

                item.AddCategory(new SyndicationCategory("Technology"));
                item.AddContributor(new SyndicationPerson(null, "test@mail.com"));

                //
                // Format the item as SyndicationContent
                var content = new SyndicationContent(formatter.CreateContent(item));

                // Add custom fields/attributes
                content.AddField(new SyndicationContent("customElement", ExampleNs, "Custom Value"));

                // Write 
                await writer.Write(content);
                await writer.Write(content);

                await writer.Flush();
            }

            string res = sw.ToString();
        }
    }

    static class DateTimeOffsetExtentions
    {
        public static string ToRfc1123(this DateTimeOffset dto)
        {
            return dto.ToString("r");
        }
    }
}
