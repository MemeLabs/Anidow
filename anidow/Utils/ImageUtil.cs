using System;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace Anidow.Utils
{
    public static class ImageUtil
    {
        private static Bitmap UriToBitmap(string link)
        {
            var bitmapImage = new BitmapImage(new Uri(link));

            using MemoryStream outStream = new();
            var enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmapImage));
            enc.Save(outStream);
            var bitmap = new Bitmap(outStream);

            return new Bitmap(bitmap);
        }

        public static Icon UriToIcon(string link) => Icon.FromHandle(UriToBitmap(link).GetHicon());
    }
}