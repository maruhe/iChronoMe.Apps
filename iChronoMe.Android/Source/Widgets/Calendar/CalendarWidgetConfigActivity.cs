using System;
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
                if (holder.WidgetExists<WidgetCfg_Calendar>(appWidgetId))
                {
                    var it = new Intent(this, typeof(CalendarWidgetConfigActivityAdvanced));
                    it.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                    StartActivity(it);
                }
                else
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

            Task.Factory.StartNew(async () =>
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
            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, myEventsMonth, myEventsList, null);
            listAdapter.Items.Add("Agenda", new WidgetCfg_CalendarTimetable());
            listAdapter.Items.Add("Round Calendar", new WidgetCfg_CalendarCircleWave());
            listAdapter.Items.Add("Monatsansicht", new WidgetCfg_CalendarMonthView());
            if (DeletedWidgets.Count > 0)
                listAdapter.Items.Add("gelöschte Widgets", null);

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Kalender-Typ")
                .SetSingleChoiceItems(listAdapter, -1, new WidgetTypeOnClickListener(this, appWidgetId, listAdapter))
                .SetNegativeButton("abbrechen", new myCancelClickListener(this))
                .SetOnCancelListener(new myDialogCancelListener(this))
                .Create();
            dlg.Show();
        }

        public void ShowDeletedWidgetSelector()
        {
            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, myEventsMonth, myEventsList, wallpaperDrawable);

            foreach (var cfg in DeletedWidgets)
                listAdapter.Items.Add(cfg.WidgetId.ToString(), cfg);

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Gelöschte Widgets")
                .SetSingleChoiceItems(listAdapter, -1, new DeletedWidgetOnClickListener(this, appWidgetId, listAdapter))
                .SetNegativeButton("genug", new myCancelClickListener(this))
                .SetOnCancelListener(new myDialogCancelListener(this))
                .Create();
            dlg.Show();
        }

        public void ShowWidgetStyleSelector(Type widgetType)
        {
            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, myEventsMonth, myEventsList, wallpaperDrawable);

            foreach (var o in Enum.GetValues(typeof(WidgetTheme)))
            {
                var cfg = Activator.CreateInstance(widgetType);
                (cfg as WidgetCfg).SetTheme((WidgetTheme)o);
                listAdapter.Items.Add(o.ToString(), cfg as WidgetCfg_Calendar);
            }

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Hintergrund und Titel")
                .SetSingleChoiceItems(listAdapter, -1, new WidgetThemeOnClickListener(this, appWidgetId, listAdapter))
                .SetNegativeButton("genug", new myCancelClickListener(this))
                .SetOnCancelListener(new myDialogCancelListener(this))
                .Create();
            dlg.Show();
        }

        public void ShowCircleWidgetLengthSelector(WidgetCfg_CalendarCircleWave cfgTemplate)
        {
            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, myEventsMonth, myEventsList, wallpaperDrawable);

            var cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.FirstDayType = FirstDayType.WeekStart;
            cfg.TimeUnit = TimeUnit.Week;
            cfg.TimeUnitCount = 2;
            listAdapter.Items.Add("2 Wochen", cfg);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.FirstDayType = FirstDayType.MonthStart;
            cfg.TimeUnit = TimeUnit.Month;
            cfg.TimeUnitCount = 1;
            listAdapter.Items.Add("1 Monat", cfg);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.FirstDayType = FirstDayType.MonthStart;
            cfg.TimeUnit = TimeUnit.Month;
            cfg.TimeUnitCount = 2;
            listAdapter.Items.Add("2 Monate", cfg);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.FirstDayType = FirstDayType.MonthStart;
            cfg.TimeUnit = TimeUnit.Month;
            cfg.TimeUnitCount = 3;
            listAdapter.Items.Add("3 Monate", cfg);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.FirstDayType = FirstDayType.MonthStart;
            cfg.TimeUnit = TimeUnit.Month;
            cfg.TimeUnitCount = CalendarModel.GetMonthsOfYear(CalendarModel.GetYearFromUtcDate(DateTime.Now).Year) / 2;
            listAdapter.Items.Add(cfg.TimeUnitCount + " Monate", cfg);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.FirstDayType = FirstDayType.YearStart;
            cfg.TimeUnit = TimeUnit.Year;
            cfg.TimeUnitCount = 1;
            listAdapter.Items.Add("1 Jahr", cfg);

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Zeitspanne")
                .SetSingleChoiceItems(listAdapter, -1, new CircleLengthOnClickListener(this, appWidgetId, listAdapter))
                .SetNegativeButton("genug", new myCancelClickListener(this))
                .SetOnCancelListener(new myDialogCancelListener(this))
                .Create();
            dlg.Show();
        }

        public void ShowCircleWidgetDayColorTypeSelector(WidgetCfg_CalendarCircleWave cfgTemplate)
        {
            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, myEventsMonth, myEventsList, wallpaperDrawable);

            var clr = xColor.FromHex(DynamicColors.SampleColorSetS[5][2]);

            var cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(cfgTemplate.TimeUnit, Core.DynamicCalendar.GradientType.StaticColor) { CustomColors = new xColor[] { clr } } } };
            listAdapter.Items.Add("Einfärbig", cfg);


            var clr1 = clr.AddLuminosity(.1);
            var clr2 = clr.AddLuminosity(-.1);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(cfgTemplate.TimeUnit, new xColor[] { clr1, clr2 }) } };
            listAdapter.Items.Add("Hell nach Dunkel", cfg);

            clr1 = clr.AddLuminosity(-.1);
            clr2 = clr.AddLuminosity(.1);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(cfgTemplate.TimeUnit, new xColor[] { clr1, clr2 }) } };
            listAdapter.Items.Add("Dunkel nach Hell", cfg);


            var clrs = DynamicColors.SampleColorSetS[5];

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(cfgTemplate.TimeUnit, new xColor[] { clrs[0], clrs[1] }) } };
            listAdapter.Items.Add("2 Farben", cfg);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(cfgTemplate.TimeUnit, new xColor[] { clrs[0], clrs[1], clrs[2] }) } };
            listAdapter.Items.Add("3 Farben", cfg);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(cfgTemplate.TimeUnit, new xColor[] { clrs[0], clrs[1], clrs[2], clrs[3] }) } };
            listAdapter.Items.Add("4 Farben", cfg);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(cfgTemplate.TimeUnit, new xColor[] { clrs[0], clrs[1], clrs[2], clrs[3], clrs[4] }) } };
            listAdapter.Items.Add("5 Farben", cfg);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(TimeUnit.Month, Core.DynamicCalendar.GradientType.Rainbow) } };
            listAdapter.Items.Add("Ein Regenbogen pro Monat", cfg);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(TimeUnit.Year, Core.DynamicCalendar.GradientType.Rainbow) } };
            listAdapter.Items.Add("Ein Regenbogen pro Jahr", cfg);

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Tagesfarben")
                .SetSingleChoiceItems(listAdapter, -1, new CircleDayColorTypeOnClickListener(this, appWidgetId, listAdapter))
                .SetPositiveButton("custom", (d, e) =>
                    {
                        //(d as IDialogInterface)?.Dismiss();
                        ShowCircleWidgetCustomDayColorsSelector(cfgTemplate);
                    })
                .SetNegativeButton("genug", new myCancelClickListener(this))
                .SetOnCancelListener(new myDialogCancelListener(this))
                .Create();
            dlg.Show();
        }

        public void ShowCircleWidgetDayColorsSelector(WidgetCfg_CalendarCircleWave cfgTemplate, int iClrS)
        {
            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, myEventsMonth, myEventsList, wallpaperDrawable) { ShowColorList = true };

            int i1 = Math.Max(4 - iClrS, 0);
            int i2 = Math.Min(4, i1 + iClrS - 1);
            float nLum1 = 0;
            float nLum2 = 0;
            if (iClrS == 1 && cfgTemplate.DayBackgroundGradient.GradientS[0].CustomColors.Length == 2)
            {
                if (cfgTemplate.DayBackgroundGradient.GradientS[0].CustomColors[0].Luminosity > cfgTemplate.DayBackgroundGradient.GradientS[0].CustomColors[1].Luminosity)
                {
                    nLum1 = .1F;
                    nLum2 = -.1F;
                }
                else
                {
                    nLum1 = -.1F;
                    nLum2 = .1F;
                }
            }

            int i = 0;
            foreach (var clrs in DynamicColors.SampleColorSetS)
            {
                i++;
                var cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                List<xColor> xclrs = new List<xColor>();

                for (int iClr = i1; iClr <= i2; iClr++)
                {
                    xclrs.Add(xColor.FromHex(clrs[iClr]));
                }
                if (iClrS == 1 && nLum1 != 0)
                {
                    var clr1 = xclrs[0].AddLuminosity(nLum1);
                    var clr2 = xclrs[0].AddLuminosity(nLum2);
                    xclrs.Clear();
                    xclrs.Add(clr1);
                    xclrs.Add(clr2);
                }
                cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(cfg.TimeUnit, xclrs.ToArray()) } };
                listAdapter.Items.Add("Sample " + i, cfg);
            }

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Tagesfarben")
                .SetSingleChoiceItems(listAdapter, -1, new CircleDayColorsOnClickListener(this, appWidgetId, listAdapter))
                .SetPositiveButton("custom", (d, e) =>
                {
                    //(d as IDialogInterface)?.Dismiss();
                    ShowCircleWidgetCustomDayColorsSelector(cfgTemplate);
                })
                .SetNegativeButton("genug", new myCancelClickListener(this))
                .SetOnCancelListener(new CancelToCircleWidgetDayColorTypeSelectorListener(this, cfgTemplate))
                .Create();
            dlg.Show();
        }

        public void ShowCircleWidgetCustomDayColorsSelector(WidgetCfg_CalendarCircleWave cfgTemplate)
        {
            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Tagesfarben")
                .SetMessage("Es erscheinen nun Fenster zur Farbauswahl\njede gewählte Farbe wird dem Farbverlauf hinzugefügt\nwenn du genug Farben beisammen hast klick einfach außerhalb des Fensters oder auf 'Zurück'")
                .SetPositiveButton("let's go", async (d, w) =>
                {
                    List<xColor> xclrs = new List<xColor>();
                    int iClr = 0;
                    while (true)
                    {
                        iClr++;
                        var clrDlg = ColorPickerDialog.NewBuilder()
                            .SetDialogType(ColorPickerDialog.DialogType.Preset)
                            .SetAllowCustom(true)
                            .SetShowColorShades(true)
                            .SetColorShape(ColorShape.Circle)
                            .SetShowAlphaSlider(false)
                            .SetDialogTitle("Farbe " + iClr);

                        var clr = await clrDlg.ShowAsyncNullable(this);

                        if (clr == null)
                            break;
                        else
                            xclrs.Add(clr.Value.ToColor());
                    }
                    if (xclrs.Count == 0)
                        ShowCircleWidgetDayColorTypeSelector(cfgTemplate);
                    else if (xclrs.Count == 1)
                    {
                        var cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                        cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(TimeUnit.Month, xclrs.ToArray()) } };
                        ShowCircleWidgetTodayColorsSelector(cfg);
                    }
                    else
                    {
                        var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, myEventsMonth, myEventsList, wallpaperDrawable);

                        var cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                        cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(TimeUnit.Week, xclrs.ToArray()) } };
                        listAdapter.Items.Add("je Woche", cfg);

                        cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                        cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(TimeUnit.Month, xclrs.ToArray()) } };
                        listAdapter.Items.Add("je Monat", cfg);

                        cfg.WidgetId = appWidgetId;
                        new WidgetConfigHolder().SetWidgetCfg(cfg);
                        Task.Delay(100).Wait();
                        UpdateWidget();


                        cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                        cfg.DayBackgroundGradient = new DateGradient() { GradientS = { new DynamicGradient(TimeUnit.Year, xclrs.ToArray()) } };
                        listAdapter.Items.Add("je Jahr", cfg);

                        var dlg2 = new AlertDialog.Builder(this)
                            .SetTitle("Tagesfarben")
                            .SetSingleChoiceItems(listAdapter, -1, new CircleDayColorsOnClickListener(this, appWidgetId, listAdapter))
                            .SetNegativeButton("genug", new myCancelClickListener(this))
                            .SetOnCancelListener(new CancelToCircleWidgetDayColorTypeSelectorListener(this, cfgTemplate))
                            .Create();
                        dlg2.Show();
                    }
                })
                .SetOnCancelListener(new CancelToCircleWidgetDayColorTypeSelectorListener(this, cfgTemplate))
                .Create();
            dlg.Show();

        }

        public void ShowCircleWidgetTodayColorsSelector(WidgetCfg_CalendarCircleWave cfgTemplate)
        {
            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, myEventsMonth, myEventsList, wallpaperDrawable);

            listAdapter.Items.Add("Tagesfarbe", (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone());

            var cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.ColorTodayBackground = WidgetCfg.tcLight;
            listAdapter.Items.Add(WidgetCfg.tcLight.HexString, cfg);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.ColorTodayBackground = WidgetCfg.tcDark;
            listAdapter.Items.Add(WidgetCfg.tcDark.HexString, cfg);

            int i = 0;
            foreach (var clrs in DynamicColors.SampleColorSetS)
            {
                i++;
                var clr = xColor.FromHex(clrs[0]);
                if (!listAdapter.Items.ContainsKey(clr.HexString))
                {
                    cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                    cfg.ColorTodayBackground = clr;
                    listAdapter.Items.Add(clr.HexString, cfg);
                }
            }

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Heute hervorheben")
                .SetSingleChoiceItems(listAdapter, -1, new CircleTodayColorsOnClickListener(this, appWidgetId, listAdapter))
                .SetPositiveButton("custom", async (d, w) =>
                {
                    var clrDlg = ColorPickerDialog.NewBuilder()
                        .SetDialogType(ColorPickerDialog.DialogType.Preset)
                        .SetAllowCustom(true)
                        .SetShowColorShades(true)
                        .SetColorShape(ColorShape.Circle)
                        .SetShowAlphaSlider(false)
                        .SetDialogTitle("Heute hervorheben");

                    var clr = await clrDlg.ShowAsyncNullable(this);

                    cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                    if (clr.HasValue)
                    {
                        cfg.ColorTodayBackground = clr.Value.ToColor();
                        cfg.WidgetId = appWidgetId;
                        new WidgetConfigHolder().SetWidgetCfg(cfg);
                        Task.Delay(100).Wait();
                        UpdateWidget();
                    }
                    ShowCircleWidgetDayNumbersTypeSelector(cfg);
                })
                .SetNegativeButton("genug", new myCancelClickListener(this))
                .SetOnCancelListener(new CancelToCircleWidgetDayColorTypeSelectorListener(this, cfgTemplate))
                .Create();
            dlg.Show();
        }

        public void ShowCircleWidgetDayNumbersTypeSelector(WidgetCfg_CalendarCircleWave cfgTemplate)
        {
            var tToday = DateTime.Now;
            var dToday = CalendarModel.GetDateFromUtcDate(tToday);
            if (CalendarModel.BaseSample != CalendarModelSample.Gregorian.ToString() || dToday.DayNumber != tToday.Day || dToday.MonthNumber != tToday.Month || dToday.YearNumber != tToday.Year)
            {
                var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, myEventsMonth, myEventsList, wallpaperDrawable);

                var cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                cfg.DayNumberStyle = DayNumberStyle.CalendarModell;
                listAdapter.Items.Add(DayNumberStyle.CalendarModell.ToString(), cfg);

                cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                cfg.DayNumberStyle = DayNumberStyle.Gregorian;
                listAdapter.Items.Add(DayNumberStyle.Gregorian.ToString(), cfg);

                cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                cfg.DayNumberStyle = DayNumberStyle.CalendarModellAndGregorian;
                listAdapter.Items.Add(DayNumberStyle.CalendarModellAndGregorian.ToString(), cfg);

                cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                cfg.DayNumberStyle = DayNumberStyle.GregorianAndCalendarModell;
                listAdapter.Items.Add(DayNumberStyle.GregorianAndCalendarModell.ToString(), cfg);

                cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                cfg.DayNumberStyle = DayNumberStyle.None;
                listAdapter.Items.Add(DayNumberStyle.None.ToString(), cfg);

                var dlg = new AlertDialog.Builder(this)
                    .SetTitle("Nummerierung")
                    .SetSingleChoiceItems(listAdapter, -1, new CircleDayNumbersTypeOnClickListener(this, appWidgetId, listAdapter))
                    .SetNegativeButton("genug", new myCancelClickListener(this))
                    .SetOnCancelListener(new CancelToCircleWidgetDayColorTypeSelectorListener(this, cfgTemplate))
                    .Create();
                dlg.Show();

            }
            else
                ShowCircleWidgetDayNumbersColorsSelector(cfgTemplate);
        }

        public void ShowCircleWidgetDayNumbersColorsSelector(WidgetCfg_CalendarCircleWave cfgTemplate)
        {
            FinishAndRemoveTask();
            return;

            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, myEventsMonth, myEventsList, wallpaperDrawable);

            listAdapter.Items.Add("dfsdfds", (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone());

            var cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.ColorTodayBackground = WidgetCfg.tcLight;
            listAdapter.Items.Add(WidgetCfg.tcLight.HexString, cfg);

            cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
            cfg.ColorTodayBackground = WidgetCfg.tcDark;
            listAdapter.Items.Add(WidgetCfg.tcDark.HexString, cfg);

            int i = 0;
            foreach (var clrs in DynamicColors.SampleColorSetS)
            {
                i++;
                var clr = xColor.FromHex(clrs[0]);
                if (!listAdapter.Items.ContainsKey(clr.HexString))
                {
                    cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                    cfg.ColorTodayBackground = clr;
                    listAdapter.Items.Add(clr.HexString, cfg);
                }
            }

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("fsfdsfds")
                .SetSingleChoiceItems(listAdapter, -1, new CircleTodayColorsOnClickListener(this, appWidgetId, listAdapter))
                /*.SetPositiveButton("custom", async (d, w) => {
                    var clrDlg = ColorPickerDialog.NewBuilder()
                        .SetDialogType(ColorPickerDialog.DialogType.Preset)
                        .SetAllowCustom(true)
                        .SetShowColorShades(true)
                        .SetColorShape(ColorShape.Circle)
                        .SetShowAlphaSlider(false)
                        .SetDialogTitle("sfjdsljfkd");

                    var clr = await clrDlg.ShowAsyncNullable(this);

                    cfg = (WidgetCfg_CalendarCircleWave)cfgTemplate.Clone();
                    if (clr.HasValue)
                    {
                        cfg.ColorTodayBackground = clr.Value.ToColor();
                        cfg.WidgetId = appWidgetId;
                        new WidgetConfigHolder().SetWidgetCfg(cfg);
                        Task.Delay(100).Wait();
                        UpdateWidget();
                    }
                    ShowCircleWidgetDayNumbersTypeSelector(cfg);
                })*/
                .SetNegativeButton("genug", new myCancelClickListener(this))
                .SetOnCancelListener(new CancelToCircleWidgetDayColorTypeSelectorListener(this, cfgTemplate))
                .Create();
            dlg.Show();
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

    public class WidgetTypeOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        CalendarWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public WidgetTypeOnClickListener(CalendarWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            Intent resultValue = new Intent();
            resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, myActivity.appWidgetId);
            myActivity.SetResult(Result.Ok, resultValue);

            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];
            if (cfg == null)
            {
                myActivity.ShowDeletedWidgetSelector();
                return;
            }

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.ShowWidgetStyleSelector(cfg.GetType());
        }
    }

    public class DeletedWidgetOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        CalendarWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public DeletedWidgetOnClickListener(CalendarWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            Intent resultValue = new Intent();
            resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, myActivity.appWidgetId);
            myActivity.SetResult(Result.Ok, resultValue);

            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.FinishAndRemoveTask();
        }
    }

    public class WidgetThemeOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        CalendarWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public WidgetThemeOnClickListener(CalendarWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            if (cfg is WidgetCfg_CalendarCircleWave)
                myActivity.ShowCircleWidgetLengthSelector(cfg as WidgetCfg_CalendarCircleWave);
            else
                myActivity.FinishAndRemoveTask();
        }
    }

    public class CircleLengthOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        CalendarWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public CircleLengthOnClickListener(CalendarWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.ShowCircleWidgetDayColorTypeSelector(cfg as WidgetCfg_CalendarCircleWave);
        }
    }

    public class CircleDayColorTypeOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        CalendarWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public CircleDayColorTypeOnClickListener(CalendarWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            if (which > 6)
                myActivity.ShowCircleWidgetTodayColorsSelector(cfg as WidgetCfg_CalendarCircleWave);
            else
            {
                int iClrs = 1;
                if (which >= 3)
                    iClrs = (cfg as WidgetCfg_CalendarCircleWave).DayBackgroundGradient.GradientS[0].CustomColors.Length;
                myActivity.ShowCircleWidgetDayColorsSelector(cfg as WidgetCfg_CalendarCircleWave, iClrs);
            }
        }
    }


    public class CircleDayColorsOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        CalendarWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public CircleDayColorsOnClickListener(CalendarWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.ShowCircleWidgetTodayColorsSelector((WidgetCfg_CalendarCircleWave)cfg);
        }
    }

    public class CircleTodayColorsOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        CalendarWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public CircleTodayColorsOnClickListener(CalendarWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.ShowCircleWidgetDayNumbersTypeSelector((WidgetCfg_CalendarCircleWave)cfg);
        }
    }

    public class CircleDayNumbersTypeOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        CalendarWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public CircleDayNumbersTypeOnClickListener(CalendarWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.ShowCircleWidgetDayNumbersColorsSelector((WidgetCfg_CalendarCircleWave)cfg);
        }
    }

    public class myCancelClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        Activity myActivity;

        public myCancelClickListener(Activity activity)
        {
            myActivity = activity;
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            dialog?.Dismiss();
            myActivity.FinishAndRemoveTask();
        }

        protected override void Dispose(bool disposing)
        {
            myActivity = null;
            base.Dispose(disposing);
        }
    }

    public class myDialogCancelListener : Java.Lang.Object, IDialogInterfaceOnCancelListener
    {
        Activity myActivity;

        public myDialogCancelListener(Activity activity)
        {
            myActivity = activity;
        }

        public void OnCancel(IDialogInterface dialog)
        {
            dialog?.Dismiss();
            myActivity.FinishAndRemoveTask();
        }
    }

    public class CancelToCircleWidgetDayColorTypeSelectorListener : Java.Lang.Object, IDialogInterfaceOnCancelListener
    {
        CalendarWidgetConfigActivity myActivity;
        WidgetCfg_CalendarCircleWave Cfg;

        public CancelToCircleWidgetDayColorTypeSelectorListener(CalendarWidgetConfigActivity activity, WidgetCfg_CalendarCircleWave cfg)
        {
            myActivity = activity;
            Cfg = cfg;
        }

        public void OnCancel(IDialogInterface dialog)
        {
            dialog?.Dismiss();
            myActivity.ShowCircleWidgetDayColorTypeSelector(Cfg);
        }
    }

}