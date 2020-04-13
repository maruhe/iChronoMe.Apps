using System;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.Lifetime
{
#if DEBUG
    [BroadcastReceiver(Label = "@string/widget_title_lifetime", Name = "me.ichrono.droid.Lifetime.LifetimeWidget")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/widget_lifetime")]
#endif
    public class LifetimeWidget : MainWidgetBase
    {
        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            xLog.Verbose("start");

            var cfgHolder = new WidgetConfigHolder();

            foreach (int iWidgetId in appWidgetIds)
            {
                var cfg = cfgHolder.GetWidgetCfg<WidgetCfg_Lifetime>(iWidgetId);
                RemoteViews rv = new RemoteViews(context.PackageName, Resource.Layout.widget_lifetime);

                Point wSize = MainWidgetBase.GetWidgetSize(iWidgetId, cfg, appWidgetManager);

                bool mini = wSize.X < 100;

                DateTime bLifeStart = cfg.LifeStartTime;
                if (bLifeStart == DateTime.MinValue)
                    continue;

                rv.SetTextViewText(Resource.Id.widget_title, cfg.WidgetTitle);
                rv.SetTextColor(Resource.Id.widget_title, cfg.ColorTitleText.ToAndroid());

                string c = "";
                TimeSpan tsLifeTime = DateTime.Now - bLifeStart;
                //c += tsLifeTime.ToString(@"d\.hh\:mm") + "\n";
                int iWeeks = tsLifeTime.Days / 7;
                int iWeekRest = tsLifeTime.Days - (iWeeks * 7);
                c += tsLifeTime.Days.ToString() + (mini ? " f.d." : " volle Tage") + "\n";
                c += iWeeks.ToString() + (mini ? " w." : " Wochen") + (iWeekRest > 0 ? " +" + iWeekRest.ToString() : "") + "\n";

                int iYears = (int)(tsLifeTime.TotalDays / sys.OneYear);
                TimeSpan tsLifeTimeRest = tsLifeTime.Add(TimeSpan.FromDays(iYears * sys.OneYear * -1));

                c += iYears.ToString() + " sun's," + (mini ? "\n" : " ") + tsLifeTimeRest.Days.ToString() + "d, " + tsLifeTimeRest.Hours + "h";

                rv.SetTextViewText(Resource.Id.widget_text, c);
                rv.SetTextColor(Resource.Id.widget_text, cfg.ColorLifetimeText.ToAndroid());

                rv.SetViewVisibility(Resource.Id.background_image, ViewStates.Gone);
                if (cfg.ColorBackground.ToAndroid() != Color.Transparent)
                {
                    GradientDrawable back = new GradientDrawable();
                    back.SetShape(ShapeType.Rectangle);
                    int i = 16;
                    back.SetCornerRadii(new float[] { i, i, i, i, i, i, i, i });
                    back.SetColor(cfg.ColorBackground.ToAndroid());
                    //back.SetStroke(1, Color.Black);
                    rv.SetViewVisibility(Resource.Id.background_image, ViewStates.Visible);
                    rv.SetImageViewBitmap(Resource.Id.background_image, MainWidgetBase.GetDrawableBmp(back, wSize.X, wSize.Y));
                }

                if (cfg.ShowLifeTimeProgress || cfg.ShowLifeTimePercentage)
                {
                    rv.SetViewVisibility(Resource.Id.eof_layout, ViewStates.Visible);
                    TimeSpan tsFullLifeLength = cfg.EndOfLifeTime - cfg.LifeStartTime;
                    TimeSpan tsDoneLifeLength = DateTime.Now - cfg.LifeStartTime;
                    if (cfg.ShowLifeTimeProgress)
                    {
                        rv.SetViewVisibility(Resource.Id.eof_progress_layout, ViewStates.Visible);
                        rv.SetProgressBar(Resource.Id.eof_progress, 1000, (int)(tsDoneLifeLength.TotalDays * 1000 / tsFullLifeLength.TotalDays), false);
                    }
                    else
                        rv.SetViewVisibility(Resource.Id.eof_progress_layout, ViewStates.Gone);
                    if (cfg.ShowLifeTimePercentage)
                    {
                        rv.SetViewVisibility(Resource.Id.eof_percentage, ViewStates.Visible);
                        rv.SetTextViewText(Resource.Id.eof_percentage, (tsDoneLifeLength.TotalDays * 100 / tsFullLifeLength.TotalDays).ToString("0.##") + "% ");
                        rv.SetTextColor(Resource.Id.eof_percentage, cfg.ColorLifeTimePercentage.ToAndroid());
                    }
                    else
                        rv.SetViewVisibility(Resource.Id.eof_percentage, ViewStates.Gone);
                }
                else
                    rv.SetViewVisibility(Resource.Id.eof_layout, ViewStates.Gone);

                //Config Click
                Intent cfgIntent = new Intent(Intent.ActionMain);
                cfgIntent.SetComponent(ComponentName.UnflattenFromString("me.ichrono.droid/me.ichrono.droid.Widgets.Lifetime.LifetimeWidgetConfigActivity"));
                cfgIntent.SetFlags(ActivityFlags.NoHistory);
                cfgIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, iWidgetId);
                var cfgPendingIntent = PendingIntent.GetActivity(context, iWidgetId, cfgIntent, PendingIntentFlags.UpdateCurrent);
                rv.SetOnClickPendingIntent(Resource.Id.widget, cfgPendingIntent);

                appWidgetManager.UpdateAppWidget(iWidgetId, rv);

            }
            base.OnUpdate(context, appWidgetManager, appWidgetIds);
        }
    }
}