using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Service.Wallpaper;
using Android.Views;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.Types;
using iChronoMe.Widgets;
using iChronoMe.Widgets.AndroidHelpers;
using static Android.App.ActivityManager;

namespace iChronoMe.Droid.Wallpaper.LiveWallpapers
{
    [Service(Label = "@string/wallpaper_title_clock_analog", Permission = "android.permission.BIND_WALLPAPER", Name = "me.ichrono.droid.LiveWallpapers.WallpaperClockService")]
    [IntentFilter(new string[] { "android.service.wallpaper.WallpaperService" })]
    [MetaData("android.service.wallpaper", Resource = "@xml/wallpaper_analogclock")]
    public class WallpaperClockService : WallpaperService
    {
        public override Engine OnCreateEngine()
        {
            return new WallpaperClockEngine(this);
        }

        public class WallpaperClockEngine : WallpaperService.Engine
        {
            public Handler mHandler { get; } = new Handler();
            Context mContext;

            private Paint paint = new Paint();
            private Paint backPaint = new Paint(); 
            private Paint backTint = new Paint();
            private Point size = new Point();
            private PointF touch_point = new PointF(-1, -1);
            private float offset;
            private long start_time;
            protected Drawable wallpaperDrawable;

            private Action mDrawAction;
            private bool is_visible;

            KeyguardManager myKM;
            private bool bIsLockScreen = false;

            private LocationTimeHolder lth = LocationTimeHolder.LocalInstance;

            public WallpaperClockEngine(Context wall) : base(wall as WallpaperClockService)
            {
                mContext = wall;
                start_time = SystemClock.ElapsedRealtime();
                RefreshConfig();
                try
                {
                    WallpaperManager wpMgr = WallpaperManager.GetInstance(wall);
                    wallpaperDrawable = wpMgr.Drawable;
                    wpMgr.Dispose();
                }
                catch (System.Exception ex)
                {
                    try
                    {
                        WallpaperManager wpMgr = WallpaperManager.GetInstance(wall);
                        wallpaperDrawable = wpMgr.BuiltInDrawable;
                        wpMgr.Dispose();
                    }
                    catch (System.Exception ex2)
                    {
                        ex2.ToString();
                    }

                    ex.ToString();
                }

                if (wallpaperDrawable == null)
                    wallpaperDrawable = wall.Resources.GetDrawable(Resource.Mipmap.dummy_wallpaper, null);

                // Set up the paint to draw the lines for our cube
                paint.Color = Color.White;
                paint.AntiAlias = true;
                paint.StrokeWidth = 2;
                paint.StrokeCap = Paint.Cap.Round;
                paint.SetStyle(Paint.Style.Stroke);

                backPaint.SetStyle(Paint.Style.Fill);

                mDrawAction = delegate { DrawFrame(); };
            }

            WallpaperConfig cfg;
            Dictionary<string, ItemCache> cfgCache = new Dictionary<string, ItemCache>();
            internal void RefreshConfig()
            {
                cfg?.Dispose();
                cfg = WallpaperConfigHolder.GetConfig(bIsLockScreen ? WallpaperType.LockScreen : WallpaperType.HomeScreen, sys.DisplayOrientation, true);
                //mDrawAction?.Invoke();

                lock (cfgCache)
                {
                    foreach (var item in cfg.Items)
                    {
                        ItemCache itemCache = null;
                        if (cfgCache.ContainsKey(item.Guid))
                            itemCache = cfgCache[item.Guid];
                        else
                        {
                            itemCache = new ItemCache();
                            cfgCache.Add(item.Guid, itemCache);
                        }

                        if (itemCache.CanvasMapper == null)
                        {
                            itemCache.CanvasMapper = new SKCanvasMapper();
                            allMapper.Add(itemCache.CanvasMapper);
                            itemCache.CanvasMapper.PaintSurface += Mapper_PaintSurface;
                        }
                        itemCache.CanvasMapper.ConfigTag = item;

                        if (itemCache.ClockView == null)
                        {
                            itemCache.ClockView = new WidgetView_ClockAnalog();
                            itemCache.CanvasMapper.ViewTag = itemCache.ClockView;
                        }
                        itemCache.ClockView.ReadConfig(item.ClockCfg);
                        itemCache.ClockView.FlowMinuteHand = false;
                        itemCache.ClockView.FlowSecondHand = false;
                        itemCache.ClockView.ShowSecondHand = true;

                        itemCache.CanvasMapper.ConfigTag = item;

                        if (itemCache.ClockView.BackgroundImage != itemCache.BackgroundFile)
                            itemCache.BackgroundFile = string.Empty;

                        if (!string.IsNullOrEmpty(itemCache.ClockView.BackgroundImage))
                        {
                            int w = item.Width;
                            int h = item.Heigth;
                            var bitmap = itemCache.BackgroundCache;
                            if (itemCache.ClockView.BackgroundImage != itemCache.BackgroundFile || bitmap == null || bitmap.Handle == IntPtr.Zero || bitmap.Width != w || bitmap.Height != h)
                            {
                                if (bitmap != null)
                                {
                                    if (bitmap.Handle != IntPtr.Zero && !bitmap.IsRecycled)
                                        bitmap.Recycle();
                                    bitmap.Dispose();
                                }
                                try
                                {
                                    using (var fullsize = BitmapFactory.DecodeFile(itemCache.ClockView.GetClockFacePng(itemCache.ClockView.BackgroundImage, sys.DisplayShortSite)))
                                    {
                                        itemCache.BackgroundCache = Bitmap.CreateScaledBitmap(fullsize, w, h, false);
                                    }
                                }
                                catch
                                {
                                    var drw = new ScaleDrawable(mContext.GetDrawable(Resource.Drawable.icons8_error_clrd), GravityFlags.Center, w, h).Drawable;
                                    Bitmap bmp = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888);
                                    Canvas canvas = new Canvas(bmp);
                                    drw.SetBounds(0, 0, w, h);
                                    drw.Draw(canvas);
                                    itemCache.BackgroundCache = bmp;
                                }
                            }
                            itemCache.BackgroundFile = itemCache.ClockView.BackgroundImage;
                        }
                        else if (itemCache.BackgroundCache != null)
                        {
                            if (itemCache.BackgroundCache.Handle != IntPtr.Zero && !itemCache.BackgroundCache.IsRecycled)
                                itemCache.BackgroundCache.Recycle();
                            itemCache.BackgroundCache.Dispose();
                            itemCache.BackgroundCache = null;
                            itemCache.BackgroundFile = string.Empty;
                        }
                    }
                }

                GC.Collect();
            }

            public override void OnCreate(ISurfaceHolder surfaceHolder)
            {
                base.OnCreate(surfaceHolder);

                // By default we don't get touch events, so enable them.
                SetTouchEventsEnabled(true);
            }

            public override void OnDestroy()
            {
                base.OnDestroy();

                mHandler.RemoveCallbacks(mDrawAction);
            }

            public override void OnVisibilityChanged(bool visible)
            {
                is_visible = visible;

                bIsLockScreen = false;
                if (is_visible)
                {
                    if (myKM == null)
                        myKM = (KeyguardManager)mContext.GetSystemService(Context.KeyguardService);
                    if (myKM.IsDeviceLocked || myKM.IsKeyguardLocked)
                    {
                        bIsLockScreen = true;
                        //Tools.ShowToast(mContext, "Start LockScreenMode");
                    }
                }
                //Tools.ShowToast(mContext, "IsVisible: " + visible + "\nIsDeviceLocked: " + myKM.IsDeviceLocked + "\nIsDeviceLocked: " + myKM.IsKeyguardLocked);

                if (visible)
                    DrawFrame();
                else
                    mHandler.RemoveCallbacks(mDrawAction);
            }

            public override void OnSurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
            {
                base.OnSurfaceChanged(holder, format, width, height);

                //store the size of the frame
                size.Set(width, height);

                DrawFrame();
            }

            List<SKCanvasMapper> allMapper = new List<SKCanvasMapper>();
            public override void OnSurfaceDestroyed(ISurfaceHolder holder)
            {
                base.OnSurfaceDestroyed(holder);

                is_visible = false;
                mHandler.RemoveCallbacks(mDrawAction);
                foreach (var m in allMapper)
                    m.DetachedFromWindow();
                allMapper.Clear();
            }

            public override void OnOffsetsChanged(float xOffset, float yOffset, float xOffsetStep, float yOffsetStep, int xPixelOffset, int yPixelOffset)
            {
                offset = xOffset;

                DrawFrame();
            }

            // Store the position of the touch event so we can use it for drawing later
            public override void OnTouchEvent(MotionEvent e)
            {
                if (e.Action == MotionEventActions.Move)
                    touch_point.Set(e.GetX(), e.GetY());
                else
                    touch_point.Set(-1, -1);

                base.OnTouchEvent(e);
            }

            // Draw one frame of the animation. This method gets called repeatedly
            // by posting a delayed Runnable. You can do any drawing you want in
            // here. This example draws a wireframe cube.
            void DrawFrame()
            {
                if (SurfaceHolder == null)
                    return;
                ISurfaceHolder holder = SurfaceHolder;

                Canvas c = null;

                try
                {
                    c = holder.LockCanvas();

                    if (c != null)
                    {
                        DrawWallpaper(c);
                    }
                }
                finally
                {
                    if (c != null)
                        holder.UnlockCanvasAndPost(c);
                }

                // Reschedule the next redraw
                mHandler.RemoveCallbacks(mDrawAction);

                if (is_visible)
                {
                    mHandler.PostDelayed(mDrawAction, 1000 / 10);
                    /*
                    if (clockView.FlowSecondHand)
                        mHandler.PostDelayed(mDrawCube, 1000 / 60);
                    else if (clockView.FlowMinuteHand)
                        mHandler.PostDelayed(mDrawCube, 1000 / 10);
                    else
                        mHandler.PostDelayed(mDrawCube, 1000 - lth.RealSunTime.Millisecond);
                        */
                }
            }

            DateTime tstAnimationStart = DateTime.MinValue;
            DateTime tstAnimationEnd = DateTime.MinValue;

            // Draw a wireframe cube by drawing 12 3 dimensional lines between
            // adjacent corners of the cube
            Bitmap background = null;
            //DateTime tLastDraw = DateTime.MinValue;
            DateTime tLastGC = DateTime.MinValue;
            public DateTime DrawWallpaper(Canvas c)
            {
                DateTime tRes = DateTime.Now.AddSeconds(1);
                var swStart = DateTime.Now;
                try
                {
                    if (bIsLockScreen)
                    {
                        if (!myKM.IsDeviceLocked && !myKM.IsKeyguardLocked)
                        {
                            bIsLockScreen = false;
                            tstAnimationStart = DateTime.Now;
                            tstAnimationEnd = tstAnimationStart.AddSeconds(.5);
                            //Tools.ShowToast(mContext, "LockScreenMode end");
                        }
                    }
                }
                catch { }

                try
                {
                    if (background == null)
                    {
                        if (wallpaperDrawable != null)
                        {
                            var drw = new ScaleDrawable(wallpaperDrawable, GravityFlags.Center, size.X, size.Y).Drawable;
                            Bitmap bmp = Bitmap.CreateBitmap(size.X, size.Y, Bitmap.Config.Argb8888);
                            Canvas canvas = new Canvas(bmp);
                            drw.SetBounds(0, 0, size.X, size.Y);
                            drw.Draw(canvas);
                            background = bmp;
                        }
                    }
                }
                catch { }

                try
                {
                    c.DrawColor(Color.White);
                    if (wallpaperDrawable != null)
                        wallpaperDrawable.Draw(c);
                    lock (cfgCache)
                    {
                        foreach (var item in cfg.Items)
                        {

                            int x = item.X;
                            int y = item.Y;
                            int w = item.Width;
                            int h = item.Heigth;

                            ItemCache itemCache = cfgCache[item.Guid];

                            /*

                            int x = 0;
                            int y = 0;
                            int w = size.X;
                            int h = size.Y;

                            if (bIsLockScreen)
                            {
                                y = size.Y / 10;
                                h -= y;
                            }
                            else
                            {
                                w -= size.X / 2;
                                y = size.Y * 3 / 11;
                                h -= size.Y / 2;
                            }

                            if (tstAnimationEnd > DateTime.Now)
                            {
                                double nTotal = (tstAnimationEnd - tstAnimationStart).TotalMilliseconds;
                                double nDone = (DateTime.Now - tstAnimationStart).TotalMilliseconds;

                                double x1 = 0;
                                double y1 = size.Y / 10;
                                double w1 = size.X;
                                double h1 = size.Y - y;

                                double x2 = 0;
                                double y2 = size.Y * 3 / 11;
                                double w2 = size.X - size.X / 2;
                                double h2 = size.Y - size.Y / 2;

                                double nx = x1 + (x2 - x1) * nDone / nTotal;
                                double ny = y1 + (y2 - y1) * nDone / nTotal;
                                double nw = w1 + (w2 - w1) * nDone / nTotal;
                                double nh = h1 + (h2 - h1) * nDone / nTotal;

                                x = (int)nx;
                                y = (int)ny;
                                w = (int)nw;
                                h = (int)nh;
                            }
                            */

                            itemCache.CanvasMapper.UpdateCanvasSize(w, h);
                            c.Translate(x, y);

                            if (item.ClockCfg.ColorBackground.A > 0)
                            {
                                backPaint.Color = item.ClockCfg.ColorBackground.ToAndroid();
                                c.DrawCircle(w / 2, h / 2, w / 2, backPaint);
                            }

                            if (itemCache.BackgroundCache != null && itemCache.BackgroundCache.Handle != IntPtr.Zero && !itemCache.BackgroundCache.IsRecycled)
                            {
                                if (item.ClockCfg.BackgroundImageTint.A == 0)
                                    c.DrawBitmap(itemCache.BackgroundCache, 0, 0, null);
                                else
                                {
                                    ColorFilter filter = new PorterDuffColorFilter(item.ClockCfg.BackgroundImageTint.ToAndroid(), PorterDuff.Mode.SrcIn);
                                    backTint.SetColorFilter(filter);

                                    c.DrawBitmap(itemCache.BackgroundCache, 0, 0, backTint);
                                }
                            }

                            var tClock = sys.GetTimeWithoutMilliSeconds(lth.GetTime(item.ClockCfg.CurrentTimeType).AddSeconds(1));
                            var tNext = DateTime.Now.AddMilliseconds(1000 - tClock.Millisecond);
                            if (tNext < tRes)
                                tRes = tNext;

                            itemCache.CanvasMapper.Draw(c, tClock);

                            if (sys.Debugmode)
                                c.DrawText(itemCache.ClockView.PerformanceInfo, 15, -5, new Paint { Color = Color.Blue, TextSize = sys.DpPx(16) });
                            c.Translate(-(x), -(y));
                        }

                        if (tLastGC.AddSeconds(10) < DateTime.Now)
                        {
                            GC.Collect();
                            tLastGC = DateTime.Now;
                            if (sys.Debugmode)
                                c.DrawText("collected", c.Width - 300, 150, new Paint { Color = Color.Black, TextSize = sys.DpPx(16) });
                        }
                        if (sys.Debugmode)
                        {
                            c.DrawText((DateTime.Now - swStart).TotalMilliseconds.ToString("#.00") + "ms", c.Width - 300, 100, new Paint { Color = Color.Black, TextSize = sys.DpPx(16) });
                            if (tLastMeminfo.AddSeconds(1) < DateTime.Now)
                            {
                                if (activityManager == null)
                                    activityManager = (ActivityManager)mContext.GetSystemService(Context.ActivityService);
                                var y = activityManager.GetProcessMemoryInfo(new int[] { Process.MyPid() })[0];
                                kbMem = y.TotalPss;
                                tLastMeminfo = DateTime.Now;
                            }
                            c.DrawText((kbMem / 1024).ToString("#.00") + "mb", c.Width - 300, 50, new Paint { Color = Color.Black, TextSize = sys.DpPx(16) });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Paint p = new Paint();
                    p.Color = Color.Red;
                    p.TextSize = sys.DpPx(24);
                    //c.DrawColor(Color.White);
                    c.DrawText(ex.Message, 100, 100, p);
                }

                if (tstAnimationEnd != DateTime.MinValue && tstAnimationEnd < DateTime.Now)
                {
                    tstAnimationStart = DateTime.MinValue;
                    tstAnimationEnd = DateTime.MinValue;
                }

                return tRes;
            }
            ActivityManager activityManager = null;
            DateTime tLastMeminfo = DateTime.MinValue;
            float kbMem = 0;

            private void Mapper_PaintSurface(object sender, CMPaintSurfaceEventArgs e)
            {
                if (sender is SKCanvasMapper)
                {
                    var cm = (sender as SKCanvasMapper);
                    if (cm.ViewTag is WidgetView_ClockAnalog)
                    {
                        (cm.ViewTag as WidgetView_ClockAnalog).DrawCanvas(e.Surface.Canvas, (DateTime)e.Param, (int)e.Info.Width, (int)e.Info.Height, false);
                    }
                }              
            }
        }        
    }

    public class ItemCache
    {
        public WidgetView_ClockAnalog ClockView { get; set; }
        public SKCanvasMapper CanvasMapper { get; set; }
        public Android.Graphics.Bitmap BackgroundCache { get; set; }
        public string BackgroundFile { get; set; }
    }
}
