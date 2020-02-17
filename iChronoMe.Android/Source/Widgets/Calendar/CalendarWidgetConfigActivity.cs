﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V4.App;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Core.Types;
using iChronoMe.Widgets;
using Net.ArcanaStudio.ColorPicker;

namespace iChronoMe.Droid.Widgets.Calendar
{
    [Activity(Label = "CalendarWidgetConfigActivity", Name = "me.ichrono.droid.Widgets.Calendar.CalendarWidgetConfigActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    public class CalendarWidgetConfigActivity : BaseWidgetActivity
    {
        public int appWidgetId = -1;
        DynamicCalendarModel CalendarModel;
        EventCollection myEventsMonth;
        EventCollection myEventsList;
        Drawable wallpaperDrawable;
        AlertDialog pDlg;
        List<WidgetCfg_Calendar> DeletedWidgets = new List<WidgetCfg_Calendar>();
        WidgetConfigHolder holder;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Intent launchIntent = Intent;
            Bundle extras = launchIntent.Extras;

            if (extras != null)
            {
                appWidgetId = extras.GetInt(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
                Intent resultValue = new Intent();
                resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                SetResult(Result.Canceled, resultValue);
            }
            if (appWidgetId < 0)
            {
                Toast.MakeText(this, "Fehlerhafte Parameter!", ToastLength.Long).Show();
                FinishAndRemoveTask();
                return;
            }
        }

        bool bPermissionTryed = false;

        protected override void OnResume()
        {
            base.OnResume();
            if (NeedsStartAssistant())
                ShowStartAssistant();
            else
            {
                if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.WriteCalendar) != Permission.Granted || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
                {
                    if (!bPermissionTryed)
                    {
                        ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ReadCalendar, Manifest.Permission.WriteCalendar, Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation }, 2);
                        bPermissionTryed = true;
                    }
                    else
                    {
                        new AlertDialog.Builder(this)
                            .SetMessage("calendar-permission is required for a calendar-widget!")
                            .SetPositiveButton("accept", (s, e) => { FinishAndRemoveTask(); })
                            .Create().Show();
                    }
                    return;
                }

                holder = new WidgetConfigHolder();
                if (false && holder.WidgetExists<WidgetCfg_Calendar>(appWidgetId))
                {
                    var it = new Intent(this, typeof(CalendarWidgetConfigActivityAdvanced));
                    it.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                    StartActivity(it);
                }
                //else
                {
                    var progressBar = new Android.Widget.ProgressBar(this);
                    progressBar.Indeterminate = true;
                    pDlg = new AlertDialog.Builder(this)
                        .SetCancelable(false)
                        .SetTitle("Daten werden aufbereitet...")
                        .SetView(progressBar)
                        .Create();
                    pDlg.Show();

                    StartWidgetSelection();
                }
            }
        }

        Point wSize = new Point(400, 300);

        public void StartWidgetSelection()
        {
            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.WriteCalendar) != Permission.Granted || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                ShowExitMessage("Die Kalender-Widget's funktionieren nur mit Zugriff auf Kalender und Standort!");
                return;
            }

            Task.Factory.StartNew(() =>
            {
                try
                {
                    Task.Delay(100).Wait();

                    //int iWidth = Math.Min(400, (int)(sys.DisplayShortSiteDp * .9));
                    //wSize = new Point(iWidth, (int)(iWidth * .75));

                    AppWidgetManager widgetManager = AppWidgetManager.GetInstance(this);
                    List<int> ids = new List<int>(widgetManager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(CalendarWidget)).Name)));

                    try
                    {
                        foreach (var cfg in holder.AllCfgs())
                        {
                            if (cfg is WidgetCfg_Calendar && !ids.Contains(cfg.WidgetId))
                                DeletedWidgets.Add((WidgetCfg_Calendar)cfg);
                        }
                    }
                    catch (Exception ex)
                    {
                        sys.LogException(ex);
                    }

                    try
                    {
                        WidgetConfigHolder cfgHolderArc = new WidgetConfigHolder(true);
                        foreach (var cfgArc in cfgHolderArc.AllCfgs())
                        {
                            if (cfgArc is WidgetCfg_Calendar)
                                DeletedWidgets.Add((WidgetCfg_Calendar)cfgArc);
                        }
                    }
                    catch (Exception ex)
                    {
                        sys.LogException(ex);
                    }

                    try
                    {
                        WallpaperManager wpMgr = WallpaperManager.GetInstance(this);
                        wallpaperDrawable = wpMgr.FastDrawable;
                        wpMgr.Dispose();
                    }
                    catch (System.Exception ex)
                    {
                        try
                        {
                            WallpaperManager wpMgr = WallpaperManager.GetInstance(this);
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
                        wallpaperDrawable = Resources.GetDrawable(Resource.Drawable.dummy_wallpaper, Theme);

                    myEventsMonth = new EventCollection();

                    myEventsList = new EventCollection();

                    CalendarModel = new CalendarModelCfgHolder().GetDefaultModelCfg();

                    var dToday = CalendarModel.GetDateFromUtcDate(DateTime.Now);
                    var dFirst = dToday.BoM;
                    var dLast = dFirst.AddDays((int)(CalendarModel.GetDaysOfMonth(dFirst.Year, dFirst.Month) * 2));
                    myEventsMonth.DoLoadCalendarEventsGrouped(dFirst.UtcDate.AddDays(-7), dLast.UtcDate).Wait();
                    myEventsList.DoLoadCalendarEventsGrouped(dToday.UtcDate, dToday.UtcDate.AddDays(22), 10).Wait();

                    RunOnUiThread(() =>
                    {
                        ShowWidgetTypeSelector();
                        pDlg.Dismiss();
                    });
                }
                catch (System.Exception ex)
                {
                    ShowExitMessage(ex.Message);
                }
            });
        }

        private void ShowExitMessage(string cMessage)
        {
            var alert = new AlertDialog.Builder(this)
               .SetMessage(cMessage)
               .SetCancelable(false);
            alert.SetPositiveButton("OK", (senderAlert, args) =>
            {
                (senderAlert as Dialog).Dismiss();
                FinishAndRemoveTask();
            });

            alert.Show();
        }

        private void ShowWidgetTypeSelector()
        {
            var tStartAssistant = typeof(WidgetCfgAssistant_Calendar_Start);
            if (holder.WidgetExists<WidgetCfg_CalendarCircleWave>(appWidgetId))
            {
                ShowWidgetCircleWaveSelector();
                return;
            }
            else if (holder.WidgetExists<WidgetCfg_Calendar>(appWidgetId))
                tStartAssistant = typeof(WidgetCfgAssistant_Calendar_Theme);
            var cfg = holder.GetWidgetCfg<WidgetCfg_Calendar>(appWidgetId, false);
            var manager = new WidgetConfigAssistantManager<WidgetCfg_Calendar>(this, CalendarModel, myEventsList, myEventsMonth, wallpaperDrawable);
            Task.Factory.StartNew(async () =>
            {
                bool bStartedSubManager = false;
                try
                {
                    var result = await manager.StartAt(tStartAssistant, cfg);
                    if (result != null)
                    {
                        result.WidgetConfig.WidgetId = appWidgetId;
                        holder.SetWidgetCfg(result.WidgetConfig);

                        if (cfg == null && result.WidgetConfig is WidgetCfg_CalendarCircleWave)
                        {
                            bStartedSubManager = true;
                            ShowWidgetCircleWaveSelector(true);
                            return;
                        }

                        Intent resultValue = new Intent();
                        resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                        resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                        SetResult(Result.Ok, resultValue);

                        UpdateWidget();
                    }
                }
                catch (Exception ex)
                {
                    sys.LogException(ex);
                    RunOnUiThread(() => Toast.MakeText(this, ex.Message, ToastLength.Long).Show());
                }
                finally
                {
                    if (!bStartedSubManager)
                        FinishAndRemoveTask();
                }
            });
        }

        private void ShowWidgetCircleWaveSelector(bool bIsFirstCreate = false)
        {
            var tStartAssistant = typeof(WidgetCfgAssistant_CalendarCircleWave_OptionsBase);
            if (bIsFirstCreate)
                tStartAssistant = typeof(WidgetCfgAssistant_CalendarCircleWave_Length);
            var cfg = holder.GetWidgetCfg<WidgetCfg_CalendarCircleWave>(appWidgetId, false);
            var manager = new WidgetConfigAssistantManager<WidgetCfg_CalendarCircleWave>(this, CalendarModel, myEventsList, myEventsMonth, wallpaperDrawable);
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    var result = await manager.StartAt(tStartAssistant, cfg);
                    if (result != null)
                    {
                        result.WidgetConfig.WidgetId = appWidgetId;
                        holder.SetWidgetCfg(result.WidgetConfig);

                        Intent resultValue = new Intent();
                        resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                        resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                        SetResult(Result.Ok, resultValue);

                        UpdateWidget();
                    }
                }
                catch (Exception ex)
                {
                    sys.LogException(ex);
                    RunOnUiThread(() => Toast.MakeText(this, ex.Message, ToastLength.Long).Show());
                }
                finally
                {
                    FinishAndRemoveTask();
                }
            });

        }

        public void UpdateWidget()
        {
            Intent updateIntent = new Intent(this, typeof(CalendarWidget));
            updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            AppWidgetManager widgetManager = AppWidgetManager.GetInstance(this);
            int[] ids = new int[] { appWidgetId };
            updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            SendBroadcast(updateIntent);
        }
    }
}