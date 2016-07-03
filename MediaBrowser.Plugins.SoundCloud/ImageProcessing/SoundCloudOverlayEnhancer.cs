using ImageMagickSharp;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.SoundCloud.Drawing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud.ImageProcessing
{
    public class SoundCloudOverlayEnhancer : IImageEnhancer
    {
        private const string Version = "4";

        protected IImageProcessor ImageProcessor
        {
            get;
            private set;
        }

        protected ILogger Logger
        {
            get
            {
                return Plugin.Instance.Logger;
            }
        }

        public MetadataProviderPriority Priority
        {
            get
            {
                return MetadataProviderPriority.First;
            }
        }

        public SoundCloudOverlayEnhancer(IImageProcessor imageProcessor)
        {
            this.ImageProcessor = imageProcessor;
        }

        public async Task EnhanceImageAsync(IHasImages item, string inputPath, string outputPath, ImageType imageType, int imageIndex)
        {
            using (MagickWand img = await this.EnhanceImageAsyncInternal(item, new MagickWand(inputPath), imageType, imageIndex))
            {
                img.SaveImage(outputPath);
            }
        }

        protected async Task<MagickWand> EnhanceImageAsyncInternal(IHasImages item, MagickWand originalImage, ImageType imageType, int imageIndex)
        {
            Logger.Debug("SoundCloudOverlayEnhancer will treat {0}", new object[] { item.PrimaryImagePath });

            Point point = new Point(0, 0);

            var folderItem = item as Folder;

            if (imageType != ImageType.Primary || folderItem == null || folderItem.SourceType != SourceType.Channel)
            {
                return null;
            }

            var overlayType = GetOverlayFromFolderId(folderItem.ExternalId);

            string str = Plugin.Instance.GetExtractedResourceFilePath(overlayType);

            var width = originalImage.CurrentImage.Width;
            var height = originalImage.CurrentImage.Height;

            if (File.Exists(str))
            {
                try
                {
                    var newImage = OverlayHelper.GetNewColorImage("#c9c9c9", width, height);

                    ////using (var saturation = new MagickWand(width, height, new PixelWand("#202020", 0.5)))
                    ////{
                    ////    originalImage.CurrentImage.CompositeImage(saturation, CompositeOperator.SaturateCompositeOp, 0, 0);
                    ////}

                    newImage.CurrentImage.CompositeImage(originalImage, CompositeOperator.SoftLightCompositeOp, point.X, point.Y);

                    using (MagickWand overlayWand = new MagickWand(str))
                    {
                        if (overlayWand.CurrentImage.Height != originalImage.CurrentImage.Height)
                        {
                            // our images are always square, so:
                            overlayWand.CurrentImage.ResizeImage(height, height);
                        }

                        using (var overlayShadowed = overlayWand.CloneMagickWand())
                        {
                            ////using (var whitePixelWand = new PixelWand(ColorName.White))
                            ////{
                            ////    overlayShadowed.CurrentImage.BackgroundColor = whitePixelWand;
                            ////    overlayShadowed.CurrentImage.ShadowImage(80, 5, 5, 5);
                            ////    overlayShadowed.CurrentImage.CompositeImage(overlayWand, CompositeOperator.CopyCompositeOp, 0, 0);
                            ////}

                            newImage.CurrentImage.CompositeImage(overlayShadowed, CompositeOperator.OverCompositeOp, point.X, point.Y);
                        }
                    }

                    return newImage;
                }
                catch (Exception exception1)
                {
                    Exception exception = exception1;
                    this.Logger.ErrorException("Error loading overlay: {0}", exception, overlayType);
                }
            }
            else
            {
                this.Logger.Warn(string.Concat("SoundCloud - Undefined overlay type: ", overlayType), new object[0]);
            }

            return originalImage;
        }

        public ImageSize GetEnhancedImageSize(IHasImages item, ImageType imageType, int imageIndex, ImageSize originalImageSize)
        {
            return originalImageSize;
        }

        public string GetConfigurationCacheKey(IHasImages item, ImageType imageType)
        {
            Folder folder = item as Folder;
            BaseItem baseItem = item as BaseItem;

            string str1 = "";
            string str2 = "";

            if (baseItem != null)
            {
                str1 = baseItem.Id.ToString();
            }

            if (folder != null)
            {
                str2 = folder.ExternalId;
            }

            string[] keys = new string[6];
            keys[0] = item.DisplayMediaType ?? "";
            keys[1] = item.GetType().Name;
            keys[2] = Version;
            keys[3] = imageType.ToString();
            keys[4] = str1;

            keys[5] = str2;

            string keyString = string.Concat(keys);
            return BaseExtensions.GetMD5(keyString).ToString();
        }

        private string GetOverlayFromFolderId(string folderID)
        {
            if (!string.IsNullOrWhiteSpace(folderID))
            {
                if (folderID.StartsWith("usertracks_"))
                {
                    return "overlay_tracks";
                }
                if (folderID.StartsWith("userplaylists_"))
                {
                    return "overlay_playlists";
                }
                if (folderID.StartsWith("followings_"))
                {
                    return "overlay_followings";
                }
                if (folderID.StartsWith("followers_"))
                {
                    return "overlay_followers";
                }
                if (folderID.StartsWith("favorites_"))
                {
                    return "overlay_favorites";
                }
            }

            return null;
        }

        public bool Supports(IHasImages item, ImageType imageType)
        {
            var folderItem = item as Folder;

            if (imageType == ImageType.Primary && folderItem != null && folderItem.SourceType == SourceType.Channel)
            {
                if (folderItem.ChannelId != null && folderItem.ChannelId == Plugin.Instance.OwnChannelId && folderItem.ExternalId != null)
                {
                    var overlay = GetOverlayFromFolderId(folderItem.ExternalId);
                    return overlay != null;
                }
            }

            return false;
        }


        private Size ToSize(ImageSize size)
        {
            Size size1 = new Size(Convert.ToInt32(size.Width), Convert.ToInt32(size.Height));
            return size1;
        }
    }
}
