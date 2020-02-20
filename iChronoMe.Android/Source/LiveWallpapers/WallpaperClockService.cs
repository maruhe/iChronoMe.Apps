using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Service.Wallpaper;
using Android.Views;
using Android.Widget;
using iChronoMe.Core;

namespace iChronoMe.Droid.LiveWallpapers
{
#if DEBUG
	[Service(Label = "@string/widget_title_clock_analog", Permission = "android.permission.BIND_WALLPAPER", Name = "me.ichrono.droid.LiveWallpapers.WallpaperClockService")]
	[IntentFilter(new string[] { "android.service.wallpaper.WallpaperService" })]
	[MetaData("android.service.wallpaper", Resource = "@xml/wallpaper_analogclock")]
	public class WallpaperClockService : WallpaperService
	{
		public override Engine OnCreateEngine()
		{
			return new WallpaperClockEngine(this);
		}

		class WallpaperClockEngine : WallpaperService.Engine
		{
			private Handler mHandler = new Handler();

			private Paint paint = new Paint();
			private Point size = new Point();
			private PointF touch_point = new PointF(-1, -1);
			private float offset;
			private long start_time;
			protected Drawable wallpaperDrawable;

			private Action mDrawCube;
			private bool is_visible;

			private LocationTimeHolder lth = LocationTimeHolder.LocalInstance;
			private WallpaperClockView clockView = new WallpaperClockView();

			public WallpaperClockEngine(WallpaperClockService wall) : base(wall)
			{
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
					wallpaperDrawable = wall.Resources.GetDrawable(Resource.Drawable.dummy_wallpaper, null);

				clockView.FlowMinuteHand = true;
				clockView.FlowSecondHand = true;

				// Set up the paint to draw the lines for our cube
				paint.Color = Color.White;
				paint.AntiAlias = true;
				paint.StrokeWidth = 2;
				paint.StrokeCap = Paint.Cap.Round;
				paint.SetStyle(Paint.Style.Stroke);

				mDrawCube = delegate { DrawFrame(); };
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
				ISurfaceHolder holder = SurfaceHolder;

				Canvas c = null;

				try
				{
					c = holder.LockCanvas();

					if (c != null)
					{
						DrawWallpaper(c);
						DrawTouchPoint(c);
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
					mHandler.PostDelayed(mDrawCube, 1000 - lth.RealSunTime.Millisecond);
			}

			// Draw a wireframe cube by drawing 12 3 dimensional lines between
			// adjacent corners of the cube
			Bitmap background = null;
			void DrawWallpaper(Canvas c)
			{
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
				} catch { }

				try
				{					
					c.DrawColor(Color.White);
					if (wallpaperDrawable != null)
						wallpaperDrawable.Draw(c);
					clockView.DrawClock(c, lth.RealSunTime, size.X, size.Y);
				}
				catch (Exception ex)
				{
					Paint p = new Paint();
					p.Color = Color.Red;
					c.DrawColor(Color.White);
					c.DrawText(ex.Message, 0, 0, p);
				}
			}

			// Draw a circle around the current touch point, if any.
			void DrawTouchPoint(Canvas c)
			{
				if (touch_point.X >= 0 && touch_point.Y >= 0)
					c.DrawCircle(touch_point.X, touch_point.Y, 80, paint);
			}
		}
	}
#endif
}
