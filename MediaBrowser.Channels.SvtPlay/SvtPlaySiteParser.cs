using HtmlAgilityPack;
using MediaBrowser.Controller.Channels;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Channels.SvtPlay
{
    public class SvtPlaySiteParser
    {
        internal static List<ChannelItemInfo> ParseNode(HtmlNode node, bool abroadOnly)
        {
            var items = new List<ChannelItemInfo>();

            if (node == null)
                return items;

            var playableArticles = node.SelectNodes(".//article[contains(@class, 'playJsInfo-Core') or contains(@class, 'slick_item')]");

            if (playableArticles != null)
                foreach (var article in playableArticles)
                {
                    var playable = ParsePlayableArticle(article, abroadOnly);
                    if (playable != null)
                        items.Add(playable);
                }

            var folderArticles = node.SelectNodes(".//article[not(contains(@class, 'playJsInfo-Core') or contains(@class, 'slick_item'))]");

            if (folderArticles != null)
                foreach (var article in folderArticles)
                {
                    var folder = ParseFolderArticle(article);
                    if (folder != null)
                        items.Add(folder);
                }


            return items;

        }

        public static ChannelItemInfo CreateFolderItem(string title, string id, string url, ChannelItemInfo child)
        {
            var folder = new ChannelItemInfo();

            folder.FolderType = MediaBrowser.Model.Channels.ChannelFolderType.Container;
            folder.Type = ChannelItemType.Folder;
            folder.Name = title;
            folder.Id = string.Format("{0}_{1}", id, url);
            if (child != null)
                folder.ImageUrl = child.ImageUrl;
            return folder;
        }

        private static ChannelItemInfo ParsePlayableArticle(HtmlNode node, bool abroadOnly)
        {
            var mobile = node.Attributes["data-mobile"] != null ? node.Attributes["data-mobile"].Value == "true" : false;
            if (!mobile)
                return null;

            var abroad = node.Attributes["data-abroad"] != null ? node.Attributes["data-abroad"].Value == "true" : false;
            if (abroadOnly && !abroad)
                return null;

            var info = new ChannelItemInfo();

            info.Type = ChannelItemType.Media;
            info.MediaType = MediaBrowser.Model.Channels.ChannelMediaType.Video;
            info.IsInfiniteStream = false;

            info.Name = node.Attributes["data-title"] != null ? node.Attributes["data-title"].Value : "";
            info.Overview = node.Attributes["data-description"] != null ? node.Attributes["data-description"].Value : "";

            if (node.Attributes["data-length"] != null)
            {
                try
                {
                    //Time is not supplied in proper format
                    int total = 0;
                    var lenght = node.Attributes["data-length"].Value;
                    var split = lenght.Split(new[] { "h", "min" }, 3, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length > 1)
                    {
                        total += int.Parse(split[0]) * 60;
                        total += int.Parse(split[1]);
                    }
                    else
                    {
                        total += int.Parse(split[0]);
                    }
                    info.RunTimeTicks = TimeSpan.FromMinutes(total).Ticks;
                }
                catch
                { }
            }

            var a = node.SelectSingleNode(".//a[contains(@class, 'play_videolist-element__link')]");
            if (a == null || a.Attributes["href"] == null)
                return null;

            info.Id = a.Attributes["href"].Value;
            info.MediaSources = new List<ChannelMediaInfo>();
            info.MediaSources.Add(new ChannelMediaInfo { Path = a.Attributes["href"].Value });

            var img = node.SelectSingleNode(".//img[contains(@class, 'play_videolist__thumbnail')]");
            if (img != null && img.Attributes["src"] != null)
            {
                var imagesource = img.Attributes["src"].Value;

                if (imagesource.Contains("ALTERNATES/extralarge_imax/"))
                {
                    info.ImageUrl = imagesource.Replace("extralarge_imax/", "medium/");
                }
                else
                {
                    info.ImageUrl = imagesource;
                }
            }
            //info.MediaSources = (await GetChannelItemMediaInfo(info.Id, CancellationToken.None)).ToList();
            return info;
        }

        private static ChannelItemInfo ParseFolderArticle(HtmlNode node)
        {
            var info = new ChannelItemInfo();

            info.FolderType = MediaBrowser.Model.Channels.ChannelFolderType.Container;
            info.Type = ChannelItemType.Folder;
            var nameNode = node.SelectSingleNode(".//h2[contains(@class, 'play_h4')]");
            info.Name = nameNode != null ? nameNode.InnerText : "";

            var a = node.SelectSingleNode(".//a[contains(@class, 'play_videolist-element__link')]");
            if (a == null || a.Attributes["href"] == null)
                return null;

            info.Id = a.Attributes["href"].Value;

            var img = node.SelectSingleNode(".//img");
            if (img != null && img.Attributes["src"] != null)
            {
                var imgsource = img.Attributes["src"].Value;

                if (!imgsource.StartsWith("http://"))
                    info.ImageUrl = "http://www.svtplay.se" + imgsource;
                else
                    info.ImageUrl = imgsource;
            }

            return info;
        }
    }

}