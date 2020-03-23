using System;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.Support.V4.App;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.ActionButton
{
    [Activity(Label = "ActionButtonWidgetConfigActivity", Name = "me.ichrono.droid.Widgets.ActionButton.ActionButtonWidgetConfigActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_CONFIGURE" })]
    public class ActionButtonWidgetConfigActivity : BaseWidgetActivity<WidgetCfg_ActionButton>
    {
        DynamicCalendarModel CalendarModel;

        protected override void OnResume()
        {
            base.OnResume();

#if DExxxBUG
            var cfg = new WidgetCfg_ActionButton();
            cfg.WidgetId = appWidgetId;
            cfg.ColorBackground = xColor.Aqua;
            new WidgetConfigHolder().SetWidgetCfg(cfg);

            Intent resultValue = new Intent();
            resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            SetResult(Result.Ok, resultValue);
            FinishAndRemoveTask();
            return;
#endif

            if (NeedsStartAssistant())
            {
                ShowStartAssistant();
                pDlg?.Dismiss();
            }
            else
                StartWidgetSelection();
        }

        System.Drawing.Point wSize = new System.Drawing.Point(100, 100);

        void StartWidgetSelection()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Task.Delay(100).Wait();

                    if (cfgHolder == null)
                        cfgHolder = new WidgetConfigHolder();

                    iChronoMe.Widgets.AndroidHelpers.Tools.HelperContext = this;
                    if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
                    {
                        RunOnUiThread(() => ShowExitMessage("Die Widget's funktionieren (aktuell) nur mit Standort-Zugriff!"));
                        return;
                    }

                    TryGetWallpaper();

                    CalendarModel = new CalendarModelCfgHolder().GetDefaultModelCfg();

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

        private void ShowWidgetTypeSelector()
        {
            if (sys.AllDrawables.Count == 0)
            {
                foreach (var prop in typeof(Resource.Drawable).GetFields())
                    sys.AllDrawables.Add(prop.Name);
            }

            var tStartAssistant = typeof(WidgetCfgAssistant_ActionButton_ClickAction);
            var cfg = cfgHolder.GetWidgetCfg<WidgetCfg_ActionButton>(appWidgetId, false);
            if (cfg != null)
                tStartAssistant = typeof(WidgetCfgAssistant_ActionButton_OptionsBase);
            if (cfg == null)
                cfg = new WidgetCfg_ActionButton();
            var manager = new WidgetConfigAssistantManager<WidgetCfg_ActionButton>(this, wallpaperDrawable);
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    var result = await manager.StartAt(tStartAssistant, cfg);
                    if (result != null)
                    {
                        new WidgetConfigHolder().SetWidgetCfg(result.WidgetConfig, appWidgetId);

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
    }
}