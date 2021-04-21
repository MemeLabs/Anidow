using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Anidow.Utils
{
    public static class ImageUtil
    {

        public static Bitmap UriToBitmap(string link)
        {
            var bitmapImage = new BitmapImage(new Uri(link));

            using MemoryStream outStream = new();
            var enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmapImage));
            enc.Save(outStream);
            var bitmap = new Bitmap(outStream);

            return new Bitmap(bitmap);
        }

        public static Icon UriToIcon(string link)
        {
            return Icon.FromHandle(UriToBitmap(link).GetHicon());
        }
    }
}
