using ImageMagickSharp;
using MediaBrowser.Plugins.SoundCloud.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud.ImageProcessing
{
    public static class OverlayHelper
    {
        public static void DrawImage(MagickWand destination, MagickWand source, Rectangle destinationRect, Rectangle sourceRect)
        {
            source.CurrentImage.CropImage(sourceRect.Width, sourceRect.Height, sourceRect.X, sourceRect.Y);
            OverlayHelper.DrawImage(destination, source, destinationRect);
        }

        public static void DrawImage(MagickWand destination, MagickWand source, Rectangle rect)
        {
            OverlayHelper.DrawImage(destination, source, (float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
        }

        private static void DrawImage(MagickWand destination, MagickWand source, float x, float y, float width, float height)
        {
            source.CurrentImage.ResizeImage(Convert.ToInt32(width), Convert.ToInt32(height));
            OverlayHelper.DrawImage(destination, source, x, y);
        }

        public static void DrawImage(MagickWand destination, MagickWand source, float x, float y)
        {
            destination.CurrentImage.CompositeImage(source, CompositeOperator.OverCompositeOp, Convert.ToInt32(x), Convert.ToInt32(y));
        }

        public static MagickWand GetNewTransparentImage(int width, int height)
        {
            return new MagickWand(width, height, new PixelWand("none", 1));
        }

        public static MagickWand GetNewColorImage(string color, int width, int height)
        {
            return new MagickWand(width, height, new PixelWand(color));
        }
    }
}
