using System;
using System.Threading.Tasks;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.Clock
{
    [Activity(Label = "AnalogClockWidgetConfigActivity", Name = "me.ichrono.droid.Widgets.Clock.AnalogClockWidgetConfigActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    public class AnalogClockWidgetConfigActivity : BaseWidgetActivity
    {
        public int appWidgetId = -1;

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

        protected override void OnResume()
        {
            base.OnResume();
            if (NeedsStartAssistant())
                ShowStartAssistant();
            else
                StartWidgetConfig();
        }

        public void StartWidgetConfig()
        {
            var holder = new WidgetConfigHolder();
            if (holder.WidgetExists<WidgetCfg_ClockAnalog>(appWidgetId))
            {
                Toast.MakeText(this, "widget bearbeiten...", ToastLength.Short).Show();
                FinishAndRemoveTask();
            }
            else
            {
                var cfg = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(appWidgetId);
                var manager = new WidgetConfigAssistantManager<WidgetCfg_ClockAnalog>(this);
                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var result = await manager.StartAt(typeof(WidgetCfgAssistant_ClockAnalog_Start), cfg);
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
                        ex.ToString();
                        Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
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