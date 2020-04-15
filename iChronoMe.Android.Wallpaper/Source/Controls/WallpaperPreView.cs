﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        Activity mContext;

        public WallpaperPreView(Context context) : base(context)
        {
            init(context);
        }
        public WallpaperPreView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            init(context);
        }
        public WallpaperPreView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            init(context);
        }

        private void init(Context context)
        {
            mContext = context as Activity;
            refresh = delegate { Invalidate(); };
        }

        Action refresh = null;

        public WallpaperClockEngine WallpaperClockEngine { get; set; }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (WallpaperClockEngine != null)
            {
                var tNext = WallpaperClockEngine.DrawWallpaper(canvas);
                WallpaperClockEngine.mHandler.RemoveCallbacks(refresh);
                if (bRunning)
                    WallpaperClockEngine.mHandler.PostDelayed(refresh, (int)(tNext-DateTime.Now).TotalMilliseconds);
            }
        }

        bool bRunning = false;
        public void Start()
        {
            bRunning = true;
            Invalidate();
        }

        public void Stop()
        {
            bRunning = false;
        }
    }
}