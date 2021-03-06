﻿using System;
using System.Collections.Generic;
using System.IO;
//using System.Drawing;
using System.Threading.Tasks;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets
{
    public abstract class MainWidgetBase : AppWidgetProvider
    {
        public const string ActionManualRefresh = "_local.intent.action.ManualRefresh";
        public const string ActionChangeTimeType = "_local.intent.action.ChangeTimeType";
        public const string ActionAnimate = "_local.intent.action.Animate";
        public const string ExtraTimeType = "_TimeType";
        public const string ExtraDuriation = "_Duriation";

        public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions)
        {
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);

            xLog.Debug("OnAppWidgetOptionsChanged: " + appWidgetId.ToString());
        }

        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            base.OnDeleted(context, appWidgetIds);

            xLog.Debug("OnDeleted: " + appWidgetIds.ToString());
        }

        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                base.OnReceive(context, intent);

                xLog.Debug("OnReceive: " + intent.Action);
                AppWidgetManager mgr = AppWidgetManager.GetInstance(context);

                if (AppWidgetManager.ActionAppwidgetDeleted.Equals(intent.Action))
                {
                    int appWidgetId = intent.Extras.GetInt(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
                    WidgetConfigHolder cfgHolder = new WidgetConfigHolder();
                    try
                    {
                        WidgetConfigHolder cfgHolderArc = new WidgetConfigHolder(true);

                        int iArchivId = appWidgetId;
                        while (cfgHolderArc.WidgetExists(iArchivId))
                            iArchivId += 10000;
                        var cfg = cfgHolder.GetWidgetCfg<WidgetCfg>(appWidgetId);
                        cfg.WidgetId = iArchivId;
                        cfgHolderArc.SetWidgetCfg(cfg);
                    }
                    catch { }

                    cfgHolder.DeleteWidget(appWidgetId);
                }
                else if (AppWidgetManager.ActionAppwidgetOptionsChanged.Equals(intent.Action))
                {
                    int appWidgetId = intent.Extras.GetInt(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
                    try
                    {
                        var options = intent.Extras.GetBundle("appWidgetOptions");
                        if (options != null)
                        {
                            foreach (var x in options.KeySet())
                                xLog.Warn("OptionsChanged: " + x + " \t" + options.Get(x).ToString());

                            var metrics = context.Resources.DisplayMetrics;

                            if (metrics == null)
                                return;

                            bool bLand = metrics.WidthPixels > metrics.HeightPixels;

                            int iNewWidth = bLand ? options.GetInt("appWidgetMaxWidth") : options.GetInt("appWidgetMinWidth");
                            int iNewHeight = bLand ? options.GetInt("appWidgetMinHeight") : options.GetInt("appWidgetMaxHeight");

                            if (iNewWidth > 0 && iNewHeight > 0)
                                StoreNewWidgetSize(appWidgetId, this.GetType(), iNewWidth, iNewHeight);
                        }
                    }
                    catch (Exception e)
                    {
                        e.ToString();
                    }
                }
                else if (ActionChangeTimeType.Equals(intent.Action))
                {
                    int appWidgetId = intent.Extras.GetInt(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
                    int timeType = intent.Extras.GetInt(ExtraTimeType, -1);

                    if (appWidgetId > 0 && timeType > 0)
                    {
                        WidgetConfigHolder cfgHolder = new WidgetConfigHolder();
                        WidgetCfg cfg = cfgHolder.GetWidgetCfg<WidgetCfg>(appWidgetId, false);
                        if (cfg != null && timeType != (int)cfg.CurrentTimeType)
                        {
                            cfg.CurrentTimeType = (TimeType)timeType;
                            cfgHolder.SetWidgetCfg(cfg);

                            Task.Delay(100).Wait();

                            Intent updateIntent = new Intent(context, this.GetType());
                            updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                            AppWidgetManager widgetManager = AppWidgetManager.GetInstance(context);
                            int[] ids = new[] { appWidgetId };
                            updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
                            context.SendBroadcast(updateIntent);

                        }
                    }
                }
            }
            catch (Exception e)
            {
                xLog.Error(e);
            }
        }

        public override void OnRestored(Context context, int[] oldWidgetIds, int[] newWidgetIds)
        {
            base.OnRestored(context, oldWidgetIds, newWidgetIds);

#if DEBUG
            sys.LogException(new Exception("Restore Widget's: " + oldWidgetIds.Length + " : " + newWidgetIds.Length));
#endif

            try
            {
                Tools.ShowToast(context, "try to restore " + oldWidgetIds.Length + " Widget's");
                var newCfgS = new List<WidgetCfg>();
                var holder = new WidgetConfigHolder();
                var holderArc = new WidgetConfigHolder(true);
                for (int i = 0; i < oldWidgetIds.Length; i++)
                {
                    try
                    {
                        int oldID = oldWidgetIds[i];
                        int newID = newWidgetIds[i];
                        var cfg = holder.GetWidgetCfg<WidgetCfg>(oldID, false);
                        if (cfg == null)
                            cfg = holderArc.GetWidgetCfg<WidgetCfg>(oldID + 10000, false);
                        if (cfg == null)
                            cfg = holderArc.GetWidgetCfg<WidgetCfg>(oldID, false);
                        if (cfg != null)
                        {
                            cfg.WidgetId = newID;
                            newCfgS.Add(cfg);
                        }
                    }
                    catch (Exception ex)
                    {
                        sys.LogException(ex);
                    }
                }
                foreach (var cfg in newCfgS)
                    holder.SetWidgetCfg(cfg, false);
                holder.SaveToFile();
                Tools.ShowToast(context, "restored " + newCfgS.Count + " Widget's");
            }
            catch (Exception ex)
            {
                sys.LogException(ex);
            }
        }

        public static TimeType GetOtherTimeType(TimeType current, TimeType widgetDefault)
        {
            if (current != widgetDefault)
                return widgetDefault;
            if (current != sys.DefaultTimeType)
                return sys.DefaultTimeType;
            if (current != TimeType.RealSunTime)
                return TimeType.RealSunTime;
            if (current != TimeType.TimeZoneTime)
                return TimeType.TimeZoneTime;
            return TimeType.MiddleSunTime;
        }

        static WidgetSizeChangedParams mNewSize = null;
        static int iDelay = 250;
        static Task delayCfgStorer;
        public static void StoreNewWidgetSize(int iWidgetId, Type widgetType, int iWidthDp, int iHeigthDp)
        {
            if (iWidgetId >= 0) // für die schleife
                mNewSize = new WidgetSizeChangedParams() { WidgetId = iWidgetId, Type = widgetType, Width = iWidthDp, Heigth = iHeigthDp };

            if (delayCfgStorer != null)
            {
                iDelay = 500;
                return;
            }

            delayCfgStorer = Task.Factory.StartNew(() =>
            {
                try
                {
                    Task.Delay(iDelay).Wait();

                    WidgetSizeChangedParams storeCfg = mNewSize;
                    mNewSize = null;


                    if (storeCfg != null)
                    {
                        WidgetConfigHolder cfgHolder = new WidgetConfigHolder();

                        if (cfgHolder.WidgetExists(storeCfg.WidgetId))
                        {
                            WidgetCfg cfg = cfgHolder.GetWidgetCfg<WidgetCfg>(storeCfg.WidgetId, false);
                            if (cfg != null)
                            {
                                cfg.WidgetWidth = storeCfg.Width;
                                cfg.WidgetHeight = storeCfg.Heigth;
                                cfgHolder.SetWidgetCfg(cfg);
                                xLog.Warn(storeCfg.Type.Name + " " + storeCfg.WidgetId + ": " + storeCfg.Width.ToString() + "x" + storeCfg.Heigth.ToString() + "dp => " + (storeCfg.Width * sys.DisplayDensity).ToString() + "x" + (storeCfg.Heigth * sys.DisplayDensity).ToString() + "px");

                                Task.Delay(50).Wait();
                                try
                                {
                                    AppWidgetManager widgetManager = AppWidgetManager.GetInstance(Application.Context);

                                    Intent updateIntent = new Intent(Application.Context, storeCfg.Type);
                                    updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                                    updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, new[] { storeCfg.WidgetId });
                                    Application.Context.SendBroadcast(updateIntent);
                                }
                                catch (Exception ex)
                                {
                                    xLog.Error(ex, "Send UpdateToWidgetError after Resze");
                                }
                            }
                            Task.Delay(250).Wait();
                        }
                    }

                    iDelay = 250;
                    delayCfgStorer = null;

                    if (mNewSize != null)
                        StoreNewWidgetSize(-1, typeof(int), -1, -1);
                }
                catch { }
            });
        }

        public static Bitmap GetDrawableBmp(Drawable drw, double iShapeWidthDp, double iShapeHeigthDp)
        {
            var max = GetMaxXY((int)(iShapeWidthDp * sys.DisplayDensity), (int)(iShapeHeigthDp * sys.DisplayDensity), sys.DisplayShortSite);
            int iShapeWidth = max.x;
            int iShapeHeigth = max.y;

            Bitmap bmp = Bitmap.CreateBitmap(iShapeWidth, iShapeHeigth, Bitmap.Config.Argb8888);
            Canvas canvas = new Canvas(bmp);
            drw.SetBounds(0, 0, iShapeWidth, iShapeHeigth);
            drw.Draw(canvas);
            return bmp;
        }

        public static Bitmap CompressBmp(Bitmap bmp)
        {
            DateTime swStart = DateTime.Now;
            MemoryStream mem = new MemoryStream();
            bmp.Compress(Bitmap.CompressFormat.Png, 90, mem);
            bmp.Recycle();
            TimeSpan ts1 = DateTime.Now - swStart;
            swStart = DateTime.Now;
            mem.Seek(0, SeekOrigin.Begin);
            Bitmap decoded = BitmapFactory.DecodeStream(mem);
            TimeSpan ts2 = DateTime.Now - swStart;
            return decoded;
        }

        public static (int x, int y, float n) GetMaxXY(double x, double y, int max = 1000)
            => GetMaxXY((int)x, (int)y, max);

        public static (int x, int y, float n) GetMaxXY(int x, int y, int max = 1000)
        {
            if (max < 1)
                return (x, y, 1);
            max = (int)(max * .9);
            float n = 1;
            if (x > y)
            {
                if (x > max)
                {
                    n = (float)max / x;
                    y = (int)((double)y * n);
                    x = max;
                }
            }
            else
            {
                if (y > max)
                {
                    n = (float)max / y;
                    x = (int)((double)x * n);
                    y = max;
                }
            }
            return (x, y, n);
        }

        public static Point GetWidgetSize(int iWidgetId, WidgetCfg cfg = null, AppWidgetManager manager = null)
        {
            int iWidth = 150;
            int iHeigth = 150;
            if (iWidgetId >= 0)
            {
                if (cfg != null && cfg.WidgetWidth > 0 && cfg.WidgetHeight > 0)
                {
                    iWidth = cfg.WidgetWidth;
                    iHeigth = cfg.WidgetHeight;
                }
                else if (manager != null)
                {
                    AppWidgetProviderInfo inf = manager.GetAppWidgetInfo(iWidgetId);
                    if (inf != null)
                    {
                        iWidth = (int)(inf.MinWidth);
                        iHeigth = (int)(inf.MinHeight);
                    }
                }
            }
            /*if (iWidth > sys.DisplayShortSiteDp)
                iWidth = (int)sys.DisplayShortSiteDp;
            if (iHeigth > sys.DisplayShortSiteDp)
                iHeigth = (int)sys.DisplayShortSiteDp;

            /*if (iWidth > sys.DisplayShortSiteDp || iHeigth > sys.DisplayShortSiteDp)
            {
                if (iWidth > iHeigth)
                {
                    iHeigth = (int)(iHeigth * sys.DisplayShortSiteDp / iWidth);
                    iWidth = (int)sys.DisplayShortSiteDp;
                }
                else
                {
                    iWidth = (int)(iHeigth * sys.DisplayShortSiteDp / iHeigth);
                    iHeigth = (int)sys.DisplayShortSiteDp;
                }
            }*/
            return new Point(iWidth, iHeigth);
        }

        public static PendingIntent GetClickActionPendingIntent(Context context, ClickAction action, int iWidgetId, string settingsUri)
        {
            var itClick = GetClickActionIntent(context, action, iWidgetId, settingsUri);
            if (itClick != null)
                return PendingIntent.GetActivity(context, iWidgetId, itClick, PendingIntentFlags.UpdateCurrent);
            return null;
        }
        public static PendingIntent GetClickActionPendingIntent(Context context, ClickActionType actionType, int iRequestID)
        {
            var itClick = GetClickActionIntent(context, actionType);
            if (itClick != null)
                return PendingIntent.GetActivity(context, iRequestID, itClick, PendingIntentFlags.UpdateCurrent);
            return null;
        }

        public static Intent GetClickActionIntent(Context context, ClickAction action, int iWidgetId, string settingsUri)
        {
            Intent itClick = null;

            switch (action.Type)
            {
                case ClickActionType.None:
                    return null;


                case ClickActionType.OpenSettings:
                    if (string.IsNullOrEmpty(settingsUri))
                        return null;
                    itClick = new Intent(Intent.ActionMain);
                    itClick.SetComponent(ComponentName.UnflattenFromString(settingsUri));
                    itClick.PutExtra(AppWidgetManager.ExtraAppwidgetId, iWidgetId);
                    break;

                case ClickActionType.OpenOtherApp:
                    if (action.Params == null || action.Params.Length == 0)
                        return null;
                    try
                    {
                        string packageName = action.Params[0].Split('=')[1];
                        itClick = context.PackageManager.GetLaunchIntentForPackage(packageName);
                        if (itClick == null)
                        {
                            // Bring user to the market or let them choose an app?
                            itClick = new Intent(Intent.ActionView);
                            itClick.SetData(Android.Net.Uri.Parse("market://details?id=" + packageName));
                        }
                        itClick.AddFlags(ActivityFlags.NewTask);
                    }
                    catch
                    {
                        return null;
                    }
                    break;

                default:

                    return GetClickActionIntent(context, action.Type);
            }

            return itClick;
        }

        public static Intent GetClickActionIntent(Context context, ClickActionType actionType)
        {
            Intent itClick = null;

            switch (actionType)
            {
                case ClickActionType.None:
                    return null;

                case ClickActionType.OpenApp:
                    itClick = new Intent(context, typeof(MainActivity));
                    itClick.AddCategory(Intent.CategoryLauncher);
                    itClick.SetFlags(ActivityFlags.ReorderToFront);
                    break;

#if DEBUG
                case ClickActionType.TestActivity:
                    itClick = new Intent(context, typeof(TestActivity));
                    itClick.SetFlags(ActivityFlags.SingleTop);
                    break;
#endif

                case ClickActionType.OpenClock:
                    itClick = new Intent(context, typeof(MainActivity));
                    itClick.PutExtra("NavigationItem", Resource.Id.nav_clock);
                    itClick.SetFlags(ActivityFlags.ReorderToFront);
                    break;

                case ClickActionType.OpenCalendar:
                    itClick = new Intent(context, typeof(MainActivity));
                    itClick.PutExtra("NavigationItem", Resource.Id.nav_calendar);
                    itClick.SetFlags(ActivityFlags.ReorderToFront);
                    break;
                case ClickActionType.OpenWorldTimeMap:
                    itClick = new Intent(context, typeof(MainActivity));
                    itClick.PutExtra("NavigationItem", Resource.Id.nav_world_time_map);
                    itClick.SetFlags(ActivityFlags.ReorderToFront);
                    break;

                case ClickActionType.CreateEvent:
                    itClick = new Intent(context, typeof(ShortCutActivity));
                    itClick.PutExtra("shortcut", "create_calender_event");
                    break;
                case ClickActionType.CreateAlarm:
                    itClick = new Intent(context, typeof(ShortCutActivity));
                    itClick.PutExtra("shortcut", "create_alarm");
                    break;
                    //case ClickActionType.TimeToTimeDialog:
                    //  break;
            }

            return itClick;
        }
    }

    class WidgetSizeChangedParams
    {
        public int WidgetId;
        public Type Type;
        public int Width;
        public int Heigth;
    }
}