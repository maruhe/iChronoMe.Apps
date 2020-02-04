﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Droid.Widgets;
using iChronoMe.Droid.Widgets.ActionButton;
using iChronoMe.Droid.Widgets.Calendar;
using iChronoMe.Droid.Widgets.Clock;
using iChronoMe.Droid.Widgets.Lifetime;
using iChronoMe.Widgets;

namespace iChronoMe.Droid
{
    [Service(Label = "Widget Update-Service", Exported = true)]
    public class BackgroundService : Service
    {
        public static WidgetUpdateThreadHolder updateHolder { get; private set; }
        public static BackgroundService currentService { get; private set; }
        public static Location lastLocation { get; set; }
        public static List<int> EffectedWidges { get; private set; } = new List<int>();

        public static string cLauncherName { get; private set; } = null;

        ClockUpdateBroadcastReceiver mReceiver;
        AppWidgetManager manager;

        public override void OnCreate()
        {
            base.OnCreate();
            currentService = this;

            manager = AppWidgetManager.GetInstance(this);

            IntentFilter intentFilter = new IntentFilter(ClockUpdateBroadcastReceiver.intentFilter);
            mReceiver = new ClockUpdateBroadcastReceiver();
            mReceiver.CommandReceived += MReceiver_CommandReceived;
            this.RegisterReceiver(mReceiver, intentFilter);

            Task.Factory.StartNew(() =>
            {
                //archive lost configs
                int[] appWidgetID1s = manager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(AnalogClockWidget)).Name));
                int[] appWidgetID2s = manager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(CalendarWidget)).Name));
                int[] appWidgetID3s = manager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(LifetimeWidget)).Name));
                int[] appWidgetID4s = manager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(ActionButtonWidget)).Name));
                List<int> iS = new List<int>();
                iS.AddRange(appWidgetID1s);
                iS.AddRange(appWidgetID2s);
                iS.AddRange(appWidgetID3s);
                iS.AddRange(appWidgetID4s);
                var holder = new WidgetConfigHolder();
                var holderArc = new WidgetConfigHolder(true);
                int iDeleted = 0;
                foreach (int iWidget in holder.AllIds())
                {
                    if (iWidget >= 0 && !iS.Contains(iWidget))
                    {
                        try
                        {
                            int iArchivId = iWidget;
                            while (holderArc.WidgetExists(iArchivId))
                                iArchivId++;
                            var cfg = holder.GetWidgetCfg<WidgetCfg>(iWidget);
                            cfg.WidgetId = iArchivId;
                            holderArc.SetWidgetCfg(cfg, false);
                        }
                        catch { }
                        holder.DeleteWidget(iWidget, false);
                        iDeleted++;
                    }
                }
                if (iDeleted > 0)
                {
                    holder.SaveToFile();
                    holderArc.SaveToFile();
                }
            });
        }

        private LinearLayout mLayout;
        private IWindowManager mManager;

        public void CreateFlowWind()
        {
            mManager = GetSystemService(WindowService).JavaCast<IWindowManager>();
            mLayout = new LinearLayout(this);
            LinearLayout.LayoutParams mParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent);
            mLayout.SetBackgroundColor(Color.Rgb(255, 255, 0));
            mLayout.LayoutParameters = mParams;

            WindowManagerTypes lFlag = WindowManagerTypes.Phone;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                lFlag = WindowManagerTypes.ApplicationPanel;

            WindowManagerLayoutParams parameters = new WindowManagerLayoutParams(400, 150, lFlag, WindowManagerFlags.NotTouchable | WindowManagerFlags.LayoutInScreen | WindowManagerFlags.NotFocusable, Android.Graphics.Format.Translucent);
            parameters.X = 0;
            parameters.Y = 0;
            parameters.Gravity = GravityFlags.Center;
            mManager.AddView(mLayout, parameters);
        }

        private void MReceiver_CommandReceived(string command, string baseaction)
        {
            if (ClockUpdateBroadcastReceiver.cmdStopUpdates.Equals(command))
                StopUpdate(true);
            else if (ClockUpdateBroadcastReceiver.cmdRestartUpdates.Equals(command))
                RestartUpdateDelay();
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            RestartUpdateDelay();

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            StopUpdate();
            base.OnDestroy();
        }

        bool bIsForeGround = false;
        void RegisterForegroundService()
        {
            if (bIsForeGround)
                return;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.NMr1
                || AppConfigHolder.MainConfig.AlwaysShowForegroundNotification)
            {
                Notification notification = GetForegroundNotification();
                // Enlist this instance of the service as a foreground service
                StartForeground(101, notification);
                bIsForeGround = true;
            }
        }

        public Notification GetForegroundNotification(string cTitle = "Hintergrunddienst", string cText = "damit die Widges laufen..", WidgetCfgClickAction clickAction = WidgetCfgClickAction.OpenSettings)
        {
            string channelId = Build.VERSION.SdkInt >= BuildVersionCodes.O ? createNotificationChannel() : null;

            Notification notification = new Android.Support.V4.App.NotificationCompat.Builder(this, channelId)
                .SetContentTitle(cTitle)
                .SetContentText(cText)
                .SetSmallIcon(Resource.Drawable.sunclock)
                .SetContentIntent(BuildIntentToShowMainActivity(clickAction))
                .SetOngoing(true)
                .Build();

            return notification;
        }

        private string createNotificationChannel()
        {
            var channelId = "widget_service";
            var channelName = "Widget Update-Service";
            var chan = new NotificationChannel(channelId, channelName, NotificationImportance.Min);
            chan.Description = "keeps the Clock-Widges going on :-)";
            chan.LightColor = Color.Blue;
            chan.LockscreenVisibility = NotificationVisibility.Private;
            var service = GetSystemService(NotificationService) as NotificationManager;
            service.CreateNotificationChannel(chan);
            return channelId;
        }

        private void xxRestartUpdate()
        {
            StopUpdate();
            EffectedWidges.Clear();

            int[] appWidgetID2s = manager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(AnalogClockWidget)).Name));

            if (!AppConfigHolder.MainConfig.AlwaysShowForegroundNotification &&
                appWidgetID2s.Length == 0)
            {
                //warum auf zu??
                //if (Build.VERSION.SdkInt >= BuildVersionCodes.O && !bIsForeGround)
                  //  RegisterForegroundService();
                if (bIsForeGround)
                {
                    StopForeground(true);
                    bIsForeGround = false;
                }
                StopSelf();
                return;
            }

            if ((new WidgetConfigHolder().AllIds<WidgetCfg_ClockAnalog>().Length > 0 && Build.VERSION.SdkInt >= BuildVersionCodes.NMr1) || AppConfigHolder.MainConfig.AlwaysShowForegroundNotification)
            {
                if (!bIsForeGround)
                {
                    RegisterForegroundService();
                }

                updateHolder = new WidgetUpdateThreadHolder(this);
            }
        }

        Task tskRestartUpdateDelay = null;
        DateTime tLastRestartUpdateDelay = DateTime.MinValue;
        public void RestartUpdateDelay()
        {
            int iDelay = 250;
            if (tLastRestartUpdateDelay.AddSeconds(5) > DateTime.Now)
                iDelay = 1500;
            tLastRestartUpdateDelay = DateTime.Now;

            if (tskRestartUpdateDelay == null)
            {
                tskRestartUpdateDelay = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Task.Delay(iDelay);
                        xxRestartUpdate();
                    }
                    catch (Exception ex)
                    {
                        xLog.Error(ex, "RestartUpdate");
                    }
                    tskRestartUpdateDelay = null;
                }
                );
            }
        }

        public void StopUpdate(bool bCheckStopAll = false)
        {
            if (updateHolder != null)
                updateHolder.Stop();

            if (bCheckStopAll)
            {
                if (!AppConfigHolder.MainConfig.AlwaysShowForegroundNotification)
                {
                    int[] appWidgetID2s = manager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(AnalogClockWidget)).Name));

                    if (appWidgetID2s.Length == 0)
                    {
                        if (bIsForeGround)
                        {
                            StopForeground(true);
                            bIsForeGround = false;
                        }
                        return;
                    }
                }
            }
        }

        public static void RestartService(Context context, string cAction)
        {
            bool running = IsServiceRunning(context, typeof(BackgroundService));
            if (running)
            {
                try { running = updateHolder.IsAlive; }
                catch { running = false; }
            }
            if (!running)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    context.StartForegroundService(new Intent(context, typeof(BackgroundService)));
                }
                else
                {
                    context.StartService(new Intent(context, typeof(BackgroundService)));
                }
            }
            else
            {
                Intent update_widget = new Intent();
                update_widget.SetAction(ClockUpdateBroadcastReceiver.intentFilter);
                update_widget.PutExtra(ClockUpdateBroadcastReceiver.command, ClockUpdateBroadcastReceiver.cmdRestartUpdates);
                update_widget.PutExtra(ClockUpdateBroadcastReceiver.baseaction, cAction);
                context.SendBroadcast(update_widget);
            }
        }

        public static bool IsServiceRunning(Context context, System.Type ClassTypeof)
        {
            ActivityManager manager = (ActivityManager)context.GetSystemService(ActivityService);
            var srvS = manager.GetRunningServices(int.MaxValue);
            foreach (var service in srvS)
            {
                if (service.Service.ShortClassName.EndsWith(ClassTypeof.Name))
                {
                    return true;
                }
            }
            return false;
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        PendingIntent BuildIntentToShowMainActivity(WidgetCfgClickAction clickAction)
        {
            if (clickAction == WidgetCfgClickAction.None)
                return null;

            Type tView = typeof(BackgroundServiceInfoActivity);
            if (clickAction == WidgetCfgClickAction.OpenApp)
                tView = typeof(MainActivity);

            var notificationIntent = new Intent(this, tView);
            notificationIntent.SetAction(Intent.ActionMain);
            if (clickAction == WidgetCfgClickAction.OpenApp)
                notificationIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTask);
            else
                notificationIntent.SetFlags(ActivityFlags.NoHistory);

            var pendingIntent = PendingIntent.GetActivity(this, 101, notificationIntent, PendingIntentFlags.CancelCurrent);
            return pendingIntent;
        }
    }

    public class WidgetUpdateThreadHolder : Java.Lang.Object, ILocationListener
    {
        public int RunningThreads { get; private set; } = 0;
        public bool IsAlive { get => RunningThreads > 0; }

        bool bRunning = true;
        LocationTimeHolder lthLocal = null;
        WidgetConfigHolder cfgHolder = null;
        static LocationManager locationManager;
        public static Location lastLocation { get => BackgroundService.lastLocation; set => BackgroundService.lastLocation = value; }

        AppWidgetManager manager = null;
        PowerManager pm = null;

        public WidgetUpdateThreadHolder(Context ctx)
        {
            manager = AppWidgetManager.GetInstance(ctx);
            pm = (PowerManager)ctx.GetSystemService(Context.PowerService);
            cfgHolder = new WidgetConfigHolder();

            int[] appWidgetID2s = manager.GetAppWidgetIds(new ComponentName(ctx, Java.Lang.Class.FromType(typeof(AnalogClockWidget)).Name));
            List<int> iS = new List<int>();
            iS.AddRange(appWidgetID2s);
            if ((cfgHolder.AllIds<WidgetCfg_ClockAnalog>().Length > 0 && Build.VERSION.SdkInt >= BuildVersionCodes.NMr1) || AppConfigHolder.MainConfig.AlwaysShowForegroundNotification)
            {
                Notification notification = BackgroundService.currentService.GetForegroundNotification("Hintergrunddienst", sys.EzMzText(iS.Count, "Ein Widget aktiv", "{0} Widges aktiv", "keine Widges aktiv"), cfgHolder.GetWidgetCfg<WidgetCfg_Clock>(-101).ClickAction);
                NotificationManager mNotificationManager = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
                mNotificationManager.Notify(101, notification);

                if (AppConfigHolder.MainConfig.AlwaysShowForegroundNotification || iS.Count > 0)
                {
                    iS.Add(-101); //Uhrzeit in Notification anzeigen
                    if (!cfgHolder.WidgetExists<WidgetCfg_ClockAnalog>(-101)) {
                        var tmp = cfgHolder.GetWidgetCfg<WidgetCfg_ClockAnalog>(-101);
                        tmp.PositionType = WidgetCfgPositionType.LivePosition;
                        cfgHolder.SetWidgetCfg(tmp);
                    }
                }
            }
            int[] appWidgetIDs = iS.ToArray();

            //CheckLocationNeedet
            foreach (int iWidgetId in appWidgetIDs)
            {
                WidgetCfg_ClockAnalog cfg = cfgHolder.GetWidgetCfg<WidgetCfg_ClockAnalog>(iWidgetId, false);
                if (cfg?.PositionType == WidgetCfgPositionType.LivePosition)
                {
                    EnableLocationUpdate(ctx);
                    break;
                }
            }

            //Start one Thread per Widget
            foreach (int iWidgetId in appWidgetIDs)
            {
                var tsk = new Thread(() =>
                {
                    xLog.Debug("start new Thread for AnalogClock " + iWidgetId);
                    DateTime swStart = DateTime.Now;
                    WidgetCfg_ClockAnalog cfg = cfgHolder.GetWidgetCfg<WidgetCfg_ClockAnalog>(iWidgetId, false);
                    if (cfg == null)
                        return;
                    if (cfg.PositionType == WidgetCfgPositionType.None)
                    {
                        RemoteViews updateViews = new RemoteViews(ctx.PackageName, Resource.Layout.widget_unconfigured);

                        Intent defineIntent = new Intent(Intent.ActionMain);
                        defineIntent.SetComponent(ComponentName.UnflattenFromString("me.ichrono.droid/me.ichrono.droid.Widgets.Clock.ClockWidgetConfigActivity"));
                        defineIntent.SetFlags(ActivityFlags.NoHistory);
                        defineIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, iWidgetId);
                        PendingIntent pendingIntent = PendingIntent.GetActivity(ctx, iWidgetId, defineIntent, PendingIntentFlags.CancelCurrent);
                        updateViews.SetOnClickPendingIntent(Resource.Id.widget, pendingIntent);

                        manager.UpdateAppWidget(iWidgetId, updateViews);
                        return;
                    }

                    if (cfg.PositionType == WidgetCfgPositionType.LivePosition)
                    {
                        if (BackgroundService.lastLocation != null)
                        {
                            cfg.Latitude = BackgroundService.lastLocation.Latitude;
                            cfg.Longitude = BackgroundService.lastLocation.Longitude;
                        }
                    }

                    LocationTimeHolder lth = null;
                    if (cfg.PositionType == WidgetCfgPositionType.LivePosition || cfg.WidgetId == -101)
                    {
                        //Alle Widges mit lokaler Position bekommen den selben Holder
                        if (lthLocal != null)
                            lth = lthLocal;
                        else
                        {
                            lth = lthLocal = LocationTimeHolder.LocalInstance;
                            lth.ChangePositionDelay(cfg.Latitude, cfg.Longitude);
                            lthLocal.AreaChanged += LthLocal_AreaChanged;
                        }
                    }
                    else
                    {
                        lth = LocationTimeHolder.NewInstanceDelay(cfg.Latitude, cfg.Longitude);
                        lth.AreaChanged += LthLocal_AreaChanged;
                    }

                    Point wSize = MainWidgetBase.GetWidgetSize(iWidgetId, cfg, manager);
                    int iClockSizeDp = wSize.X;
                    if (wSize.Y < wSize.X)
                        iClockSizeDp = wSize.Y;
                    int iClockSize = (int)(iClockSizeDp * sys.DisplayDensity);

                    WidgetView_ClockAnalog clockView = new WidgetView_ClockAnalog();
                    clockView.ReadConfig(cfg);

                    Android.Net.Uri uBackgroundImage = null;
                    DateTime tBackgroundUpdate = DateTime.MinValue;
                    try
                    {
                        if (!string.IsNullOrEmpty(cfg.BackgroundImage))
                        {
                            string cBackImgPath = cfg.BackgroundImage;
                            if (!cfg.BackgroundImage.Contains("/"))
                                cfg.BackgroundImage = System.IO.Path.Combine(System.IO.Path.Combine(sys.PathShare, "imgCache_clockface"), cfg.BackgroundImage);
                            Java.IO.File fBack = new Java.IO.File(cfg.BackgroundImage);
                            bool bBackEx = fBack.Exists();
                            if (bBackEx)
                            {
                                uBackgroundImage = GrandImageAccessToLaunchers(ctx, fBack);
                            }
                        }
                    }
                    catch (Exception eImg)
                    {
                        xLog.Error(eImg, "WindgetBackground");
                    }
                    Bitmap bmpBackgroundColor = null;
                    if (cfg.ColorBackground.ToAndroid() != Color.Transparent)
                    {
                        GradientDrawable shape = new GradientDrawable();
                        shape.SetShape(ShapeType.Oval);
                        shape.SetColor(cfg.ColorBackground.ToAndroid());
                        bmpBackgroundColor = MainWidgetBase.GetDrawableBmp(shape, iClockSizeDp, iClockSizeDp);
                    }

                    TimeSpan swInit = DateTime.Now - swStart;

                    TimeType tType = cfg.ShowTimeType;
                    DateTime tLastDateRefresh = DateTime.MinValue;
                    DynamicDate dDay = DynamicDate.EmptyDate;

                    RunningThreads++;
                    int iRun = 10;
                    DateTime tLastRun = DateTime.MinValue;
                    while (bRunning)
                    {
                        try
                        {
                            iRun++;
                            xLog.Verbose("AnalogClock " + iWidgetId + "\tRun " + iRun);
                            while (!IsInteractive && (tLastRun.AddMinutes(5) > DateTime.Now) && bRunning)
                                Thread.Sleep(250);

                            if (Build.VERSION.SdkInt >= BuildVersionCodes.LollipopMr1)
                                if (pm.IsPowerSaveMode)
                                    clockView.ShowSecondHand = clockView.FlowMinuteHand = false;

                            swStart = DateTime.Now;
                            if (cfg.PositionType == WidgetCfgPositionType.LivePosition)
                            {
                                if (lastLocation != null)
                                {
                                    if (cfg.Latitude != lastLocation.Latitude || cfg.Longitude != lastLocation.Longitude)
                                    {
                                        cfg.Latitude = lastLocation.Latitude;
                                        cfg.Longitude = lastLocation.Longitude;
                                        lth.ChangePositionDelay(cfg.Latitude, cfg.Longitude);
                                        SaveWidgetConfig();
                                    }
                                }
                                if (!string.IsNullOrEmpty(lth.AreaName) && cfg.WidgetTitle != lth.AreaName)
                                {
                                    cfg.WidgetTitle = lth.AreaName;
                                    BackgroundService.EffectedWidges.Clear();
                                    SaveWidgetConfig();
                                }
                            }
                            else if (string.IsNullOrEmpty(cfg.WidgetTitle) && !string.IsNullOrEmpty(lth.AreaName))
                            {
                                cfg.WidgetTitle = lth.AreaName;
                                BackgroundService.EffectedWidges.Clear();
                                SaveWidgetConfig();
                            }
                            TimeSpan swPosCheck = DateTime.Now - swStart;

                            swStart = DateTime.Now;
                            TimeSpan swUpdateClock = DateTime.Now - swStart;

                            if (iWidgetId >= 0)
                            {
                                swStart = DateTime.Now;
                                Bitmap bitmap = BitmapFactory.DecodeStream(clockView.GetBitmap(lth.GetTime(tType), iClockSize, iClockSize));
                                TimeSpan swGetClockBmp = DateTime.Now - swStart;

                                RemoteViews updateViews = new RemoteViews(ctx.PackageName, Resource.Layout.widget_clock);

                                string cTitle = cfg.WidgetTitle;
                                if (string.IsNullOrEmpty(cTitle))
                                    cTitle = sys.DezimalGradToGrad(lth.Latitude, lth.Longitude);

                                updateViews.SetImageViewBitmap(Resource.Id.analog_clock, bitmap);
                                updateViews.SetTextViewText(Resource.Id.clock_title, cTitle);
                                updateViews.SetTextColor(Resource.Id.clock_title, cfg.ColorTitleText.ToAndroid());
                                updateViews.SetTextColor(Resource.Id.clock_time, cfg.ColorTitleText.ToAndroid());

                                if (tLastRun.AddSeconds(15) < DateTime.Now)
                                {
                                    updateViews.SetImageViewBitmap(Resource.Id.background_color, bmpBackgroundColor);
                                    if (iRun > 1)
                                        updateViews.SetImageViewUri(Resource.Id.background_image, uBackgroundImage);

                                    updateViews.SetImageViewResource(Resource.Id.time_switcher, MainWidgetBase.GetTimeTypeIcon(tType, lth));

                                    if (cfg.ShowTimeType == TimeType.RealSunTime || cfg.ShowTimeType == TimeType.TimeZoneTime)
                                    {
                                        Intent changeTypeIntent = new Intent(ctx, typeof(AnalogClockWidget));
                                        changeTypeIntent.SetAction(MainWidgetBase.ActionChangeTimeType);
                                        changeTypeIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, iWidgetId);
                                        if (cfg.ShowTimeType != TimeType.RealSunTime)
                                            changeTypeIntent.PutExtra(MainWidgetBase.ExtraTimeType, (int)TimeType.RealSunTime);
                                        else
                                            changeTypeIntent.PutExtra(MainWidgetBase.ExtraTimeType, (int)TimeType.TimeZoneTime);
                                        PendingIntent changeTypePendingIntent = PendingIntent.GetBroadcast(ctx, iWidgetId, changeTypeIntent, PendingIntentFlags.UpdateCurrent);
                                        updateViews.SetOnClickPendingIntent(Resource.Id.time_switcher, changeTypePendingIntent);
                                    }
                                    else
                                    {
                                        updateViews.SetOnClickPendingIntent(Resource.Id.time_switcher, null);
                                    }
                                }

                                PendingIntent pendingIntent = null;
                                if (cfg.ClickAction == WidgetCfgClickAction.OpenSettings)
                                {
                                    Intent defineIntent = new Intent(Intent.ActionMain);
                                    defineIntent.SetComponent(ComponentName.UnflattenFromString("me.ichrono.droid/me.ichrono.droid.Widgets.Clock.ClockWidgetConfigActivity"));
                                    defineIntent.SetFlags(ActivityFlags.NoHistory);
                                    defineIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, iWidgetId);
                                    pendingIntent = PendingIntent.GetActivity(ctx, iWidgetId, defineIntent, PendingIntentFlags.CancelCurrent);
                                }
                                else if (cfg.ClickAction == WidgetCfgClickAction.OpenApp)
                                {
                                    var notificationIntent = new Intent(ctx, typeof(MainActivity));
                                    notificationIntent.SetAction(Intent.ActionMain);
                                    notificationIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTask);
                                    pendingIntent = PendingIntent.GetActivity(ctx, 0, notificationIntent, PendingIntentFlags.UpdateCurrent);
                                }
                                if (cfg.ClickAction != WidgetCfgClickAction.None)
                                {
                                    updateViews.SetOnClickPendingIntent(Resource.Id.widget, pendingIntent);
                                }

                                manager.UpdateAppWidget(iWidgetId, updateViews);
                            }
                            else
                            {
                                if (iWidgetId == -101)
                                {//Notification 

                                    if (dDay.IsEmpty || tLastDateRefresh.Date != DateTime.Now.Date || tLastDateRefresh.AddMinutes(5) <= DateTime.Now)
                                    {
                                        dDay = new CalendarModelCfgHolder().GetModelCfg(cfg.CalendarModelId).GetDateFromUtcDate(DateTime.Now);
                                        tLastDateRefresh = DateTime.Now;
                                    }

                                    string cTitle = cfg.WidgetTitle;
                                    string cText = dDay.ToString("ddd, ") + dDay.ToString("_mMd") + lth.GetTime(tType).ToString(", HH:mm");
                                    if (true)
                                    {
                                        cTitle = cText;
                                        cText = cfg.WidgetTitle + ", " + sys.DezimalGradToGrad(cfg.Latitude, true, false) + ", " + sys.DezimalGradToGrad(cfg.Longitude, false, false);
                                    }

                                    Notification notification = BackgroundService.currentService.GetForegroundNotification(cTitle, cText, cfg.ClickAction);

                                    NotificationManager mNotificationManager = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
                                    mNotificationManager.Notify(101, notification);
                                }
                            }

                            tLastRun = DateTime.Now;
                            xLog.Verbose("UpdateDone: AnalogClock " + iWidgetId);

                            if (!BackgroundService.EffectedWidges.Contains(iWidgetId))
                                BackgroundService.EffectedWidges.Add(iWidgetId);

                            if (iRun == 1)
                            {
                                //Beim ersten mal etwas warten, Hindergrundbildrechte übernhemen, und dann geht's los..
                                Thread.Sleep(500);
                            }
                            else
                            {

                                if (clockView.ShowSecondHand && clockView.FlowSecondHand)
                                    Thread.Sleep(35);
                                else
                                {
                                    Thread.Sleep(1000 - lth.GetTime(tType).Millisecond);
                                    if (!clockView.ShowSecondHand)
                                    {
                                        if (lth.GetTime(tType).Second % 2 != 0)
                                            Thread.Sleep(1000 - lth.GetTime(tType).Millisecond);
                                        if (!clockView.ShowMinuteHand || !clockView.FlowMinuteHand)
                                        {
                                            int iSleeSecondes = 58 - lth.GetTime(tType).Second - 1;
                                            for (int iSec = 0; iSec < iSleeSecondes / 5; iSec++)
                                            {
                                                Thread.Sleep(5000);
                                                if (!BackgroundService.EffectedWidges.Contains(iWidgetId))
                                                    break;
                                            }

                                            Thread.Sleep(1000 - lth.GetTime(tType).Millisecond);

                                            int iSecond = lth.GetTime(tType).Second;
                                            while (iSecond > 30 && iSecond <= 59)
                                            {
                                                Thread.Sleep(1000 - lth.GetTime(tType).Millisecond);
                                                iSecond = lth.GetTime(tType).Second;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            xLog.Error(e, "OnUpdateWidget " + iWidgetId);
                            Thread.Sleep(1000);
                        }
                    }
                    RunningThreads--;
                });
                tsk.IsBackground = true;
                tsk.Start();
            }
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

        bool IsInteractive
        {
            get
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.KitkatWatch)
                    return pm.IsScreenOn;
                return pm.IsInteractive;
            }
        }

        private void LthLocal_AreaChanged(object sender, AreaChangedEventArgs e)
        {
            BackgroundService.EffectedWidges.Clear();
        }

        Task cfgSaveTask = null;
        void SaveWidgetConfig()
        {
            if (cfgSaveTask != null)
                return;

            cfgSaveTask = Task.Factory.StartNew(async () =>
            {
                DateTime dLastSave = System.IO.File.GetLastWriteTime(cfgHolder.CfgFile);
                await Task.Delay(5000);
                if (!bRunning)
                    return;
                if (!dLastSave.Equals(System.IO.File.GetLastWriteTime(cfgHolder.CfgFile)))
                    return;

                cfgHolder.SaveToFile();
                cfgSaveTask = null;
            });
        }

        public void Stop()
        {
            bRunning = false;
            DisableLocationUpdate();
        }

        bool bGetNetworkLocation = false;

        int minTime = 15000;
        int minDistance = 25;

        protected void EnableLocationUpdate(Context ctx)
        {
            xLog.Debug("EnableLocationUpdate");
            if (locationManager == null)
            {
                Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        xLog.Debug("EnableLocationUpdate: init");
                        locationManager = (LocationManager)ctx.GetSystemService(Context.LocationService);

                        if (lastLocation == null)
                            lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);
                        if (lastLocation == null)
                            lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);

                        xLog.Debug("EnableLocationUpdate: got last: " + (lastLocation != null ? lastLocation.Provider : "false"));

                        try
                        {
                            xLog.Debug("EnableLocationUpdate: passive");
                            locationManager.RequestLocationUpdates(LocationManager.PassiveProvider, minTime, minDistance, this);
                            xLog.Debug("EnableLocationUpdate: passive: done");
                        }
                        catch (Exception e)
                        {
                            xLog.Debug("EnableLocationUpdate: passive: " + e.Message);
                        }
                        try
                        {
                            xLog.Debug("EnableLocationUpdate: network");
                            locationManager.RequestLocationUpdates(LocationManager.NetworkProvider, minTime, minDistance, this);
                            bGetNetworkLocation = true;
                            xLog.Debug("EnableLocationUpdate: network: done");
                        }
                        catch (Exception e)
                        {
                            xLog.Debug("EnableLocationUpdate: network: " + e.Message);
                        }

                        if (!bGetNetworkLocation)
                        {
                            try
                            {
                                xLog.Debug("EnableLocationUpdate: gps");
                                locationManager.RequestLocationUpdates(LocationManager.GpsProvider, minTime, minDistance, this);
                                xLog.Debug("EnableLocationUpdate: gps: done");
                            }
                            catch (Exception e)
                            {
                                xLog.Debug("EnableLocationUpdate: gps: " + e.Message);
                            }
                        }

                        xLog.Debug("EnableLocationUpdate: done");
                    }
                    catch (Exception exLoc)
                    {
                        xLog.Error(exLoc, "LocationException: " + exLoc.Message);
                    }
                });
            }
        }

        protected void DisableLocationUpdate()
        {
            xLog.Debug("DisableLocationUpdate!!!!!");
            if (locationManager != null)
            {
                locationManager.RemoveUpdates(this);
                locationManager = null;
            }
            bGetNetworkLocation = false;
        }
        public void OnLocationChanged(Location location)
        {
            if (lastLocation?.Latitude == location.Latitude && lastLocation?.Longitude == location.Longitude)
                return;
            lastLocation = location;
            BackgroundService.EffectedWidges.Clear();
            xLog.Debug("GotLocation; " + location.Provider);
        }

        public void OnProviderDisabled(string provider)
        {
            if (LocationManager.NetworkProvider.Equals(provider))
                bGetNetworkLocation = false;
        }

        public void OnProviderEnabled(string provider)
        {

        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {

        }

        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: verwalteten Zustand (verwaltete Objekte) entsorgen.
                }

                // TODO: nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer weiter unten überschreiben.
                // TODO: große Felder auf Null setzen.

                disposedValue = true;
            }
        }

        // TODO: Finalizer nur überschreiben, wenn Dispose(bool disposing) weiter oben Code für die Freigabe nicht verwalteter Ressourcen enthält.
        // ~WidgetUpdateThreadHolder()
        // {
        //   // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
        //   Dispose(false);
        // }

        // Dieser Code wird hinzugefügt, um das Dispose-Muster richtig zu implementieren.
        void IDisposable.Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
            Dispose(true);
            // TODO: Auskommentierung der folgenden Zeile aufheben, wenn der Finalizer weiter oben überschrieben wird.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}