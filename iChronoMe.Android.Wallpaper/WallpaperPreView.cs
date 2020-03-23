using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using static iChronoMe.Droid.Wallpaper.LiveWallpapers.WallpaperClockService;

namespace iChronoMe.Droid.Wallpaper.Controls
{
    [Register("me.ichrono.droid.Wallpaper.Controls.WallpaperPreView")]

    public class WallpaperPreView : View
    {
        public WallpaperPreView(Context context) : base(context)
        {
            init();
        }
        public WallpaperPreView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            init();
        }
        public WallpaperPreView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            init();
        }

        private void init()
        {
            refresh = delegate { Invalidate(); };
        }

        Action refresh = null;

        public WallpaperClockEngine WallpaperClockEngine { get; set; }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (WallpaperClockEngine != null)
            {
                WallpaperClockEngine.DrawWallpaper(canvas);

                WallpaperClockEngine.mHandler.PostDelayed(refresh, 1000 / 60);
            }
        }
    }
}