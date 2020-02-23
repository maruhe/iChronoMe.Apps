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
using iChronoMe.Core.Types;
using iChronoMe.Widgets;

using Net.ArcanaStudio.ColorPicker;

namespace iChronoMe.Droid.Widgets.ActionButton
{
    [Activity(Label = "ActionButtonWidgetConfigActivity", Name = "me.ichrono.droid.Widgets.ActionButton.ActionButtonWidgetConfigActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    public class ActionButtonWidgetConfigActivity : BaseWidgetActivity
    {
        DynamicCalendarModel CalendarModel;
        AlertDialog pDlg;

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
                ShowStartAssistant();
            else
            {
                var progressBar = new ProgressBar(this);
                progressBar.Indeterminate = true;
                pDlg = new AlertDialog.Builder(this)
                    .SetCancelable(false)
                    .SetTitle(Resource.String.progress_preparing_data)
                    .SetView(progressBar)
                    .Create();
                pDlg.Show();

                StartWidgetSelection();
            }
        }

        System.Drawing.Point wSize = new System.Drawing.Point(100, 100);

        void StartWidgetSelection()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Task.Delay(100).Wait();


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
            //if (holder.WidgetExists<WidgetCfg_ActionButton>(appWidgetId))
            //  tStartAssistant = typeof(WidgetCfgAssistant_ActionButton_OptionsBase);
            //var cfg = holder.GetWidgetCfg<WidgetCfg_ActionButton>(appWidgetId);
            var cfg = new WidgetCfg_ActionButton();
            var manager = new WidgetConfigAssistantManager<WidgetCfg_ActionButton>(this, wallpaperDrawable);
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    var result = await manager.StartAt(tStartAssistant, cfg);
                    if (result != null)
                    {
                        result.WidgetConfig.WidgetId = appWidgetId;
                        new WidgetConfigHolder().SetWidgetCfg(result.WidgetConfig);

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
            Intent updateIntent = new Intent(this, typeof(ActionButtonWidget));
            updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            AppWidgetManager widgetManager = AppWidgetManager.GetInstance(this);
            int[] ids = new int[] { appWidgetId };
            updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            SendBroadcast(updateIntent);
        }
    }
}