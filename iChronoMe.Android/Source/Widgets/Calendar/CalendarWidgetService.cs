using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V4.App;
using Android.Text;
using Android.Text.Style;
using Android.Util;
using Android.Views;
using Android.Widget;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Core.Types;
using iChronoMe.DeviceCalendar;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.Calendar
{
    [Service(Label = "Kalender-Widget Update-Service", Permission = "android.permission.BIND_JOB_SERVICE", Exported = true)]
    public class CalendarWidgetService : JobIntentService
    {
        public override void OnCreate()
        {
            base.OnCreate();

            SetTheme(Resource.Style.AppTheme_iChronoMe_Dark);
        }

        public static bool ResetData = false;
        public static bool NewWidget = false;
        const int MY_JOB_ID = 2124;

        public static void EnqueueWork(Context context, Intent work)
        {
            Java.Lang.Class cls = Java.Lang.Class.FromType(typeof(CalendarWidgetService));
            try
            {
                EnqueueWork(context, cls, MY_JOB_ID, work);
            }
            catch (Exception ex)
            {
                xLog.Debug(ex, "Exception: {0}");
            }
        }

        public static void InitEvents()
        {
            if (myEvents == null)
                myEvents = new EventCollection();
        }

        static Dictionary<int, Thread> RunningTaskS = new Dictionary<int, Thread>();
        static bool bHasPermissions = Build.VERSION.SdkInt < BuildVersionCodes.M;
        static EventCollection myEvents = null;

        static object oLock = new object();

        protected override void OnHandleWork(Intent intent)
        {
            xLog.Debug("OnHandleWork");

            lock (oLock)
            {
                CalendarModelCfgHolder.BaseGregorian.BaseSample.ToString();

                var cfgHolder = new WidgetConfigHolder();

                var appWidgetManager = AppWidgetManager.GetInstance(this);
                int[] appWidgetIDs = intent.GetIntArrayExtra(AppWidgetManager.ExtraAppwidgetIds);
                if (appWidgetIDs == null || appWidgetIDs.Length < 1)
                    appWidgetIDs = appWidgetManager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(CalendarWidget)).Name));

                if (!bHasPermissions)
                {
                    xLog.Debug("PermissionCheck");
                    try
                    {
                        bool bPermissionError = false;
                        if (this.CheckSelfPermission(Android.Manifest.Permission.WriteCalendar) != Android.Content.PM.Permission.Granted)
                            bPermissionError = true;

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

                new Thread(() =>
                {
                    xLog.Debug("Start update " + appWidgetIDs.Length + " Widgets");
                    if (myEvents == null)
                        InitEvents();

                    //lock (calendarsImpl)
                    {
                        foreach (int iWidgetId in appWidgetIDs)
                        {
                            try
                            {
                                lock (RunningTaskS)
                                {
                                    if (RunningTaskS.ContainsKey(iWidgetId))
                                    {
                                        try
                                        {
                                            ResetData = false;
                                            RunningTaskS[iWidgetId].Abort();
                                        }
                                        catch { };
                                        if (RunningTaskS.ContainsKey(iWidgetId))
                                            RunningTaskS.Remove(iWidgetId);
                                    }
                                }

                                xLog.Debug("Start update Widget " + iWidgetId);
                                var cfg = cfgHolder.GetWidgetCfg<WidgetCfg_Calendar>(iWidgetId, false);
                                if (cfg == null)
                                    continue;

                                if (ResetData && cfg is WidgetCfg_CalendarTimetable)
                                {
                                    xLog.Debug("Send ResetData to ListService: " + cfg.GetType().Name);
                                    CalendarEventListService.ResetData = true;
                                    appWidgetManager.NotifyAppWidgetViewDataChanged(iWidgetId, Resource.Id.event_list);
                                    ResetData = false;
                                    continue;
                                }

                                var tr = new Thread(() =>
                                {
                                    try
                                    {

                                        Point wSize = MainWidgetBase.GetWidgetSize(iWidgetId, cfg, appWidgetManager);

                                        xLog.Debug("Widget Type: " + cfg.GetType().Name + ", Size: " + wSize.X + "x" + wSize.Y);

                                        RemoteViews rv = new RemoteViews(PackageName, Resource.Layout.widget_calendar_universal);

                                        if (NewWidget)
                                        {
                                            NewWidget = false;
                                            appWidgetManager.UpdateAppWidget(iWidgetId, rv);
                                        }

                                        DynamicCalendarModel calendarModel = new CalendarModelCfgHolder().GetModelCfg(cfg.CalendarModelId);

                                        var titleInfo = GenerateWidgetTitle(this, rv, cfg, wSize, calendarModel);

                                        rv.SetViewVisibility(Resource.Id.header_layout, ViewStates.Gone);
                                        rv.SetViewVisibility(Resource.Id.list_layout, ViewStates.Gone);
                                        rv.SetViewVisibility(Resource.Id.event_list, ViewStates.Gone);
                                        rv.SetViewVisibility(Resource.Id.circle_image, ViewStates.Gone);
                                        rv.SetViewVisibility(Resource.Id.empty_view, ViewStates.Visible);
                                        if (cfg is WidgetCfg_CalendarCircleWave)
                                            rv.SetViewPadding(Resource.Id.empty_view, 0, 0, 0, titleInfo.iHeaderHeight * (int)sys.DisplayDensity);
                                        else
                                            rv.SetViewPadding(Resource.Id.empty_view, 0, 0, 0, 0);

                                        if (cfg is WidgetCfg_CalendarMonthView)
                                        {
                                            rv.SetViewVisibility(Resource.Id.header_layout, ViewStates.Visible);
                                            rv.SetViewVisibility(Resource.Id.list_layout, ViewStates.Visible);
                                            rv.RemoveAllViews(Resource.Id.header_layout);
                                            rv.RemoveAllViews(Resource.Id.list_layout);
                                        }

                                        if (ResetData && !(cfg is WidgetCfg_CalendarCircleWave))
                                        {
                                            ResetData = false;
                                            appWidgetManager.UpdateAppWidget(iWidgetId, rv);
                                            Thread.Sleep(250);
                                        }

                                        if (cfg is WidgetCfg_CalendarTimetable)
                                        {
                                            rv.SetViewVisibility(Resource.Id.event_list, ViewStates.Visible);

                                            Intent adapterIntent = new Intent(this, typeof(CalendarEventListService));
                                            adapterIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, iWidgetId);
                                            adapterIntent.SetData(Android.Net.Uri.Parse(adapterIntent.ToUri(IntentUriType.Scheme)));
                                            rv.SetRemoteAdapter(Resource.Id.event_list, adapterIntent);

                                            Intent itemClickIntent = new Intent(Intent.ActionMain);
                                            itemClickIntent.SetComponent(ComponentName.UnflattenFromString("me.ichrono.droid/me.ichrono.droid.Widgets.WidgetItemClickActivity"));
                                            itemClickIntent.SetFlags(ActivityFlags.NoHistory);

                                            PendingIntent itemClickPendingIntent = PendingIntent.GetActivity(this, iWidgetId, itemClickIntent, PendingIntentFlags.UpdateCurrent);

                                            rv.SetPendingIntentTemplate(Resource.Id.event_list, itemClickPendingIntent);
                                            rv.SetEmptyView(Resource.Id.event_list, Resource.Id.empty_view);
                                            appWidgetManager.UpdateAppWidget(iWidgetId, rv);
                                        }
                                        else if (cfg is WidgetCfg_CalendarMonthView)
                                        {
                                            GenerateWidgetMonthView(this, rv, cfg as WidgetCfg_CalendarMonthView, wSize, titleInfo.iHeaderHeight, calendarModel);
                                        }
                                        else if (cfg is WidgetCfg_CalendarCircleWave)
                                        {
                                            GenerateCircleWaveView(this, appWidgetManager, rv, cfg as WidgetCfg_CalendarCircleWave, wSize, titleInfo.iHeaderHeight, calendarModel);
                                        }
                                        if (sys.Debugmode)
                                            rv.SetViewVisibility(Resource.Id.debug_text, ViewStates.Visible);
                                        appWidgetManager.UpdateAppWidget(iWidgetId, rv);

                                        if (cfg is WidgetCfg_CalendarTimetable)
                                            appWidgetManager.NotifyAppWidgetViewDataChanged(iWidgetId, Resource.Id.event_list);

                                        xLog.Debug("Update Widget done: " + iWidgetId);
                                    }
                                    catch (ThreadAbortException) { return; }
                                    catch (Exception ex)
                                    {
                                        if (ex.InnerException is ThreadAbortException)
                                            return;
                                        sys.LogException(ex);
                                        xLog.Error(ex, "Update Widget Error: " + iWidgetId);
                                        RemoteViews rv = new RemoteViews(PackageName, Resource.Layout.widget_unconfigured);
                                        rv.SetTextViewText(Resource.Id.message, "error loading widget:\n" + ex.Message + "\n" + ex.StackTrace);
                                        rv.SetTextColor(Resource.Id.message, Color.IndianRed);
                                        appWidgetManager.UpdateAppWidget(iWidgetId, rv);
                                    }
                                    lock (RunningTaskS)
                                    {
                                        if (RunningTaskS.ContainsKey(iWidgetId) && RunningTaskS[iWidgetId] == Thread.CurrentThread)
                                            RunningTaskS.Remove(iWidgetId);
                                    }
                                });
                                tr.IsBackground = true;
                                lock (RunningTaskS)
                                    RunningTaskS.Add(iWidgetId, tr);

                                tr.Start();
                                tr.Join();
                            }
                            catch { }
                        }
                    }
                })
                { IsBackground = true }.Start();
            }
        }

        public static (bool bAllDone, int iHeaderHeight) GenerateWidgetTitle(Context context, RemoteViews rv, WidgetCfg_Calendar cfg, Point wSize, DynamicCalendarModel calendarModel, DynamicDate? dCurrent = null)
        {
            xLog.Debug("GenerateWidgetTitle: Start: " + wSize.X + "x" + wSize.Y);

            int iWidgetId = cfg.WidgetId;

            string cDay = DateTime.Now.ToString("dddd");
            string cMonth = DateTime.Now.ToString("MMMM");

            int iHeaderHeight = 42;

            rv.SetTextColor(Resource.Id.empty_view, cfg.ColorTitleText.ToAndroid());
            if (calendarModel == null)
            {
                rv.SetTextViewText(Resource.Id.widget_title_day, "error in");
                rv.SetTextViewText(Resource.Id.widget_title_dayname, "calendar model");
                rv.SetTextColor(Resource.Id.widget_title_day, cfg.ColorErrorText.ToAndroid());
                rv.SetTextColor(Resource.Id.widget_title_dayname, cfg.ColorErrorText.ToAndroid());
            }
            else
            {
                var date = dCurrent.HasValue ? dCurrent.Value : calendarModel.GetDateFromUtcDate(DateTime.Now);
                cDay = date.WeekDayNameFull;
                cMonth = date.MonthNameFull;

                rv.SetTextViewText(Resource.Id.widget_title_day, date.DayNumber.ToString());
                rv.SetTextColor(Resource.Id.widget_title_day, cfg.ColorTitleText.ToAndroid());
                rv.SetTextViewText(Resource.Id.widget_title_dayname, cDay
#if DEBUG
                    //+ " " + DateTime.Now.ToString("HH:mm:ss.fff")
#endif
                    );
                rv.SetTextColor(Resource.Id.widget_title_dayname, cfg.ColorTitleText.ToAndroid());
                rv.SetTextViewText(Resource.Id.widget_title_month, cMonth);
                rv.SetTextColor(Resource.Id.widget_title_month, cfg.ColorTitleText.ToAndroid());
            }

            //Title Click
            rv.SetOnClickPendingIntent(Resource.Id.widget, MainWidgetBase.GetClickActionPendingIntent(context, cfg.ClickAction, iWidgetId, "me.ichrono.droid/me.ichrono.droid.Widgets.Calendar.CalendarWidgetConfigActivity"));

            //Config Click
            rv.SetOnClickPendingIntent(Resource.Id.btn_config, MainWidgetBase.GetClickActionPendingIntent(context, new ClickAction(ClickActionType.OpenSettings), iWidgetId, "me.ichrono.droid/me.ichrono.droid.Widgets.Calendar.CalendarWidgetConfigActivity"));

            //Refresh Click
            Intent refreshIntent = new Intent(context.ApplicationContext, typeof(CalendarWidget));
            refreshIntent.SetAction(MainWidgetBase.ActionManualRefresh);
            refreshIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, iWidgetId);
            PendingIntent refreshPendingIntent = PendingIntent.GetBroadcast(context, iWidgetId, refreshIntent, PendingIntentFlags.UpdateCurrent);
            rv.SetOnClickPendingIntent(Resource.Id.btn_refresh, refreshPendingIntent);

            //CreateEvent Click
            PendingIntent createEventPendingIntent = MainWidgetBase.GetClickActionPendingIntent(context, new ClickAction(ClickActionType.CreateEvent), iWidgetId, null);

            rv.SetOnClickPendingIntent(Resource.Id.btn_add_event, createEventPendingIntent);
            rv.SetOnClickPendingIntent(Resource.Id.empty_view, createEventPendingIntent);

            if (wSize.X < 200)
            {
                rv.SetViewVisibility(Resource.Id.btn_refresh, ViewStates.Gone);
                if (cMonth.Length > 4 || cDay.Length > 7)
                    rv.SetViewVisibility(Resource.Id.btn_config, ViewStates.Gone);
            }
            else
            {
                rv.SetViewVisibility(Resource.Id.btn_refresh, ViewStates.Visible);
                rv.SetViewVisibility(Resource.Id.btn_config, ViewStates.Visible);
            }

            rv.SetViewVisibility(Resource.Id.time_switcher, ViewStates.Gone);
            rv.SetViewVisibility(Resource.Id.layout_buttons, ViewStates.Visible);

            if (wSize.Y <= 240 || wSize.X < 120)
            {
                rv.SetViewVisibility(Resource.Id.widget_title, ViewStates.Gone);
                rv.SetViewVisibility(Resource.Id.header_line, ViewStates.Gone);
                rv.SetViewVisibility(Resource.Id.layout_buttonMargin, ViewStates.Gone);
                iHeaderHeight = 0;
                if (cfg is WidgetCfg_CalendarMonthView)
                    rv.SetViewVisibility(Resource.Id.layout_buttons, ViewStates.Gone);
            }
            else
            {
                rv.SetViewVisibility(Resource.Id.widget_title, ViewStates.Visible);
                rv.SetViewVisibility(Resource.Id.header_line, ViewStates.Visible);
                rv.SetViewVisibility(Resource.Id.layout_buttonMargin, ViewStates.Visible);

                if (wSize.X > 300 && cfg is WidgetCfg_CalendarTimetable)
                {
                    rv.SetViewVisibility(Resource.Id.time_switcher, ViewStates.Visible);

                    rv.SetImageViewBitmap(Resource.Id.time_switcher, Tools.GetTimeTypeIcon(context, cfg.CurrentTimeType, LocationTimeHolder.LocalInstance, 24, cfg.ColorTitleButtons.HexString));

                    Intent changeTypeIntent = new Intent(context.ApplicationContext, typeof(CalendarWidget));
                    changeTypeIntent.SetAction(MainWidgetBase.ActionChangeTimeType);
                    changeTypeIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, iWidgetId);
                    changeTypeIntent.PutExtra(MainWidgetBase.ExtraTimeType, (int)MainWidgetBase.GetOtherTimeType(cfg.CurrentTimeType, cfg.WidgetTimeType));
                    PendingIntent changeTypePendingIntent = PendingIntent.GetBroadcast(context, iWidgetId, changeTypeIntent, PendingIntentFlags.UpdateCurrent);
                    rv.SetOnClickPendingIntent(Resource.Id.time_switcher, changeTypePendingIntent);
                }
            }

            if (wSize.X < 150)
                rv.SetViewVisibility(Resource.Id.layout_buttons, ViewStates.Gone);

            rv.SetImageViewBitmap(Resource.Id.btn_config, DrawableHelper.GetIconBitmap(context, "icons8_services", 30, cfg.ColorTitleButtons));
            rv.SetImageViewBitmap(Resource.Id.btn_refresh, DrawableHelper.GetIconBitmap(context, "icons8_refresh", 30, cfg.ColorTitleButtons));
            rv.SetImageViewBitmap(Resource.Id.btn_add_event, DrawableHelper.GetIconBitmap(context, "icons8_add", 40, cfg.ColorTitleButtons));

            if (!cfg.ShowButtonConfig)
                rv.SetViewVisibility(Resource.Id.btn_config, ViewStates.Gone);
            if (!cfg.ShowButtonRefresh)
                rv.SetViewVisibility(Resource.Id.btn_refresh, ViewStates.Gone);
            if (cfg.ShowButtonAdd)
                rv.SetViewVisibility(Resource.Id.btn_add_event, ViewStates.Visible);
            else
                rv.SetViewVisibility(Resource.Id.btn_add_event, ViewStates.Gone);

            int iXR = 8;
            var max = MainWidgetBase.GetMaxXY(wSize.X * sys.DisplayDensity, wSize.Y * sys.DisplayDensity, sys.DisplayShortSite);

            if (cfg is WidgetCfg_CalendarCircleWave)
                rv.SetImageViewBitmap(Resource.Id.header_line, null);
            else
            {
                //Header Line
                GradientDrawable line = new GradientDrawable();
                line.SetShape(ShapeType.Rectangle);
                line.SetCornerRadii(new float[] { iXR, iXR, iXR, iXR, iXR, iXR, iXR, iXR });
                line.SetColor(cfg.ColorTitleText.ToAndroid());
                rv.SetImageViewBitmap(Resource.Id.header_line, MainWidgetBase.GetDrawableBmp(line, max.x, 1.5));
            }
            int iCR = cfg.CornerRadius;
            var topRoundet = new float[] { iCR, iCR, iCR, iCR, 0, 0, 0, 0 };
            var allRoundet = new float[] { iCR, iCR, iCR, iCR, iCR, iCR, iCR, iCR };

            rv.SetViewVisibility(Resource.Id.background_image, ViewStates.Gone);
            rv.SetViewVisibility(Resource.Id.background_image_header, ViewStates.Gone);
            if (cfg.ColorBackground.ToAndroid() != Color.Transparent)
            {
                GradientDrawable back = new GradientDrawable();
                back.SetShape(ShapeType.Rectangle);
                back.SetColor(cfg.ColorBackground.ToAndroid());
                if (cfg is WidgetCfg_CalendarMonthView)
                    back.SetCornerRadii(topRoundet);
                else
                    back.SetCornerRadii(allRoundet);
                if (cfg is WidgetCfg_CalendarMonthView)
                {
                    if (iHeaderHeight > 0)
                    {
                        rv.SetImageViewBitmap(Resource.Id.background_image_header, MainWidgetBase.GetDrawableBmp(back, wSize.X, iHeaderHeight));
                        rv.SetViewVisibility(Resource.Id.background_image_header, ViewStates.Visible);
                        rv.SetViewVisibility(Resource.Id.background_image, ViewStates.Gone);
                    }
                }
                else
                {
                    rv.SetImageViewBitmap(Resource.Id.background_image, MainWidgetBase.GetDrawableBmp(back, wSize.X, wSize.X));
                    rv.SetViewVisibility(Resource.Id.background_image, ViewStates.Visible);
                    rv.SetViewVisibility(Resource.Id.background_image_header, ViewStates.Gone);
                }
            }
            xLog.Debug("GenerateWidgetTitle: done");

            return (true, iHeaderHeight);
        }

        public static void GenerateWidgetMonthView(Context context, RemoteViews rv, WidgetCfg_CalendarMonthView cfg, Point wSize, int iHeaderHeight, DynamicCalendarModel calendarModel, DynamicDate? dXCurrent = null, EventCollection widgetEvents = null)
        {
            xLog.Debug("GenerateWidgetMonthView: start " + wSize.X + "x" + wSize.Y);

            myEvents.Clear();
            myEvents.timeType = cfg.CurrentTimeType;

            int iWeekHeaderHeight = 20;

            string cCalendarFilter = "";
            if (!cfg.ShowAllCalendars)
            {
                cCalendarFilter = "|";
                foreach (string c in cfg.ShowCalendars)
                    cCalendarFilter += c + "|";
            }
            myEvents.CalendarFilter = cCalendarFilter;

            DynamicDate dToday = calendarModel.GetDateFromUtcDate(DateTime.Now);
            DynamicDate dCurrent = dXCurrent.HasValue ? dXCurrent.Value : calendarModel.GetDateFromUtcDate(DateTime.Now);
            int iYear = dCurrent.Year;
            int iMonth = dCurrent.Month;

            var dFirst = dCurrent.BoM;
            var dLast = dCurrent.EoM;
            int iFirstWeekDay = dFirst.DayOfWeek;

            //die Wochentage vor dem ersten Monatstag
            int iSpaces = iFirstWeekDay - calendarModel.FirstDayOfWeek;
            if (iSpaces < 0)
                iSpaces += calendarModel.WeekLength;

            dFirst = dFirst.AddDays(iSpaces * -1);

            int iLastWeekDay = dLast.DayOfWeek;
            int iEndDiff = calendarModel.WeekLength - 1 - iLastWeekDay + calendarModel.FirstDayOfWeek;
            if (iEndDiff < 0)
                iEndDiff += calendarModel.WeekLength;
            if (iEndDiff >= calendarModel.WeekLength)
                iEndDiff = 0;
            dLast = dLast.AddDays(iEndDiff);

            if (widgetEvents == null)
            {
                xLog.Debug("start load events");
                myEvents.DoLoadCalendarEventsGrouped(dFirst.UtcDate, dLast.UtcDate.AddDays(1)).Wait();
                widgetEvents = myEvents;
                xLog.Debug("Loadet events: " + widgetEvents.AllEvents.Count);
            }

            var myDates = new List<DynamicDate>();
            myDates.Clear();
            for (DynamicDate dDate = dFirst; dDate <= dLast; dDate = dDate.AddDays(1))
            {
                myDates.Add(dDate);
            }

            int iSeparator = 2;
            int iColoumnCount = calendarModel.WeekLength;
            int iRowCount = myDates.Count / iColoumnCount;
            double nItemWidth = ((wSize.X + iSeparator) / iColoumnCount) - iSeparator;
            double nItemHeigth = ((wSize.Y - iHeaderHeight - iWeekHeaderHeight + iSeparator) / iRowCount) - iSeparator;

#if DEBUG
            //rv.SetTextViewText(Resource.Id.widget_title_month, nItemWidth.ToString() + "x" + nItemHeigth.ToString());
#endif
            int iEventIconsLayout = Resource.Id.event_icons;

            int iShowEvents = 0;
            if (nItemHeigth >= 100)
                iShowEvents = 6;
            else if (nItemHeigth >= 84)
                iShowEvents = 5;
            else if (nItemHeigth >= 68)
                iShowEvents = 4;
            else if (nItemHeigth >= 52)
                iShowEvents = 3;
            else if (nItemHeigth >= 46)
                iShowEvents = 2;
            else if (nItemHeigth >= 30)
                iShowEvents = 1;

            iShowEvents = Math.Min(6, (int)((nItemHeigth - 18) / 14));

            if (nItemWidth < 30)
                iShowEvents = 0;
            bool bShowEventsIcon = iShowEvents == 0;
            if (bShowEventsIcon)
            {
                if (nItemHeigth > nItemWidth)
                {
                    iEventIconsLayout = Resource.Id.event_icons_vert;
                    iShowEvents = (int)((nItemHeigth - 20) / 16);
                }
                else
                {
                    iShowEvents = (int)((nItemWidth - 20) / 11);
                    if (nItemHeigth < 30)
                    {
                        if (cfg.DayNumberStyle == DayNumberStyle.CalendarModell)
                            iEventIconsLayout = Resource.Id.event_icons_right;
                        else
                            iShowEvents--;
                    }
                }
            }

            xLog.Debug("GenerateWidgetMonthView: ItemSize: " + nItemWidth + "x" + nItemHeigth + ", ShowEvents:" + iShowEvents + (bShowEventsIcon ? " asIcon" : " asText"));

            bool bShowVertLines = nItemWidth > 20;
            bool bShowHorzLines = nItemHeigth > 25;

            rv.RemoveAllViews(Resource.Id.header_layout);
            rv.RemoveAllViews(Resource.Id.list_layout);

            rv.SetViewVisibility(Resource.Id.empty_view, ViewStates.Gone);

            int iCR = cfg.CornerRadius;

            //Wochentage
            for (int iX = 0; iX < calendarModel.WeekLength; iX++)
            {
                RemoteViews rvHead = new RemoteViews(context.PackageName, Resource.Layout.widget_calendar_monthview_header_item);

                int iWeekDay = iX + calendarModel.FirstDayOfWeek;
                var wd = calendarModel.GetWeekDay(iWeekDay);
                if (wd == null)
                    throw new Exception("no week day: " + iWeekDay);

                Color clText = cfg.ColorDayText.ToAndroid();
                Color clBack = cfg.ColorDayBackground.ToAndroid();

                if (wd.HasSpecialTextColor)
                    clText = wd.SpecialTextColor.ToAndroid();
                if (wd.HasSpecialBackgroundColor)
                    clBack = wd.SpecialBackgroundColor.ToAndroid();

                if (nItemWidth > 75)
                    rvHead.SetTextViewText(Resource.Id.text, wd.FullName);
                else if (nItemWidth < 25)
                    rvHead.SetTextViewText(Resource.Id.text, wd.ShortName.Substring(0, 1));
                else
                    rvHead.SetTextViewText(Resource.Id.text, wd.ShortName);
                rvHead.SetTextColor(Resource.Id.text, clText);

                //Background
                if (iHeaderHeight == 0 && (iX == 0 || iX + 1 == iColoumnCount))
                {
                    var back = new GradientDrawable();
                    back.SetShape(ShapeType.Rectangle);
                    if (iX == 0)
                        back.SetCornerRadii(new float[] { iCR, iCR, 0, 0, 0, 0, 0, 0 });
                    else
                        back.SetCornerRadii(new float[] { 0, 0, iCR, iCR, 0, 0, 0, 0 });
                    back.SetColor(clBack);
                    rvHead.SetImageViewBitmap(Resource.Id.background_image, MainWidgetBase.GetDrawableBmp(back, nItemWidth, 20));
                }
                else
                    rvHead.SetInt(Resource.Id.header_layout, "setBackgroundColor", clBack);

                if (iX > 0 && bShowVertLines)
                {
                    RemoteViews rvLine = new RemoteViews(context.PackageName, Resource.Layout.line_vert);
                    rvLine.SetInt(Resource.Id.line_vert, "setBackgroundColor", clBack);
                    //rvLine.SetInt(Resource.Id.line_vert, "setBackgroundColor", cfg.ColorGridLines.ToAndroid());
                    rv.AddView(Resource.Id.header_layout, rvLine);
                }

                rv.AddView(Resource.Id.header_layout, rvHead);
            }

            int iDay = 0;
            for (int iRow = 0; iRow < iRowCount; iRow++)
            {
                if (iRow > 0 && bShowHorzLines)
                {
                    RemoteViews rvLine = new RemoteViews(context.PackageName, Resource.Layout.line_horz);
                    rvLine.SetInt(Resource.Id.line_horz, "setBackgroundColor", cfg.ColorGridLines.ToAndroid());
                    rv.AddView(Resource.Id.list_layout, rvLine);
                }

                RemoteViews rvRow = new RemoteViews(context.PackageName, Resource.Layout.widget_calendar_monthview_week_row);
                for (int iCol = 0; iCol < iColoumnCount; iCol++)
                {
                    if (iCol > 0 && bShowVertLines)
                    {
                        RemoteViews rvLine = new RemoteViews(context.PackageName, Resource.Layout.line_vert);
                        rvLine.SetInt(Resource.Id.line_vert, "setBackgroundColor", cfg.ColorGridLines.ToAndroid());
                        rvRow.AddView(Resource.Id.row_layout, rvLine);
                    }

                    RemoteViews rvItem = new RemoteViews(context.PackageName, Resource.Layout.widget_calendar_monthview_day_item);
                    //xLog.Verbose("AddItem: " + iRow + " * " + iCol + " => " + iDay);
                    try
                    {
                        DynamicDate dDate = myDates[iDay];

                        string cDayName = dDate.DayNumber.ToString();
                        xColor clEvent = cfg.ColorEventText;

                        var clrs = dDate.GetDayColors(cfg.ColorDayText, cfg.ColorDayBackground);

                        if (dDate.Month != dCurrent.Month)
                            clrs.BackgroundColor = clrs.BackgroundColor.LuminosityDiff(-20);

                        rvItem.SetTextViewText(Resource.Id.item_day_nr, "");
                        rvItem.SetTextViewText(Resource.Id.item_day_nr_topcenter, "");
                        rvItem.SetTextViewText(Resource.Id.item_day_nr_center, "");

                        string cDayNr = "";
                        string cDayInfo = "";
                        switch (cfg.DayNumberStyle)
                        {
                            case DayNumberStyle.CalendarModell:
                                cDayNr = cDayName;
                                break;
                            case DayNumberStyle.Gregorian:
                                cDayNr = dDate.UtcDate.ToString("d.M.");
                                break;
                            case DayNumberStyle.CalendarModellAndGregorian:
                                cDayNr = cDayName;
                                cDayInfo = dDate.UtcDate.ToString("d.M");
                                break;
                            case DayNumberStyle.GregorianAndCalendarModell:
                                cDayNr = dDate.UtcDate.ToString("d.M.");
                                cDayInfo = cDayName;
                                break;
                        }

                        SpannableStringBuilder sp = new SpannableStringBuilder(cDayNr);
                        if (dToday == dDate)
                        {
                            sp.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, cDayNr.Length, 0);
                            if (cfg.ColorTodayBackground != xColor.Transparent)
                                clrs.BackgroundColor = cfg.ColorTodayBackground;
                            if (cfg.ColorTodayText != xColor.Transparent)
                                clrs.TextColor = cfg.ColorTodayText;
                        }

                        int iDayLeftSpace = cDayNr.Length == 1 ? 4 : 0;
                        int iDayNrTextView = Resource.Id.item_day_nr;
                        if (nItemHeigth < 25)
                        {
                            if (nItemWidth < 50)
                            {
                                cDayInfo = "";
                                iDayLeftSpace = 0;
                            }
                            if (nItemWidth < 20)
                            {
                                iDayNrTextView = Resource.Id.item_day_nr_center;
                                iShowEvents = 0;
                            }
                            rvItem.SetTextViewTextSize(iDayNrTextView, (int)ComplexUnitType.Dip, (float)(nItemHeigth * 0.9));
                            rvItem.SetTextViewTextSize(Resource.Id.item_day_info, (int)ComplexUnitType.Dip, (float)(nItemHeigth * 0.8));
                            rvItem.SetViewPadding(iDayNrTextView, iDayLeftSpace, -2, 0, 0);
                            rvItem.SetViewPadding(Resource.Id.item_day_info, 0, -2, 0, 0);
                        }
                        else if (nItemWidth < 20)
                        {
                            iDayNrTextView = Resource.Id.item_day_nr_topcenter;
                            rvItem.SetTextViewTextSize(iDayNrTextView, (int)ComplexUnitType.Dip, (float)(nItemWidth * 0.9));
                            rvItem.SetViewPadding(iDayNrTextView, 0, -1, 0, 0);
                            cDayInfo = "";
                        }

                        rvItem.SetTextViewText(iDayNrTextView, sp);
                        rvItem.SetTextViewText(Resource.Id.item_day_info, cDayInfo);

                        rvItem.SetTextColor(iDayNrTextView, clrs.TextColor.ToAndroid());
                        rvItem.SetTextColor(Resource.Id.item_day_info, clrs.TextColor.ToAndroid());

                        //Background
                        if (((iRow + 1 == iRowCount) || (iRow == 0 && iHeaderHeight == 0 && iWeekHeaderHeight == 0))
                            && (iCol == 0 || iCol + 1 == iColoumnCount))
                        {
                            var back = new GradientDrawable();
                            back.SetShape(ShapeType.Rectangle);

                            if (iRow == 0)
                            {
                                if (iCol == 0)
                                    back.SetCornerRadii(new float[] { 0, 0, iCR, iCR, 0, 0, 0, 0 });
                                else
                                    back.SetCornerRadii(new float[] { iCR, iCR, 0, 0, 0, 0, 0, 0 });
                            }
                            else
                            {
                                if (iCol == 0)
                                    back.SetCornerRadii(new float[] { 0, 0, 0, 0, 0, 0, iCR, iCR });
                                else
                                    back.SetCornerRadii(new float[] { 0, 0, 0, 0, iCR, iCR, 0, 0 });
                            }
                            back.SetColor(clrs.BackgroundColor.ToAndroid());
                            rvItem.SetImageViewBitmap(Resource.Id.background_image, MainWidgetBase.GetDrawableBmp(back, nItemWidth, nItemHeigth));
                        }
                        else
                            rvItem.SetInt(Resource.Id.item_layout, "setBackgroundColor", clrs.BackgroundColor.ToAndroid());

                        //events..
                        rvItem.SetTextViewText(Resource.Id.item_event1, "");
                        rvItem.SetTextViewText(Resource.Id.item_event2, "");
                        rvItem.SetTextViewText(Resource.Id.item_event3, "");
                        rvItem.SetTextViewText(Resource.Id.item_event4, "");
                        rvItem.SetTextViewText(Resource.Id.item_event5, "");
                        rvItem.SetTextViewText(Resource.Id.item_event6, "");

                        if (widgetEvents.ContainsKey(dDate.UtcDate))
                        {
                            if (bShowEventsIcon && nItemHeigth < 30)
                            {
                                rvItem.SetViewPadding(iEventIconsLayout, 0, 0, 0, 0);
                            }

                            var events = widgetEvents[dDate.UtcDate] as SortedDictionary<DateTime, CalendarEvent>.ValueCollection;
                            if (events != null && events.Count > 0 && iShowEvents > 0)
                            {
                                int iEvnt = 0;
                                int iEvntLabel = Math.Min(events.Count, iShowEvents);
                                foreach (var evnt in events)
                                {
                                    iEvnt++;
                                    if (bShowEventsIcon)
                                    {
                                        GradientDrawable shape = new GradientDrawable();
                                        shape.SetShape(ShapeType.Oval);
                                        shape.SetColor(evnt.DisplayColor.ToAndroid());

                                        var bmpEvntIcon = MainWidgetBase.GetDrawableBmp(shape, 10, 10);
                                        if (iEvnt >= iShowEvents && events.Count > iShowEvents)
                                            DrawIconPlus(bmpEvntIcon);

                                        RemoteViews rvIcon = new RemoteViews(context.PackageName, Resource.Layout.imgview_wrap);
                                        rvIcon.SetImageViewBitmap(Resource.Id.image_view, bmpEvntIcon);
                                        if (iEvnt > 1)
                                        {
                                            if (iEventIconsLayout == Resource.Id.event_icons_vert)
                                                rvIcon.SetViewPadding(Resource.Id.image_view, 0, 1, 0, 0);
                                            else
                                                rvIcon.SetViewPadding(Resource.Id.image_view, 1, 0, 0, 0);
                                        }
                                        rvItem.AddView(iEventIconsLayout, rvIcon);

                                        if (iEvnt >= iShowEvents)
                                            break;
                                    }
                                    else
                                    {
                                        if (cfg.ShowEventColor)
                                            clEvent = evnt.DisplayColor;
                                        var diffL = clEvent.Luminosity - clrs.BackgroundColor.Luminosity;
                                        var diffH = clEvent.Hue - clrs.BackgroundColor.Hue;
                                        //if ((diffL > -.3 && diffL < .3) || (diffH > -.3 && diffH < .3))
                                        //  clEvent = clBack.InvertLuminosityDiff();

                                        try
                                        {
                                            int iViewId = (int)typeof(Resource.Id).GetField("item_event" + iEvntLabel--).GetValue(null);

                                            rvItem.SetTextViewText(iViewId, evnt.Title);
                                            rvItem.SetTextColor(iViewId, clEvent.ToAndroid());

                                            if (iEvnt >= iShowEvents || iEvntLabel < 1)
                                            {
                                                if (events.Count > iEvnt)
                                                {
                                                    rvItem.SetTextViewText(Resource.Id.item_event_info, "+" + (events.Count - iEvnt));
                                                    rvItem.SetTextColor(Resource.Id.item_event_info, clrs.TextColor.ToAndroid());
                                                }
                                                break;
                                            }
                                        }
                                        catch
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        xLog.Error(ex, "OnGetItemLayout");
                        rvItem = new RemoteViews(context.PackageName, Resource.Layout.item_error);
                        rvItem.SetTextViewText(Resource.Id.error_text, ex.Message);

                        rvItem.SetTextColor(Resource.Id.error_text, cfg.ColorErrorText.ToAndroid());
                        rvItem.SetTextViewText(Resource.Id.error_text, ex.GetType().ToString());
                    }

                    rvRow.AddView(Resource.Id.row_layout, rvItem);
                    iDay++;
                }
                rv.AddView(Resource.Id.list_layout, rvRow);
                xLog.Debug("GenerateWidgetMonthView: done");
            }

            void DrawIconPlus(Bitmap bitmap)
            {
                Canvas canvas = new Canvas(bitmap);

                Paint paint = new Paint(PaintFlags.AntiAlias);
                paint.TextAlign = Paint.Align.Center;
                paint.Color = Color.White;
                paint.TextSize = 14;
                paint.FakeBoldText = true;

                canvas.DrawText("+", 5, 10, paint);
            }
        }

        public static void GenerateCircleWaveView(Context context, AppWidgetManager appWidgetManager, RemoteViews rv, WidgetCfg_CalendarCircleWave cfg, Point wSize, int iHeaderHeight, DynamicCalendarModel calendarModel, DynamicDate? dXCurrent = null, EventCollection widgetEvents = null)
        {
            xLog.Debug("GenerateCircleWaveView: Start: " + wSize.X + "x" + wSize.Y);
            DateTime swStart = DateTime.Now;

            bool bAnimate = ResetData;
            ResetData = false;

            int iWidgetId = cfg.WidgetId;
            rv.SetViewVisibility(Resource.Id.circle_image, ViewStates.Visible);

            DynamicDate dToday = calendarModel.GetDateFromUtcDate(DateTime.Now);
            DynamicDate dFirst = calendarModel.GetDateFromUtcDate(DateTime.Now);

            switch (cfg.FirstDayType)
            {
                case FirstDayType.Today:
                    break;

                case FirstDayType.TodayLastWeek:
                    dFirst = dFirst.Add(TimeUnit.Week, -1);
                    break;

                case FirstDayType.WeekStart:
                case FirstDayType.LastWeekStart:
                    int iFirstWeekDay = dFirst.DayOfWeek;
                    int iSpaces = iFirstWeekDay - calendarModel.FirstDayOfWeek;
                    if (iSpaces < 0)
                        iSpaces += calendarModel.WeekLength;
                    dFirst = dFirst.AddDays(iSpaces * -1);
                    if (cfg.FirstDayType == FirstDayType.LastWeekStart)
                        dFirst = dFirst.Add(TimeUnit.Week, -1);
                    break;

                case FirstDayType.MonthStart:
                    dFirst = new DynamicDate(dFirst.Model, dFirst.Year, dFirst.Month, 0);
                    break;

                case FirstDayType.YearStart:
                    dFirst = new DynamicDate(dFirst.Model, dFirst.Year, 0, 0);
                    break;
            }
            if (cfg.FirstDayOffset != 0)
                dFirst = dFirst.AddDays(cfg.FirstDayOffset);

            int iYear = dFirst.Year;
            int iMonth = dFirst.Month;
            int iDayCount = 28;

            switch (cfg.TimeUnit)
            {
                case TimeUnit.Day:
                    iDayCount = cfg.TimeUnitCount;
                    break;
                case TimeUnit.Week:
                    iDayCount = cfg.TimeUnitCount * calendarModel.WeekLength;
                    break;
                case TimeUnit.Month:
                    iDayCount = calendarModel.GetDaysOfMonth(iYear, iMonth);
                    for (int i = 1; i < (int)cfg.TimeUnitCount; i++)
                        iDayCount += calendarModel.GetDaysOfMonth(iYear, iMonth + i);
                    break;
                case TimeUnit.Year:
                    iDayCount = calendarModel.GetDaysOfYear(iYear);
                    for (int i = 1; i < (int)cfg.TimeUnitCount; i++)
                        iDayCount += calendarModel.GetDaysOfYear(iYear + i);
                    break;
            }

            if (iDayCount < 1)
                iDayCount = calendarModel.GetDaysOfMonth(iYear, iMonth);

            DynamicDate dLast = dFirst.AddDays(iDayCount - 1);
            List<DynamicDate> dDayS = new List<DynamicDate>();
            for (DynamicDate dDate = dFirst; dDate <= dLast; dDate = dDate.AddDays(1))
                dDayS.Add(dDate);

            if (cfg.RotateBaseDay == RotateBaseDay.Today)
            {
                while (dDayS[0] < dToday)
                {
                    dDayS.Add(dDayS[0]);
                    dDayS.RemoveAt(0);
                }
            }

            TimeSpan tsInit = DateTime.Now - swStart;
            swStart = DateTime.Now;

            //the Color-Circle
            int iWidgetShortSide = Math.Min(wSize.X, wSize.Y);
            var max = MainWidgetBase.GetMaxXY(wSize.X * sys.DisplayDensity, wSize.Y * sys.DisplayDensity, sys.DisplayShortSite);
            int iImgCX = max.x / 2;
            int iImgCY = max.y / 2;

            int iImgShortSize = Math.Min(max.x, max.y);

            float nOuterCircle = (iImgShortSize * .75F + Math.Min(iImgShortSize * .2F, iImgShortSize * .2F * iDayCount / 366)) / 2;
            float nColorWheelThickness = nOuterCircle * 2 / 3;
            float nColorWheelRadius = nOuterCircle * 2 / 3;

            float nCircleRadius = Math.Min(iImgCX, iImgCY);

            string cDebugInfo = "widget: " + wSize.X + "x" + wSize.Y + "\nimg: " + nCircleRadius + "\nmax: " + max.x + "x" + max.y + " %" + max.n;

            Bitmap bmp = Bitmap.CreateBitmap(max.x, max.y, Bitmap.Config.Argb8888);
            Canvas canvas = new Canvas(bmp);
            //canvas.DrawARGB(200, 100, 100, 100);

#if DEBUG
            var pOut = new Paint();
            pOut.SetStyle(Paint.Style.Stroke);
            pOut.StrokeWidth = 3;
            //canvas.DrawCircle(iImgCX, iImgCY, nOuterCircle, pOut);
#endif

            int iBaseRotate = -180;
            switch (cfg.RotatePosition)
            {
                case RotatePosition.Top:
                    iBaseRotate += 90;
                    break;
                case RotatePosition.Right:
                    iBaseRotate += 180;
                    break;
                case RotatePosition.Bottom:
                    iBaseRotate += 270;
                    break;
            }

            float nAngleStart = -(360.0F / iDayCount * .5F) + iBaseRotate;
            float nAnglePart = 360.0F / iDayCount;
            RectF box = new RectF(iImgCX - nOuterCircle, iImgCY - nOuterCircle, iImgCX + nOuterCircle, iImgCY + nOuterCircle);

            //der Farbkuchen

            //DateGradient dg = new DateGradient();
            if (cfg.ColorDayBackground.A == 0)
            {
                //dg.GradientS.Add(new DynamicGradient(TimeUnit.Year, Abstractions.DynamicCalendar.GradientType.CustomColors) { CustomColors = new Xamarin.Forms.Color[] { Xamarin.Forms.Color.FromHex("#FF8F7166"), Xamarin.Forms.Color.FromHex("#FFDDcb30") } });
                //dg.GradientS.Add(new DynamicGradient(TimeUnit.Year, Abstractions.DynamicCalendar.GradientType.Rainbow));
                //dg.GradientS.Add(new DynamicGradient(TimeUnit.Year, Abstractions.DynamicCalendar.GradientType.Rainbow));
                //dg.WeekGradient = new DynamicGradient(TimeUnit.Week, Abstractions.DynamicCalendar.GradientType.Rainbow);
                //dg.MonthGradient = new DynamicGradient(TimeUnit.Month, Abstractions.DynamicCalendar.GradientType.CustomColors) { CustomColors = new Xamarin.Forms.Color[] { Xamarin.Forms.Color.FromHex("#FF8F7166"), Xamarin.Forms.Color.FromHex("#FFDDcb30") } };
                //dg.YearGradient = new DynamicGradient(TimeUnit.Year, Abstractions.DynamicCalendar.GradientType.Rainbow);
            }
            else
            {
                var clr = cfg.ColorDayBackground;
                //dg.GradientS.Add(new DynamicGradient(TimeUnit.Month, Abstractions.DynamicCalendar.GradientType.CustomColors)
                //{ CustomColors = new Xamarin.Forms.Color[] { Xamarin.Forms.Color.FromHsla(clr.Hue, clr.Saturation, clr.Luminosity + .1), Xamarin.Forms.Color.FromHsla(clr.Hue, clr.Saturation, clr.Luminosity - .1) } });
            }

            var paint = new Paint();
            paint.SetStyle(Paint.Style.Fill);
            paint.AntiAlias = true;
            int iClr = 0;
            foreach (DynamicDate dDate in dDayS)
            {
                var x = dDate.GetDayColors(new xColor(), cfg.DayBackgroundGradient.GetColor(dDate)).BackgroundColor;
                Color clr = x.ToAndroid();

                //if (dDate < dToday)
                //  clr = new Color(clr.R, clr.G, clr.B, (byte)204);

                paint.Color = clr;
                canvas.DrawArc(box, nAngleStart, nAnglePart, true, paint);
                nAngleStart += nAnglePart;

                iClr++;
            }

            //the Center
            var pEarse = new Paint();
            //pEarse.Color = Color.Transparent;
            //pEarse.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.Clear));
            pEarse.SetStyle(Paint.Style.Fill);
            for (float nRad = .43F; nRad >= .27F; nRad -= .03F)
            {
                pEarse.SetShader(new RadialGradient(iImgCX, iImgCY, nOuterCircle * nRad, Color.Black, new Color(0, 0, 0, 10), Shader.TileMode.Mirror));
                canvas.DrawCircle(iImgCX, iImgCY, nOuterCircle * nRad, pEarse);
            }

            paint.Color = Color.Black;
            //canvas.DrawCircle(iImgCX, iImgCY, nOuterCircle / 3, paint);

            float hourangle = 360.0F / 24 * LocationTimeHolder.LocalInstance.GetTime(cfg.CurrentTimeType).Hour + 90;
            float hourX = (float)(iImgCX + nOuterCircle / 6 * Math.Cos((hourangle) * Math.PI / 180));
            float hourY = (float)(iImgCY + nOuterCircle / 6 * Math.Sin((hourangle) * Math.PI / 180));


            var pCenter = new Paint();
            pCenter.SetStyle(Paint.Style.Fill);
            pCenter.SetShader(new RadialGradient(hourX, hourY, nOuterCircle / 6, new Color(255, 255, 255, 180), Color.Transparent, Shader.TileMode.Mirror));
            canvas.DrawCircle(hourX, hourY, nOuterCircle / 6, pCenter);

            //Heute hervorheben
            if (dDayS.Contains(dToday))
            {
                float len = iImgShortSize * .68F;
                box.Set(iImgCX - len, iImgCY - len, iImgCX + len, iImgCY + len);
                iClr = 0;
                int iNrToday = dDayS.IndexOf(dToday);
                nAngleStart = -(360.0F / iDayCount * .5F) + iBaseRotate;
                nAngleStart += nAnglePart * iNrToday;
                if (nAnglePart < 5)
                {
                    nAngleStart -= (5 - nAnglePart) / 2;
                    nAnglePart = 5;
                }

                Color clr = cfg.ColorTodayBackground.ToAndroid();
                if (clr.A == 0)
                {
                    clr = dToday.GetDayColors(new xColor(), cfg.DayBackgroundGradient.GetColor(dToday)).BackgroundColor.ToAndroid();
                }
                paint.Color = clr;
                canvas.DrawArc(box, nAngleStart, nAnglePart, true, paint);
            }

            TimeSpan tsWeel = DateTime.Now - swStart;
            swStart = DateTime.Now;

            //loading Events
            myEvents.Clear();
            myEvents.timeType = cfg.CurrentTimeType;

            string cCalendarFilter = "";
            if (!cfg.ShowAllCalendars)
            {
                cCalendarFilter = "|";
                foreach (string c in cfg.ShowCalendars)
                    cCalendarFilter += c + "|";
            }
            myEvents.CalendarFilter = cCalendarFilter;

            //draw Info to Cycle #############
            float nTextSize = Math.Min(16F, iWidgetShortSide / iDayCount * 1.6F);
            cDebugInfo += "\ntxt:" + Math.Round(nTextSize, 2);
            float nBubbleSize = Math.Max(nTextSize * sys.DisplayDensity * max.n, sys.DisplayDensity);
            nTextSize = Math.Max(9F, nTextSize);
            cDebugInfo += " => " + Math.Round(nTextSize, 2) + " => " + sys.DisplayDensity;
            nTextSize *= (float)sys.DisplayDensity * max.n;

            float labelRadius = nOuterCircle - nTextSize - nBubbleSize * .5F;

            float nUmfang = (float)(labelRadius * 2 * Math.PI);
            float nLabelCount = nUmfang / nTextSize / 3F;
            float nTextJumpSize = Math.Max(1F, (iDayCount / nLabelCount));
            float nText2JumpSize = (nTextJumpSize * 1.48F);

            cDebugInfo += "\nJmp: " + nTextJumpSize + " : " + nText2JumpSize;

            var pLabel = new Paint();
            pLabel.TextSize = nTextSize;

            var pPoint = new Paint();
            if (cfg.DayNumberStyle != DayNumberStyle.None)
            {
                int iText2 = 0;
                DynamicDate dLastDate = dDayS[0];
                DateTime tLastDate = dLastDate.UtcDate;

                int iToday = dDayS.IndexOf(dToday);
                if (iToday < 0)
                    iToday = 0;
                for (int iDayId = 0; iDayId < iDayCount; iDayId++)
                {
                    int iDay = iDayId + iToday;
                    if (iDay >= iDayCount)
                        iDay -= iDayCount;

                    var dDate = dDayS[iDay];
                    if (dDate.IsEmpty)
                        continue;

                    pLabel.TextAlign = Paint.Align.Center;

                    if (dToday == dDate)
                    {
                        pLabel.Color = cfg.ColorDayText.ToAndroid();
                        pPoint.Color = Color.Khaki;
                        pLabel.FakeBoldText = true;
                    }
                    else
                    {
                        pLabel.Color = cfg.ColorDayText.ToAndroid();
                        pPoint.Color = Color.White;
                        pLabel.FakeBoldText = false;
                    }

                    //Day-Number
                    if (nTextSize > 0 && iDayId % (int)nTextJumpSize == 0 && iDayCount - iDayId >= (int)nTextJumpSize)
                    {
                        string cText = dDate.DayNumber.ToString();
                        string cText2 = "";
                        if (dDate == dToday && iHeaderHeight < 40)
                        {
                            if (cfg.DayNumberStyle == DayNumberStyle.Gregorian || cfg.DayNumberStyle == DayNumberStyle.GregorianAndCalendarModell)
                                cText2 = dDate.UtcDate.ToString("ddd.");
                            else
                                cText2 = dDate.ToString("ddd.");
                        }
                        else if (cfg.DayNumberStyle == DayNumberStyle.CalendarModell)
                        {
                            if (iDay == 0 || dDate.Month != dLastDate.Month)
                            {
                                iText2 = 0;
                                cText2 = dDate.ToString("MMM");
                            }
                        }
                        else if (cfg.DayNumberStyle == DayNumberStyle.Gregorian)
                        {
                            cText = dDate.UtcDate.Day.ToString();
                            if (iDay == 0 || dDate.UtcDate.Month != tLastDate.Month)
                            {
                                iText2 = 0;
                                cText2 = dDate.UtcDate.ToString("MMM");
                            }
                        }
                        else if (cfg.DayNumberStyle == DayNumberStyle.CalendarModellAndGregorian)
                        {
                            cText = dDate.DayNumber.ToString();
                            if (iDay == 0 || dDate.UtcDate.Month != tLastDate.Month)
                            {
                                iText2 = 0;
                                cText2 = dDate.UtcDate.ToString("MMM d");
                            }
                            else if (dDate.UtcDate.Day - 1 < DateTime.DaysInMonth(dDate.UtcDate.Year, dDate.UtcDate.Month))
                                cText2 = dDate.UtcDate.ToString("d.");
                        }
                        else if (cfg.DayNumberStyle == DayNumberStyle.GregorianAndCalendarModell)
                        {
                            cText = dDate.UtcDate.Day.ToString();

                            if (iDay == 0 || dDate.Month != dLastDate.Month)
                            {
                                iText2 = 0;
                                cText2 = dDate.ToString("_mMd");
                            }
                            else if (dDate.DayNumber - 1 < calendarModel.GetDaysOfMonth(dDate.Year, dDate.Month))
                                cText2 = dDate.ToString("d.");

                            if (iDay == 0 || dDate.UtcDate.Month != tLastDate.Month)
                            {
                                iText2 = 0;
                                cText2 = dDate.UtcDate.ToString("MMM");
                            }
                        }

                        float angle = 360.0F / (iDayCount) * iDay + iBaseRotate;
                        float x = (float)(iImgCX + labelRadius * Math.Cos((angle) * Math.PI / 180));
                        float y = (float)(iImgCY + labelRadius * Math.Sin((angle) * Math.PI / 180));

                        //Rect bounds = new Rect();
                        //paint.GetTextBounds(cText, 0, cText.Length, bounds);

                        canvas.DrawCircle(x, y, nTextSize * .7F, pPoint);
                        canvas.DrawText(cText, x, y + nTextSize * .4f, pLabel);

                        float nRotate = 360F / iDayCount * iDay + iBaseRotate;// + 360F / (iDayCount) * .125F;
                        float nArbitrary = labelRadius - nTextSize * 1.2F;
                        pLabel.TextAlign = Paint.Align.Right;

                        if (nRotate < 0)
                            nRotate += 360;
                        if (nRotate > 360)
                            nRotate -= 360;
                        if (nRotate >= 90 && nRotate < 270)
                        {
                            nRotate = 180F + 360F / iDayCount * iDay + iBaseRotate;// - 360F / (iDayCount) * .125F;
                            nArbitrary *= -1;
                            pLabel.TextAlign = Paint.Align.Left;
                        }

                        //if (iText2 % iText2JumpSize == 0 && iWidgetShortSide > 150 && !string.IsNullOrEmpty(cText2))
                        {
                            //cText2 = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
                            //Gregorian Date
                            x = iImgCX;
                            y = iImgCY;
                            canvas.Rotate(nRotate, x, y);
                            canvas.Translate(nArbitrary, 0);
                            pLabel.SetStyle(Paint.Style.Fill);
                            canvas.DrawText(cText2, x, y + nTextSize * .4F, pLabel);
                            //Undo the translations and rotations so that next arc can be drawn normally
                            canvas.Translate(-(nArbitrary), 0);
                            canvas.Rotate(-(nRotate), x, y);
                        }
                        iText2++;
                        dLastDate = dDate;
                        tLastDate = dDate.UtcDate;
                    }
                }
            }

            TimeSpan tsInfo = DateTime.Now - swStart;
            swStart = DateTime.Now;

            if (appWidgetManager != null && bAnimate)
            {
                var xv = rv.Clone();
                xv.SetImageViewBitmap(Resource.Id.circle_image, bmp);
                appWidgetManager.UpdateAppWidget(iWidgetId, xv);
            }

            rv.SetViewVisibility(Resource.Id.empty_view, ViewStates.Gone);

            swStart = DateTime.Now;
            if (widgetEvents == null)
            {
                xLog.Debug("start load events");
                myEvents.DoLoadCalendarEventsGrouped(dFirst.UtcDate, dLast.UtcDate.AddDays(1)).Wait();
                widgetEvents = myEvents;
                xLog.Debug("Loadet events: " + widgetEvents.AllEvents.Count);
            }

            if (bAnimate)
            {
                int iMs = (int)((DateTime.Now - swStart).TotalMilliseconds);
                if (iMs < 500)
                    Task.Delay(500 - iMs).Wait();
            }

            TimeSpan tsEvents = DateTime.Now - swStart;
            swStart = DateTime.Now;

            int iEventDay = 0;
            for (int iDay = 0; iDay < iDayCount; iDay++)
            {
                var dDate = dDayS[iDay];
                if (dDate.IsEmpty)
                    continue;

                //Events
                if (widgetEvents.ContainsKey(dDate.UtcDate))
                {
                    var events = widgetEvents[dDate.UtcDate] as SortedDictionary<DateTime, CalendarEvent>.ValueCollection;
                    if (events != null && events.Count > 0)
                    {
                        float angle = 360.0F / (iDayCount) * iDay + iBaseRotate; // + 360.0 / (iDayCount) * .5;

                        int iEvnt = 0;
                        foreach (var evnt in events)
                        {
                            iEvnt++;

                            float nIconRadius = nOuterCircle + nBubbleSize * 1.1F * (iEvnt);
                            float x = (float)(iImgCX + nIconRadius * Math.Cos((angle) * Math.PI / 180));
                            float y = (float)(iImgCY + nIconRadius * Math.Sin((angle) * Math.PI / 180));

                            pPoint.Color = evnt.DisplayColor.ToAndroid();
                            canvas.DrawCircle(x, y, nBubbleSize * .8F, pPoint);
                        }

                        if (iEventDay % ((int)nTextJumpSize) == 0 && appWidgetManager != null && bAnimate)
                        {
                            var xv = rv.Clone();
                            xv.SetImageViewBitmap(Resource.Id.circle_image, bmp);
                            appWidgetManager.UpdateAppWidget(iWidgetId, xv);
                        }
                        iEventDay++;
                    }
                }
            }
            /* */

            TimeSpan tsBubbles = DateTime.Now - swStart;
            swStart = DateTime.Now;

            cDebugInfo += "\n" + (int)tsInit.TotalMilliseconds + ", " + (int)tsWeel.TotalMilliseconds + ", " + (int)tsInfo.TotalMilliseconds + ", " + (int)tsEvents.TotalMilliseconds + ", " + (int)tsBubbles.TotalMilliseconds;

            rv.SetImageViewBitmap(Resource.Id.circle_image, bmp);
            //rv.SetTextViewText(Resource.Id.debug_text, cDebugInfo);
            xLog.Debug("GenerateCircleWaveView: Done");
        }

        public static void AddDummyListEvents(Context mContext, RemoteViews rv, WidgetCfg_CalendarTimetable cfg, Android.Graphics.Point wSize, DynamicCalendarModel CalendarModel, EventCollection myEventsList)
        {
            var viewsFactory = new CalendarEventListRemoteViewsFactory(mContext, cfg as WidgetCfg_CalendarTimetable, wSize, CalendarModel, myEventsList);
            int iCount = System.Math.Min(10, viewsFactory.Count);
            if (iCount > 0)
            {
                rv.SetViewVisibility(Resource.Id.list_layout, ViewStates.Visible);
                rv.SetViewVisibility(Resource.Id.empty_view, ViewStates.Gone);
                for (int i = 1; i < iCount; i++)
                {
                    rv.AddView(Resource.Id.list_layout, viewsFactory.GetViewAt(i));
                }
            }
        }
    }
}