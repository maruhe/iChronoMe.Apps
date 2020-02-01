using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.Widgets;

namespace iChronoMe.Droid.Widgets.Moon
{
    [BroadcastReceiver(Label = "Mond")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    //[MetaData("android.appwidget.provider", Resource = "@xml/widget_moon")]
    public class MoonWidget : MainWidgetBase
    {
        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            Log.Debug("MoonWidget", "OnUpdate");

            var cfgHolder = new WidgetConfigHolder();

            foreach (int iWidgetId in appWidgetIds)
            {
                var cfg = cfgHolder.GetWidgetCfg<WidgetCfg_Moon>(iWidgetId, false);
                //Config beim ersten mal anlegen
                if (cfg == null)
                {
                    cfg = cfgHolder.GetWidgetCfg<WidgetCfg_Moon>(iWidgetId);
                    cfgHolder.SetWidgetCfg(cfg);
                }

                RemoteViews rv = new RemoteViews(context.PackageName, Resource.Layout.widget_moon);

                Point wSize = MainWidgetBase.GetWidgetSize(iWidgetId, cfg, appWidgetManager);
                int iSize = wSize.X;
                if (wSize.Y > iSize)
                    iSize = wSize.Y;
                iSize = (int)(iSize * sys.DisplayDensity);
                //bool mini = wSize.X < 100;

                var moon = new SunMoon(0, 0, DateTime.Now);
                double sichtlaenge = moon.MoonAge;

                float radius = iSize / 2;
                float rand = 0;
                double laenge;
                int x1, x2;
                int y;

                Bitmap bmpShape = Bitmap.CreateBitmap(iSize, iSize, Bitmap.Config.Argb8888);
                Canvas c = new Canvas(bmpShape);

                //c.DrawColor(Color.Black);

                Paint paint = new Paint();
                paint.SetStyle(Paint.Style.Fill);
                paint.Color = Color.White;

                c.DrawCircle(radius, radius, radius, paint);

                for (double phi = 0; phi <= 90; ++phi)
                {
                    laenge = 2.0 * radius * Math.Cos(Math.PI / 180.0 * phi) * (Math.Abs(sichtlaenge) / 180.0);
                    if (sichtlaenge < 0)
                    {
                        x1 = (int)(-radius * Math.Cos(Math.PI / 180.0 * phi));
                        x2 = (int)(x1 + laenge);
                    }
                    else
                    {
                        x2 = (int)(radius * Math.Cos(Math.PI / 180.0 * phi));
                        x1 = (int)(x2 - laenge);
                    }
                    y = (int)(radius * Math.Sin(Math.PI / 180.0 * phi));
                    paint = new Paint();
                    paint.StrokeWidth = (int)((92-phi)/2);
                    paint.Color = Color.Green;
                    c.DrawLine(
                        x1 + (int)(radius + rand), (int)(y + radius + rand),
                        x2 + (int)(radius + rand), (int)(y + radius + rand), paint);
                    c.DrawLine(
                        x1 + (int)(radius + rand), (int)(-y + radius + rand),
                        x2 + (int)(radius + rand), (int)(-y + radius + rand), paint);
                }

                rv.SetImageViewBitmap(Resource.Id.moon_shape, replaceColor(replaceColor(bmpShape, Color.White, new Color(0, 0, 0, 170)), Color.Green, Color.Transparent));

                appWidgetManager.UpdateAppWidget(iWidgetId, rv);

            }
            base.OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public Bitmap replaceColor(Bitmap src, int fromColor, int targetColor)
        {
            if (src == null)
            {
                return null;
            }
            // Source image size
            int width = src.Width;
            int height = src.Height;
            int[] pixels = new int[width * height];
            //get pixels
            src.GetPixels(pixels, 0, width, 0, 0, width, height);

            for (int x = 0; x < pixels.Length; ++x)
            {
                pixels[x] = (pixels[x] == fromColor) ? targetColor : pixels[x];
            }
            // create result bitmap output
            Bitmap result = Bitmap.CreateBitmap(width, height, src.GetConfig());
            //set pixels
            result.SetPixels(pixels, 0, width, 0, 0, width, height);

            return result;
        }
    }
}