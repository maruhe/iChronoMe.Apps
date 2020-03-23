using System;
using System.Threading.Tasks;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.Clock
{
    [Activity(Label = "AnalogClockWidgetConfigActivity", Name = "me.ichrono.droid.Widgets.Clock.AnalogClockWidgetConfigActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_CONFIGURE" })]
    public class AnalogClockWidgetConfigActivity : BaseWidgetActivity<WidgetCfg_ClockAnalog>
    {

        protected override void OnResume()
        {
            base.OnResume();
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

                    SearchForDeletedWidgets();
                    TryGetWallpaper();

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
            var cfg = cfgHolder.GetWidgetCfg<WidgetCfg_ClockAnalog>(appWidgetId, false);
            var tStartAssistant = typeof(WidgetCfgAssistant_ClockAnalog_Start);
            if (cfg != null)
                tStartAssistant = typeof(WidgetCfgAssistant_ClockAnalog_OptionsBase);
            if (cfg == null)
                cfg = new WidgetCfg_ClockAnalog();
            var manager = new WidgetConfigAssistantManager<WidgetCfg_ClockAnalog>(this, wallpaperDrawable);
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    var result = await manager.StartAt(tStartAssistant, cfg);
                    if (result != null)
                    {
                        if (!cfgHolder.WidgetExists<WidgetCfg_ClockAnalog>(-101) || AppWidgetManager.GetInstance(this).GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(AnalogClockWidget)).Name)).Length == 1)
                        {
                            var tmp = cfgHolder.GetWidgetCfg<WidgetCfg_ClockAnalog>(-101);
                            tmp.PositionType = result.WidgetConfig.PositionType;
                            tmp.WidgetTitle = result.WidgetConfig.WidgetTitle;
                            tmp.Latitude = result.WidgetConfig.Latitude;
                            tmp.Longitude = result.WidgetConfig.Longitude;
                            cfgHolder.SetWidgetCfg(tmp, false);
                        }

                        cfgHolder.SetWidgetCfg(result.WidgetConfig, appWidgetId);

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