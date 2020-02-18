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
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.Clock
{
    [Activity(Label = "AnalogClockWidgetConfigActivity", Name = "me.ichrono.droid.Widgets.Clock.AnalogClockWidgetConfigActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    public class AnalogClockWidgetConfigActivity : BaseWidgetActivity
    {
        AlertDialog pDlg;

        protected override void OnResume()
        {
            base.OnResume();
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
                        //RunOnUiThread(() => ShowExitMessage("Die Widget's funktionieren (aktuell) nur mit Standort-Zugriff!"));
                        //return;
                    }

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
            var holder = new WidgetConfigHolder();
            if (false && holder.WidgetExists<WidgetCfg_ClockAnalog>(appWidgetId))
            {
                Toast.MakeText(this, "widget bearbeiten...", ToastLength.Short).Show();
                FinishAndRemoveTask();
            }
            //else
            {
                var tStartAssistant = typeof(WidgetCfgAssistant_ClockAnalog_Start);
                if (holder.WidgetExists<WidgetCfg_ClockAnalog>(appWidgetId))
                    tStartAssistant = typeof(WidgetCfgAssistant_ClockAnalog_OptionsBase);
                var cfg = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(appWidgetId);
                var manager = new WidgetConfigAssistantManager<WidgetCfg_ClockAnalog>(this, wallpaperDrawable);
                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var result = await manager.StartAt(tStartAssistant, cfg);
                        if (result != null)
                        {
                            if (!holder.WidgetExists<WidgetCfg_ClockAnalog>(-101) || AppWidgetManager.GetInstance(this).GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(AnalogClockWidget)).Name)).Length == 1)
                            {
                                var tmp = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(-101);
                                tmp.PositionType = result.WidgetConfig.PositionType;
                                tmp.WidgetTitle = result.WidgetConfig.WidgetTitle;
                                tmp.Latitude = result.WidgetConfig.Latitude;
                                tmp.Longitude = result.WidgetConfig.Longitude;
                                holder.SetWidgetCfg(tmp, false);
                            }

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
        }

        public void UpdateWidget()
        {
            Intent updateIntent = new Intent(this, typeof(AnalogClockWidget));
            updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            AppWidgetManager widgetManager = AppWidgetManager.GetInstance(this);
            int[] ids = new int[] { appWidgetId };
            updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            SendBroadcast(updateIntent);
        }
    }
}