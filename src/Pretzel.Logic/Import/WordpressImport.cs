﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Abstractions;
using System.Xml.Linq;
using Pretzel.Logic.Extensions;
using System.IO;
using System.Xml;

namespace Pretzel.Logic.Import
{
    public class WordpressImport
    {
        private IFileSystem fileSystem;
        private string pathToSite;
        private string pathToImportFile;

        public WordpressImport(IFileSystem fileSystem, string pathToSite, string pathToImportFile)
        {
            this.fileSystem = fileSystem;
            this.pathToSite = pathToSite;
            this.pathToImportFile = pathToImportFile;
        }

        public void Import()
        {
            var xml = fileSystem.File.ReadAllText(pathToImportFile);
            var root = XElement.Parse(CleanXml(xml));

            XNamespace wp = "http://wordpress.org/export/1.1/";
            XNamespace content = "http://purl.org/rss/1.0/modules/content/";

            var posts = from e in root.Descendants("item")
                        select new WordpressPost
                        {
                            Title = e.Element("title").Value,
                            PostName = e.Element(wp + "post_name").Value,
                            Published = Convert.ToDateTime(e.Element("pubDate").Value),
                            Content = e.Element(content + "encoded").Value,
                            Tags = from t in e.Elements("category")
                                   where t.Attribute("domain").Value == "post_tag"
                                   select t.Value,
                            Categories = from t in e.Elements("category")
                                         where t.Attribute("domain").Value == "category"
                                         select t.Value
                        };

            foreach (var p in posts)
            {
                ImportPost(p);
            }
        }

        private void ImportPost(WordpressPost p)
        {
            var header = new
            {
                Title = p.Title,
                Layout = "post",
                Categories = p.Categories,
                Tags = p.Tags
            };

            var yamlHeader = string.Format("---\r\n{0}---\r\n\r\n", header.ToYaml());
            var postContent = yamlHeader + p.Content; //todo would be nice to convert to proper md
            var fileName = string.Format(@"_posts\{0}-{1}.md", p.Published.ToString("yyyy-MM-dd"), p.PostName.Replace(' ', '-')); //not sure about post name

            fileSystem.File.WriteAllText(Path.Combine(pathToSite, fileName), postContent);
        }


        private string CleanXml(string xml)
        {
            //massive hack to get around wordpress missing the atom namespace
            return xml.Replace("<atom:", "<atom");
        }

        protected class WordpressPost
        {
            public string Title { get; set; }
            public string PostName { get; set; }
            public DateTime Published { get; set; }
            public string Content { get; set; }
            public IEnumerable<string> Tags { get; set; }
            public IEnumerable<string> Categories { get; set; }
        }
    }
}