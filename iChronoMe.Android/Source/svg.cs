using System.Collections.Generic;

using Android.Graphics;

using FFImageLoading;
using FFImageLoading.Svg.Platform;

namespace iChronoMe.Droid
{
    public static class svg
    {
        public static Bitmap GetIcon(string cName, int iWidth, int iHeight, Color? newColor = null)
        {
            if (string.IsNullOrEmpty(cName))
                return null;
            var sdr = new SvgDataResolver(iWidth, iHeight, true);
            if (newColor.HasValue && newColor.Value.A > 0)
                sdr.ReplaceStringMap = new Dictionary<string, string> { { "#000000", "#" + newColor.Value.R.ToString("X2") + newColor.Value.G.ToString("X2") + newColor.Value.B.ToString("X2") } };
            var img = ImageService.Instance.LoadCompiledResource(cName).WithCustomDataResolver(sdr);
            return img.AsBitmapDrawableAsync().Result.Bitmap;
        }

        static Dictionary<string, Bitmap> IconCache = new Dictionary<string, Bitmap>();
        public static Bitmap GetCacheIcon(string cIconName, int iWidth, int iHeight, Color? newColor = null)
        {
            lock (IconCache)
            {
                string cId = cIconName + "_" + iWidth + ":" + iHeight + "_" + (newColor.HasValue ? newColor.Value.ToArgb().ToString() : "x");
                if (IconCache.ContainsKey(cId))
                    return IconCache[cId];
                var bmp = svg.GetIcon(cIconName, iWidth, iHeight, newColor);
                IconCache.Add(cId, bmp);
                return bmp;
            }
        }

        public static bool IsIconColored(string cName)
        {
            if (string.IsNullOrEmpty(cName))
                return false;
            var bmp = GetIcon(cName, 10, 10);

            for (int iX = 0; iX < bmp.Width / 2; iX++)
            {
                for (int iY = 0; iY < bmp.Height / 2; iY++)
                {
                    var clr = new Color(bmp.GetPixel(iX, iY));
                    if (clr.A != 0 && (clr.R != 0 || clr.G != 0 || clr.B != 0))
                        return true;
                }
            }

            return false;
        }
    }
}