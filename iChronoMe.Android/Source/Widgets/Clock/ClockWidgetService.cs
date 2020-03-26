using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;

using AndroidX.Core.App;
using AndroidX.Core.Content;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.Types;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.Clock
{
    /*
     * not working fine this stuff
    [Service(Label = "Clock-Widget Update-Service", Permission = "android.permission.BIND_JOB_SERVICE", Exported = true)]
    public class ClockWidgetService : JobIntentService
    { 
        public override void OnCreate()
        {
            base.OnCreate();

            SetTheme(Resource.Style.BaseTheme_iChronoMe_Dark);
        }

        public static string CheckRequiredFile = System.IO.Path.Combine(sys.PathConfig, ".HasClockWidgets");
        public const string Action_CheckLocation = "Action_CheckLocation";
        public const string Action_OptionsChanged = "Action_OptionsChanged";
        public const string Action_ScreenOn = "Action_ScreenOn";
        public const string Extra_UpdateFromWidget = "Extra_UpdateFromWidget";
        public const string Extra_FromTimer = "Extra_FromTimer";
        const int MY_JOB_ID = 1123;


        public static void EnqueueWork(Context context, Intent work)
        {
            Java.Lang.Class cls = Java.Lang.Class.FromType(typeof(ClockWidgetService));
            try
            {
                EnqueueWork(context, cls, MY_JOB_ID, work);
            }
            catch (Exception ex)
            {
                xLog.Debug(ex, "Exception: {0}");
            }
        }

        static bool bHasPermissions = Build.VERSION.SdkInt < BuildVersionCodes.M;

        static WidgetConfigHolder cfgHolder = null;
        static AppWidgetManager appWidgetManager = null;
        static AlarmManager alarmManager = null;

        static object oLock = new object();

        protected override void OnHandleWork(Intent intent)
        {
            xLog.Debug("OnHandleWork " + intent.Action);

            lock (oLock)
            {
                if (appWidgetManager == null)
                    appWidgetManager = AppWidgetManager.GetInstance(this);
                int[] appWidgetIDs = intent.GetIntArrayExtra(AppWidgetManager.ExtraAppwidgetIds);
                if (appWidgetIDs == null || appWidgetIDs.Length == 0)
                {
                    appWidgetIDs = appWidgetManager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(AnalogClockWidget)).Name));
                }
                if (appWidgetIDs == null || appWidgetIDs.Length == 0)
                {
                    StopSelf();
                    try
                    {
                        if (System.IO.File.Exists(CheckRequiredFile))
                            System.IO.File.Delete(CheckRequiredFile);
                    } catch { }
                    return;
                }

                if (!bHasPermissions)
                {
                    xLog.Debug("PermissionCheck");
                    try
                    {
                        bool bPermissionError = false;

                        if (this.CheckSelfPermission(Android.Manifest.Permission.AccessCoarseLocation) != Android.Content.PM.Permission.Granted)
                            bPermissionError = true;

                        if (!bPermissionError)
                            bHasPermissions = true;
                    }
                    catch { }

                    if (!bHasPermissions)
                    {
                        xLog.Debug("Missing Permissions!");
                        foreach (int iWidgetId in appWidgetIDs)
                        {
                            RemoteViews rv = new RemoteViews(PackageName, Resource.Layout.widget_permission_error);

                            Intent cfgIntent = new Intent(Intent.ActionMain);
                            cfgIntent.SetComponent(ComponentName.UnflattenFromString("me.ichrono.droid/me.ichrono.droid.Widgets.WidgetItemClickActivity"));
                            cfgIntent.SetFlags(ActivityFlags.NoHistory);
                            cfgIntent.PutExtra("_ClickCommand", "CheckCalendarWidget");
                            cfgIntent.PutExtra("_ConfigComponent", "me.ichrono.droid/me.ichrono.droid.Widgets.Calendar.CalendarWidgetConfigActivity");
                            cfgIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, iWidgetId);

                            PendingIntent cfgPendingIntent = PendingIntent.GetActivity(this, 11, cfgIntent, PendingIntentFlags.UpdateCurrent);
                            rv.SetOnClickPendingIntent(Resource.Id.widget, cfgPendingIntent);

                            appWidgetManager.UpdateAppWidget(iWidgetId, rv);
                        }
                        return;
                    }
                }

                try
                {
                    if (!System.IO.File.Exists(CheckRequiredFile))
                        System.IO.File.WriteAllText(CheckRequiredFile, string.Empty);
                }
                catch { }

                CleanCache(lastClockTimeS, new int[] { -3 });

                if (cfgHolder == null)
                    cfgHolder = new WidgetConfigHolder();
                else
                {
                    if (cfgHolder.CheckUpdate())
                    {
                        CleanCache(wSizeS, null);
                        CleanCache(clockViewS, null);
                        CleanCache(uBackgroundImageS, null);
                        CleanCache(bmpBackgroundColorS, null);
                    }
                }

                switch (intent.Action)
                {
                    case Action_CheckLocation:

                        break;

                    case Action_ScreenOn:
                        lock (lastUpdateCommandS)
                        {
                            foreach (int i in appWidgetIDs)
                            {
                                if (!lastUpdateCommandS.ContainsKey(i) || lastUpdateCommandS[i].AddMinutes(55) < DateTime.Now)
                                    lastUpdateCommandS[i] = DateTime.Now.AddMinutes(-55);
                            }
                        }
                        goto default;

                    case Action_OptionsChanged:
                        CleanCache(wSizeS, appWidgetIDs);
                        CleanCache(clockViewS, appWidgetIDs);
                        CleanCache(uBackgroundImageS, appWidgetIDs);
                        CleanCache(bmpBackgroundColorS, appWidgetIDs);
                        goto default;

                    default:
                        UpdateWidgets(appWidgetIDs, intent.Extras);
                        break;
                }
            }
        }

        private void CleanCache(IDictionary dict, int[] appWidgetIDs)
        {
            lock (dict)
            {
                if (appWidgetIDs == null || appWidgetIDs.Length == 0)
                    dict.Clear();
                else
                {
                    foreach (int i in appWidgetIDs)
                    {
                        try
                        {
                            dict.Remove(i);
                            dict.Remove(-212);
                        }
                        catch (Exception ex)
                        {
                            xLog.Error(ex);
                        }
                    }
                }
            }
        }

        public void UpdateWidgets(int[] appWidgetIDs, Bundle extras)
        {
            if (appWidgetIDs == null || appWidgetIDs.Length == 0)
                return;

            foreach (int i in appWidgetIDs)
                UpdateWidget(i, extras);
        }

        static Dictionary<int, DateTime> lastUpdateCommandS = new Dictionary<int, DateTime>();
        static Dictionary<int, Point> wSizeS = new Dictionary<int, Point>();
        static Dictionary<int, WidgetView_ClockAnalog> clockViewS = new Dictionary<int, WidgetView_ClockAnalog>();
        static Dictionary<int, Android.Net.Uri> uBackgroundImageS = new Dictionary<int, Android.Net.Uri>();
        static Dictionary<int, Bitmap> bmpBackgroundColorS = new Dictionary<int, Bitmap>();
        static Dictionary<int, DateTime> lastClockTimeS = new Dictionary<int, DateTime>();
        static Dictionary<int, WidgetCfg_Clock> lastConfigS = new Dictionary<int, WidgetCfg_Clock>();
        static Dictionary<int, string> xxxS = new Dictionary<int, string>();

        public void UpdateWidget(int iWidgetId, Bundle extras)
        {
            Context context = this;
            if (context == null || iWidgetId == 0)
                return;
            new Thread(() =>
            {
                try
                {
                    xLog.Debug("Start update ClockWidget appWidgetID");
                    var swStart = DateTime.Now;

                    var cfg = cfgHolder.GetWidgetCfg<WidgetCfg_Clock>(iWidgetId, false);
                    if (cfg == null || cfg.PositionType == WidgetCfgPositionType.None)
                    {
                        RemoteViews updateViews = new RemoteViews(context.PackageName, Resource.Layout.widget_unconfigured);
                        updateViews.SetOnClickPendingIntent(Resource.Id.widget, MainWidgetBase.GetClickActionPendingIntent(context, new ClickAction(ClickActionType.OpenSettings), iWidgetId, "me.ichrono.droid/me.ichrono.droid.Widgets.Clock.AnalogClockWidgetConfigActivity"));
                        appWidgetManager.UpdateAppWidget(iWidgetId, updateViews);
                        return;
                    }

                    Point wSize;
                    if (wSizeS.ContainsKey(iWidgetId))
                        wSize = wSizeS[iWidgetId];
                    else
                    {
                        wSize = MainWidgetBase.GetWidgetSize(iWidgetId, cfg, appWidgetManager);
                        wSizeS[iWidgetId] = wSize;
                    }
                    int iClockSizeDp = Math.Min(wSize.X, wSize.Y);
                    int iClockSize = (int)(iClockSizeDp * sys.DisplayDensity);

                    var lth = LocationTimeHolder.LocalInstance;
                    WidgetView_ClockAnalog clockView = null;

                    var tt = cfg.CurrentTimeType;
                    var tNow = lth.GetTime(tt);
                    if (tNow.Second == 59)
                    {
                        tNow = sys.GetTimeWithoutSeconds(tNow).AddMinutes(1);
                    }

                    if (cfg is WidgetCfg_ClockAnalog)
                    {
                        var cfgA = cfg as WidgetCfg_ClockAnalog;

                        if (lastClockTimeS.ContainsKey(iWidgetId) &&
                            ((tNow - lastClockTimeS[iWidgetId]).ToPositive() > TimeSpan.FromMinutes(5) ||
                            (lastConfigS.ContainsKey(iWidgetId) && lastConfigS[iWidgetId].CurrentTimeType != cfg.CurrentTimeType)))
                        {
                            var from = lastClockTimeS[iWidgetId];
                            lastClockTimeS.Remove(iWidgetId);
                            if (string.IsNullOrEmpty(cfgA.WidgetTitle) && lastConfigS.ContainsKey(iWidgetId) && !string.IsNullOrEmpty(lastConfigS[iWidgetId].WidgetTitle))
                                cfgA.WidgetTitle = lastConfigS[iWidgetId].WidgetTitle;
                            Animate(cfgA, from, tNow);
                            return;
                        }

                        lock (clockViewS)
                        {
                            if (clockViewS.ContainsKey(iWidgetId))
                                clockView = clockViewS[iWidgetId];
                            else
                            {
                                clockView = new WidgetView_ClockAnalog();
                                clockView.ClockFaceLoaded += ClockView_ClockFaceLoaded;
                                clockViewS[iWidgetId] = clockView;
                            }
                        }
                        clockView.ReadConfig(cfgA);
                        bool bShowClockProgress = false;
                        Android.Net.Uri uBackgroundImage = null;
                        if (!string.IsNullOrEmpty(cfgA.BackgroundImage))
                        {
                            lock (uBackgroundImageS)
                            {
                                if (uBackgroundImageS.ContainsKey(iWidgetId))
                                    uBackgroundImage = uBackgroundImageS[iWidgetId];
                                else
                                {
                                    uBackgroundImage = GetWidgetBackgroundUri(context, clockView, cfgA, iClockSize, ref bShowClockProgress);
                                    uBackgroundImageS[iWidgetId] = uBackgroundImage;
                                }
                            }
                        }

                        Bitmap bmpBackgroundColor = null;
                        if (cfg.ColorBackground.ToAndroid() != Color.Transparent)
                        {
                            lock (bmpBackgroundColorS)
                            {
                                if (bmpBackgroundColorS.ContainsKey(iWidgetId))
                                    bmpBackgroundColor = bmpBackgroundColorS[iWidgetId];
                                else
                                {
                                    GradientDrawable shape = new GradientDrawable();
                                    shape.SetShape(ShapeType.Oval);
                                    shape.SetColor(cfg.ColorBackground.ToAndroid());
                                    bmpBackgroundColor = MainWidgetBase.GetDrawableBmp(shape, iClockSizeDp, iClockSizeDp);
                                    bmpBackgroundColorS[iWidgetId] = bmpBackgroundColor;
                                }
                            }
                        }

                        if (iWidgetId >= 0)
                        {
                            var tsPrepare = DateTime.Now - swStart;
                            var rv = GetClockAnalogRemoteView(context, cfgA, clockView, iClockSize, lth, tNow, uBackgroundImage, bmpBackgroundColor, true);
                            rv.SetViewVisibility(Resource.Id.clock_progress, bShowClockProgress ? ViewStates.Visible : ViewStates.Gone);
                            if (bShowClockProgress)
                                rv.SetViewPadding(Resource.Id.clock_progress, iClockSize / 3, iClockSize / 3, iClockSize / 3, iClockSize / 3);

                            string cText = DateTime.Now.ToString("HH:mm:ss.fff") + " ..  " + tNow.ToString("HH:mm:ss.fff");
                            lock (xxxS)
                            {
                                if (xxxS.ContainsKey(iWidgetId))
                                    cText += "\n" + xxxS[iWidgetId];
                                xxxS[iWidgetId] = cText;
                            }

                            var tsCreateView = DateTime.Now - swStart;

                            if (tNow.Second == 0 && tNow.Millisecond == 0)
                            {
                                var tX = lth.GetTime(tt);
                                if (tX < tNow)
                                    Thread.Sleep(tNow - tX);
                                cText = (int)(tNow - tX).TotalMilliseconds + "\n" + cText;
                            }
                            cText = (int)tsPrepare.TotalMilliseconds + "/" + (int)tsClockImage.TotalMilliseconds + "/" + (int)tsCreateView.TotalMilliseconds + "   __   " + cText;
                            rv.SetTextViewText(Resource.Id.clock_title, cText);
                            appWidgetManager.UpdateAppWidget(iWidgetId, rv);
                            lastClockTimeS[iWidgetId] = tNow;
                            lastConfigS[iWidgetId] = cfg;
                        }
                    }

                    if (extras != null)
                    {
                        if (extras.GetInt(Extra_UpdateFromWidget, -1) == 1)
                        {
                            lastUpdateCommandS[iWidgetId] = DateTime.Now;
                        }

                        if (lastUpdateCommandS.ContainsKey(iWidgetId) && lastUpdateCommandS[iWidgetId].AddHours(1) > DateTime.Now)
                        {
                            if (alarmManager == null)
                                alarmManager = (AlarmManager)GetSystemService(Context.AlarmService);

                            Intent intent = new Intent(context, typeof(AnalogClockWidget));
                            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, new int[] { iWidgetId });
                            intent.PutExtra(Extra_FromTimer, 1);
                            PendingIntent pi = PendingIntent.GetBroadcast(context, iWidgetId, intent, PendingIntentFlags.UpdateCurrent);

                            var tNext = sys.GetTimeWithoutSeconds(tNow).AddMinutes(1);
                            var ts = (tNext - lth.GetTime(tt)).ToPositive();
                            if (ts.TotalSeconds < 90)
                            {
                                if (ts.TotalSeconds < 15)
                                {
                                    int wait = (int)ts.TotalMilliseconds;
                                    if (wait > 500)
                                        Thread.Sleep(wait - 200);

                                    EnqueueWork(context, intent);
                                }
                                else
                                    alarmManager.SetExact(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime() + (int)ts.TotalMilliseconds-200, pi);
                            }
                        }
                    }
                } 
                catch (ThreadAbortException) { }
                catch (Exception ex)
                {
                    sys.LogException(ex);
                    xLog.Error(ex, "Update Widget Error: " + iWidgetId);
                    RemoteViews rv = new RemoteViews(context.PackageName, Resource.Layout.widget_unconfigured);
                    rv.SetTextViewText(Resource.Id.message, "error loading widget:\n" + ex.Message + "\n" + ex.StackTrace);
                    rv.SetTextColor(Resource.Id.message, Color.IndianRed);
                    appWidgetManager.UpdateAppWidget(iWidgetId, rv);
                }
            }).Start();
        }

        public void Animate(WidgetCfg_ClockAnalog cfg, DateTime tAnimateFrom, DateTime tAnimateTo)
        {
            if (cfg == null || cfg.WidgetId == 0)
                return;

            int iWidgetId = cfg.WidgetId;
            try
            {
                //TimeType Changed => animate

                Point wSize = MainWidgetBase.GetWidgetSize(iWidgetId, cfg, appWidgetManager);
                int iClockSizeDp = wSize.X;
                if (wSize.Y < wSize.X)
                    iClockSizeDp = wSize.Y;
                int iClockSize = (int)(iClockSizeDp * sys.DisplayDensity);

                WidgetView_ClockAnalog clockView = new WidgetView_ClockAnalog();
                clockView.ReadConfig(cfg);

                bool bShowClockProgress = false;
                DateTime tBackgroundUpdate = DateTime.MinValue;
                Android.Net.Uri uBackgroundImage = GetWidgetBackgroundUri(this, clockView, cfg, iClockSize, ref bShowClockProgress);

                Bitmap bmpBackgroundColor = null;
                if (cfg.ColorBackground.ToAndroid() != Color.Transparent)
                {
                    GradientDrawable shape = new GradientDrawable();
                    shape.SetShape(ShapeType.Oval);
                    shape.SetColor(cfg.ColorBackground.ToAndroid());
                    bmpBackgroundColor = MainWidgetBase.GetDrawableBmp(shape, iClockSizeDp, iClockSizeDp);
                }

                TimeType tType = cfg.CurrentTimeType;
                var lth = LocationTimeHolder.LocalInstance;

                TimeSpan tsDuriation = TimeSpan.FromSeconds(1);

                var animator = new WidgetAnimator_ClockAnalog(clockView, tsDuriation, ClockAnalog_AnimationStyle.HandsNatural)
                    .SetStart(tAnimateFrom)
                    .SetEnd(tAnimateTo)
                    .SetPushFrame((h, m, s) =>
                    {
                        if ((m) != tAnimateFrom.Minute && (!clockView.FlowMinuteHand || !clockView.FlowSecondHand))
                        {
                            clockView.FlowMinuteHand = true;
                            clockView.FlowSecondHand = true;
                        }

                        var rv = GetClockAnalogRemoteView(this, cfg, clockView, iClockSize, lth, h, m, s, uBackgroundImage, bmpBackgroundColor, false);
                        rv.SetImageViewBitmap(Resource.Id.time_switcher, null);
                        appWidgetManager.UpdateAppWidget(iWidgetId, rv);
                    })
                    .SetLastRun((h, m, s) =>
                    {
                        clockView.ReadConfig(cfg);
                        var rvf = GetClockAnalogRemoteView(this, cfg, clockView, iClockSize, lth, h, m, s, uBackgroundImage, bmpBackgroundColor, true);
                        appWidgetManager.UpdateAppWidget(iWidgetId, rvf);
                    })
                    .SetFinally(() =>
                    {
                        UpdateWidget(iWidgetId, null);
                    })
                    .StartAnimation();

#if DExxxBUG
                    Intent changeTypeIntent = new Intent(ctx, typeof(AnalogClockWidget));
                    changeTypeIntent.SetAction(MainWidgetBase.ActionChangeTimeType);
                    changeTypeIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, iWidgetId);
                    if (tType != TimeType.RealSunTime)
                        changeTypeIntent.PutExtra(MainWidgetBase.ExtraTimeType, (int)TimeType.RealSunTime);
                    else
                        changeTypeIntent.PutExtra(MainWidgetBase.ExtraTimeType, (int)TimeType.TimeZoneTime);
                    ctx.SendBroadcast(changeTypeIntent);
#endif

            }
            catch (Exception ex)
            {
                sys.LogException(ex);
                UpdateWidget(iWidgetId, null);
            }
        }

        private void ClockView_ClockFaceLoaded(object sender, EventArgs e)
        {
            try
            {
                EnqueueWork(this, new Intent(Action_OptionsChanged));
            } catch { }
        }

        public static Android.Net.Uri GetWidgetBackgroundUri(Context ctx, WidgetView_ClockAnalog vClock, WidgetCfg_ClockAnalog cfg, int sizePX, ref bool bShowClockProgress)
        {
            try
            {
                if (!string.IsNullOrEmpty(cfg.BackgroundImage))
                {
                    string cBackImgPath = vClock.GetClockFacePng(cfg.BackgroundImage, sizePX);
                    bShowClockProgress = Equals(cBackImgPath, cfg.BackgroundImage);
                    Java.IO.File fBack = new Java.IO.File(cBackImgPath);
                    bool bBackEx = fBack.Exists();
                    if (bBackEx)
                    {
                        return GrandImageAccessToLaunchers(ctx, fBack);
                    }
                }
            }
            catch (Exception eImg)
            {
                xLog.Error(eImg, "WindgetBackground");
            }
            return null;
        }

        public static RemoteViews GetClockAnalogRemoteView(Context ctx, WidgetCfg_ClockAnalog cfg, WidgetView_ClockAnalog clockView, int iClockSize,
            LocationTimeHolder lth, DateTime tNow, Android.Net.Uri uBackgroundImage, Bitmap bmpBackgroundColor, bool bUpdateAll)
        {
            return GetClockAnalogRemoteView(ctx, cfg, clockView, iClockSize, lth, tNow.TimeOfDay.TotalHours % 12, tNow.TimeOfDay.TotalMinutes % 60, tNow.TimeOfDay.TotalSeconds % 60, uBackgroundImage, bmpBackgroundColor, bUpdateAll);
        }

        static TimeSpan tsClockImage = TimeSpan.FromTicks(0);
        public static RemoteViews GetClockAnalogRemoteView(Context ctx, WidgetCfg_ClockAnalog cfg, WidgetView_ClockAnalog clockView, int iClockSize,
            LocationTimeHolder lth, double nHour, double nMinute, double nSecond, Android.Net.Uri uBackgroundImage, Bitmap bmpBackgroundColor, bool bUpdateAll)
        {
            int iWidgetId = cfg.WidgetId;
            var tType = cfg.CurrentTimeType;

            var swStart = DateTime.Now;
            Bitmap bitmap = BitmapFactory.DecodeStream(clockView.GetBitmap(nHour, nMinute, nSecond, iClockSize, iClockSize, false));
            tsClockImage = DateTime.Now - swStart;

            RemoteViews updateViews = new RemoteViews(ctx.PackageName, Resource.Layout.widget_clock);

            string cTitle = cfg.WidgetTitle;
            if (string.IsNullOrEmpty(cTitle))
                cTitle = sys.DezimalGradToGrad(lth.Latitude, lth.Longitude);

            updateViews.SetImageViewBitmap(Resource.Id.analog_clock, bitmap);
            updateViews.SetTextViewText(Resource.Id.clock_title, cTitle);
            updateViews.SetTextColor(Resource.Id.clock_title, cfg.ColorTitleText.ToAndroid());
            updateViews.SetTextColor(Resource.Id.clock_time, cfg.ColorTitleText.ToAndroid());

            if (bUpdateAll)
            {
                updateViews.SetImageViewBitmap(Resource.Id.background_color, bmpBackgroundColor);
                updateViews.SetImageViewUri(Resource.Id.background_image, uBackgroundImage);
                updateViews.SetInt(Resource.Id.background_image, "setColorFilter", cfg.BackgroundImageTint.ToAndroid());

                updateViews.SetImageViewBitmap(Resource.Id.time_switcher, Tools.GetTimeTypeIcon(ctx, tType, lth, 32, cfg.ColorTitleText.HexString));

                Intent changeTypeIntent = new Intent(ctx, typeof(AnalogClockWidget));
                changeTypeIntent.SetAction(MainWidgetBase.ActionChangeTimeType);
                changeTypeIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, iWidgetId);
                changeTypeIntent.PutExtra(MainWidgetBase.ExtraTimeType, (int)MainWidgetBase.GetOtherTimeType(cfg.CurrentTimeType, cfg.WidgetTimeType));
                PendingIntent changeTypePendingIntent = PendingIntent.GetBroadcast(ctx, iWidgetId, changeTypeIntent, PendingIntentFlags.UpdateCurrent);
                updateViews.SetOnClickPendingIntent(Resource.Id.time_switcher, changeTypePendingIntent);
            }

            updateViews.SetOnClickPendingIntent(Resource.Id.ll_click, MainWidgetBase.GetClickActionPendingIntent(ctx, cfg.ClickAction, iWidgetId, "me.ichrono.droid/me.ichrono.droid.Widgets.Clock.AnalogClockWidgetConfigActivity"));

            return updateViews;
        }

        public static Android.Net.Uri GrandImageAccessToLaunchers(Context ctx, Java.IO.File fImage)
        {
            string auth = "me.ichrono.droid.fileprovider";
            var uRes = FileProvider.GetUriForFile(ctx, auth, fImage);

            //granet Image-Access to all Launchers
            Intent intent = new Intent(Intent.ActionMain);
            intent.AddCategory(Intent.CategoryHome);
            var resInfoList = ctx.PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchAll);
            foreach (ResolveInfo resolveInfo in resInfoList)
            {
                String packageName = resolveInfo.ActivityInfo.PackageName;
                ctx.GrantUriPermission(packageName, uRes, ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantPrefixUriPermission | ActivityFlags.GrantWriteUriPermission);
            }
            return uRes;
        }
    }
    */
}