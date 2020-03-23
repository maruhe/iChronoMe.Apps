using System;
using System.Collections.Generic;
using System.Threading;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets
{
    public abstract class BaseWidgetActivity<T> : BaseActivity
        where T : WidgetCfg
    {
        protected Drawable wallpaperDrawable;
        protected int appWidgetId = -1;
        protected AppWidgetManager widgetManager = null;
        protected WidgetConfigHolder cfgHolder = null;
        protected List<T> DeletedWidgets { get; } = new List<T>();
        protected AlertDialog pDlg;


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
                Toast.MakeText(this, Resource.String.error_message_paramers, ToastLength.Long).Show();
                FinishAndRemoveTask();
                return;
            }

            LoadAppTheme();
            SetTheme(Resource.Style.TransparentTheme);

            var progressBar = new ProgressBar(this);
            progressBar.Indeterminate = true;
            pDlg = new AlertDialog.Builder(this)
                .SetCancelable(false)
                .SetTitle(Resource.String.progress_preparing_data)
                .SetView(progressBar)
                .Create();
            pDlg.Show();
        }

        protected void TryGetWallpaper()
        {
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
                wallpaperDrawable = Resources.GetDrawable(Resource.Mipmap.dummy_wallpaper, Theme);
        }

        protected void SearchForDeletedWidgets(WidgetConfigHolder cfgHolderArc = null, WidgetConfigHolder cfgHolderExtra = null)
        {
            if (widgetManager == null)
                widgetManager = AppWidgetManager.GetInstance(this);
            if (cfgHolder == null)
                cfgHolder = new WidgetConfigHolder();
            var t = GetWidgetType();
            if (t == null)
                return;
            List<int> ids = new List<int>(widgetManager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(t).Name)));

            try
            {
                foreach (var cfg in cfgHolder.AllCfgs())
                {
                    if (cfg is T && !ids.Contains(cfg.WidgetId))
                        DeletedWidgets.Add((T)cfg);
                }
            }
            catch (Exception ex)
            {
                sys.LogException(ex);
            }

            try
            {
                if (cfgHolderArc == null)
                    cfgHolderArc = new WidgetConfigHolder(true);
                foreach (var cfgArc in cfgHolderArc.AllCfgs())
                {
                    if (cfgArc is T)
                        DeletedWidgets.Add((T)cfgArc);
                }
            }
            catch (Exception ex)
            {
                sys.LogException(ex);
            }
            if (cfgHolderExtra != null)
            {
                try
                {
                    foreach (var cfgArc in cfgHolderExtra.AllCfgs())
                    {
                        if (cfgArc is T)
                            DeletedWidgets.Add((T)cfgArc);
                    }
                }
                catch (Exception ex)
                {
                    sys.LogException(ex);
                }
            }
        }

        public void UpdateWidget()
        {
            var t = GetWidgetType();
            if (t == null)
                return;
            Intent updateIntent = new Intent(this, t);
            updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            if (widgetManager == null)
                widgetManager = AppWidgetManager.GetInstance(this);
            int[] ids = new int[] { appWidgetId };
            updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            SendBroadcast(updateIntent);
        }

        private Type GetWidgetType()
        {
            if (typeof(T) == typeof(WidgetCfg_ClockAnalog))
                return typeof(Clock.AnalogClockWidget);
            if (typeof(T) == typeof(WidgetCfg_Calendar))
                return typeof(Calendar.CalendarWidget);
            if (typeof(T) == typeof(WidgetCfg_ActionButton))
                return typeof(ActionButton.ActionButtonWidget);
            if (typeof(T) == typeof(WidgetCfg_Lifetime))
                return typeof(Lifetime.LifetimeWidget);

            return null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            new Thread(() =>
            {
                try
                {
                    System.IO.Directory.Delete(System.IO.Path.Combine(sys.PathCache, "WidgetPreview"), true);
                }
                catch { };
            }).Start();
        }
    }
}