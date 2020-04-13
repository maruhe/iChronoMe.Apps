using System;

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

namespace iChronoMe.Droid.Wallpaper.LiveWallpapers
{
#if DEBUG
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

            private SKCanvasMapper mapper;
            private Paint paint = new Paint();
            private Point size = new Point();
            private PointF touch_point = new PointF(-1, -1);
            private float offset;
            private long start_time;
            protected Drawable wallpaperDrawable;

            private Action mDrawCube;
            private bool is_visible;

            KeyguardManager myKM;
            private bool bIsLockScreen = false;

            private LocationTimeHolder lth = LocationTimeHolder.LocalInstance;
            private WidgetView_ClockAnalog clockView = new WidgetView_ClockAnalog();

            public WallpaperClockEngine(Context wall) : base(wall as WallpaperClockService)
            {
                mContext = wall;
                start_time = SystemClock.ElapsedRealtime();
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

                clockView.FlowMinuteHand = true;
                clockView.FlowSecondHand = false;
                clockView.ColorTickMarks = clockView.ColorHourHandStroke = clockView.ColorMinuteHandStroke = clockView.ColorSecondHandStroke = xColor.WhiteSmoke;
                clockView.ColorHourHandFill = clockView.ColorMinuteHandFill = xColor.Transparent;

                // Set up the paint to draw the lines for our cube
                paint.Color = Color.White;
                paint.AntiAlias = true;
                paint.StrokeWidth = 2;
                paint.StrokeCap = Paint.Cap.Round;
                paint.SetStyle(Paint.Style.Stroke);

                mapper = new SKCanvasMapper();
                mapper.PaintSurface += Mapper_PaintSurface;

                mDrawCube = delegate { DrawFrame(); };
            }

            internal void RefreshConfig(WallpaperConfig cfg)
            {
                DrawFrame();
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

                mHandler.RemoveCallbacks(mDrawCube);
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
                    mHandler.RemoveCallbacks(mDrawCube);
            }

            public override void OnSurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
            {
                base.OnSurfaceChanged(holder, format, width, height);

                //store the size of the frame
                size.Set(width, height);

                DrawFrame();
            }

            public override void OnSurfaceDestroyed(ISurfaceHolder holder)
            {
                base.OnSurfaceDestroyed(holder);

                is_visible = false;
                mHandler.RemoveCallbacks(mDrawCube);
                mapper.DetachedFromWindow();
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
                mHandler.RemoveCallbacks(mDrawCube);

                if (is_visible)
                {
                    if (clockView.FlowSecondHand)
                        mHandler.PostDelayed(mDrawCube, 1000 / 60);
                    else if (clockView.FlowMinuteHand)
                        mHandler.PostDelayed(mDrawCube, 1000 / 10);
                    else
                        mHandler.PostDelayed(mDrawCube, 1000 - lth.RealSunTime.Millisecond);
                }
            }

            DateTime tstAnimationStart = DateTime.MinValue;
            DateTime tstAnimationEnd = DateTime.MinValue;

            // Draw a wireframe cube by drawing 12 3 dimensional lines between
            // adjacent corners of the cube
            Bitmap background = null;
            DateTime tLastDraw = DateTime.MinValue;
            public void DrawWallpaper(Canvas c)
            {
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

                    var cfg = WallpaperConfigHolder.GetConfig(bIsLockScreen ? WallpaperType.LockScreen : WallpaperType.HomeScreen, sys.DisplayOrientation);

                    int x = cfg.Items[0].X;
                    int y = cfg.Items[0].Y;
                    int w = cfg.Items[0].Width;
                    int h = cfg.Items[0].Heigth;

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

                    var tsInvervall = DateTime.Now - tLastDraw;
                    tLastDraw = DateTime.Now;
                    if (tsInvervall.TotalMinutes < 2 && sys.Debugmode)
                    {
                        c.DrawText((int)tsInvervall.TotalMilliseconds + " ms\n"+clockView.PerformanceInfo, 100, 100, new Paint { Color = Color.Red, TextSize = 45 });
                    }

                    mapper.UpdateCanvasSize(w, h);
                    c.Translate(x, y);
                    mapper.Draw(c);
                }
                catch (Exception ex)
                {
                    Paint p = new Paint();
                    p.Color = Color.Red;
                    c.DrawColor(Color.White);
                    c.DrawText(ex.Message, 0, 0, p);
                }

                if (tstAnimationEnd != DateTime.MinValue && tstAnimationEnd < DateTime.Now)
                {
                    tstAnimationStart = DateTime.MinValue;
                    tstAnimationEnd = DateTime.MinValue;
                }
            }

            private void Mapper_PaintSurface(object sender, SkiaSharp.Views.Android.SKPaintSurfaceEventArgs e)
            {
                clockView.DrawCanvas(e.Surface.Canvas, lth.RealSunTime, (int)e.Info.Width, (int)e.Info.Height, false);
            }
        }
    }
#endif
}
