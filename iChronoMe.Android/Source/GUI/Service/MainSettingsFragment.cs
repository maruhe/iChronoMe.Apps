using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Core.DataBinding;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Core.ViewModels;
using iChronoMe.Droid.Adapters;
using iChronoMe.Droid.ViewModels;
using iChronoMe.Droid.Widgets;
using iChronoMe.Droid.Widgets.ActionButton;
using iChronoMe.Droid.Widgets.Calendar;
using iChronoMe.Droid.Widgets.Clock;
using iChronoMe.Droid.Widgets.Lifetime;
using iChronoMe.Widgets;
using iChronoMe.Widgets.Assistants;

namespace iChronoMe.Droid.GUI.Service
{
    public class MainSettingsFragment : ActivityFragment
    {
        DataBinder binder;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            RootView = (ViewGroup)inflater.Inflate(Resource.Layout.fragment_setting_main, container, false);

            var cfg = new MasterConfigViewModel();

            binder = new DataBinder(Activity, RootView);

            binder.BindViewProperty(Resource.Id.sp_default_timetype, nameof(Spinner.SelectedItemPosition), cfg, nameof(MasterConfigViewModel.AppDefaultTimeType_SpinnerPosition), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cb_calendar_default_timetype, nameof(CheckBox.Checked), cfg, nameof(MasterConfigViewModel.CalendarUseAppDefautlTimeType), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.tv_calendar_timetype, nameof(Spinner.Visibility), cfg, nameof(MasterConfigViewModel.CalendarUseOwnTimeType), BindMode.OneWay);
            binder.BindViewProperty(Resource.Id.sp_calendar_timetype, nameof(Spinner.SelectedItemPosition), cfg, nameof(MasterConfigViewModel.CalendarTimeType_SpinnerPosition), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.sp_calendar_timetype, nameof(Spinner.Visibility), cfg, nameof(MasterConfigViewModel.CalendarUseOwnTimeType), BindMode.OneWay);
            binder.BindViewProperty(Resource.Id.cb_notification_showalways, nameof(CheckBox.Checked), cfg, nameof(MasterConfigViewModel.AlwaysShowForegroundNotification), BindMode.TwoWay);

            binder.BindViewProperty(Resource.Id.cb_send_error_logs, nameof(CheckBox.Checked), cfg, nameof(MasterConfigViewModel.SendErrorLogs), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cb_deny_error_screens, nameof(CheckBox.Visibility), cfg, nameof(MasterConfigViewModel.SendErrorLogs), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cb_deny_error_screens, nameof(CheckBox.Checked), cfg, nameof(MasterConfigViewModel.DenyErrorScreens), BindMode.TwoWay);

            RootView.FindViewById<Spinner>(Resource.Id.sp_default_timetype).Adapter = new TimeTypeAdapter(Activity, true);
            RootView.FindViewById<Spinner>(Resource.Id.sp_calendar_timetype).Adapter = new TimeTypeAdapter(Activity, true);

            RootView.FindViewById<Button>(Resource.Id.btn_notification_config).Click += btnNotifyCfg_Click;

            RootView.FindViewById<Button>(Resource.Id.btn_widgets_config).Click += btnWidgetsConfig_Click;
#if DEBUG
            RootView.FindViewById<Button>(Resource.Id.btn_livewallpaper_config).Click += btnLiveWallpaper_Click;
#else
            RootView.FindViewById<Button>(Resource.Id.btn_livewallpaper_config).Visibility = ViewStates.Gone;
#endif

            RootView.FindViewById<Button>(Resource.Id.btn_clear_cache).Click += btnClearCache_Click;
            RootView.FindViewById<Button>(Resource.Id.btn_system_test).Click += btnSystemTest_Click;

            binder.UserChangedProperty += Binder_UserChangedProperty;

            return RootView;
        }

        public Task<int> UserInputTaskTask { get { return tcsUI == null ? Task.FromResult(-1) : tcsUI.Task; } }
        private TaskCompletionSource<int> tcsUI = null;
        private void btnWidgetsConfig_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                var manager = AppWidgetManager.GetInstance(Context);
                int[] clockS = manager.GetAppWidgetIds(new ComponentName(Context, Java.Lang.Class.FromType(typeof(AnalogClockWidget)).Name));
                int[] calendars = manager.GetAppWidgetIds(new ComponentName(Context, Java.Lang.Class.FromType(typeof(CalendarWidget)).Name));
                int[] buttons = manager.GetAppWidgetIds(new ComponentName(Context, Java.Lang.Class.FromType(typeof(ActionButtonWidget)).Name));
                int[] chronos = manager.GetAppWidgetIds(new ComponentName(Context, Java.Lang.Class.FromType(typeof(LifetimeWidget)).Name));

                if (clockS.Length + calendars.Length + buttons.Length + chronos.Length == 0)
                {
                    Tools.ShowToast(Context, localize.info_no_widgets_found);
                    return;
                }

                var holder = new WidgetConfigHolder();

                var samples = new System.Collections.Generic.List<WidgetCfgSample<WidgetCfg>>();
                foreach (int i in clockS)
                {
                    var cfg = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(i, false);
                    if (cfg == null)
                        continue;
                    samples.Add(new WidgetCfgSample<WidgetCfg>("widget " + i, cfg));
                }
                foreach (int i in calendars)
                {
                    var cfg = holder.GetWidgetCfg<WidgetCfg_Calendar>(i, false);
                    if (cfg == null)
                        continue;
                    samples.Add(new WidgetCfgSample<WidgetCfg>("widget " + i, cfg));
                }
                foreach (int i in buttons)
                {
                    var cfg = holder.GetWidgetCfg<WidgetCfg_ActionButton>(i, false);
                    if (cfg == null)
                        continue;
                    samples.Add(new WidgetCfgSample<WidgetCfg>("widget " + i, cfg));
                }
                foreach (int i in chronos)
                {
                    var cfg = holder.GetWidgetCfg<WidgetCfg_Lifetime>(i, false);
                    if (cfg == null)
                        continue;
                    samples.Add(new WidgetCfgSample<WidgetCfg>("widget " + i, cfg));
                }

                var assi = new WidgetCfgAssistant_Dummy(localize.title_EditWidget, samples);

                var listAdapter = new WidgetPreviewListAdapter<WidgetCfg>(Activity, assi, new Point(400, 300), null, CalendarModelCfgHolder.BaseGregorian, new EventCollection(), new EventCollection());

                tcsUI = new TaskCompletionSource<int>();
                Activity.RunOnUiThread(() =>
                {
                    var dlg = new AlertDialog.Builder(Activity)
                        .SetTitle(assi.Title)
                        .SetNegativeButton("abbrechen", (s, e) => { tcsUI.TrySetResult(-1); })
                        .SetSingleChoiceItems(listAdapter, -1, new AsyncSingleChoiceClickListener(tcsUI))
                        .SetOnCancelListener(new AsyncDialogCancelListener<int>(tcsUI));

                    dlg.Create().Show();
                });

                UserInputTaskTask.Wait();

                if (UserInputTaskTask.Result >= 0)
                {
                    string settingsUri = "";

                    var cfg = assi.Samples[UserInputTaskTask.Result];

                    if (cfg.WidgetConfig is WidgetCfg_ClockAnalog)
                        settingsUri = "me.ichrono.droid/me.ichrono.droid.Widgets.Clock.AnalogClockWidgetConfigActivity";
                    else if (cfg.WidgetConfig is WidgetCfg_Calendar)
                        settingsUri = "me.ichrono.droid/me.ichrono.droid.Widgets.Calendar.CalendarWidgetConfigActivity";
                    else if (cfg.WidgetConfig is WidgetCfg_ActionButton)
                        settingsUri = "me.ichrono.droid/me.ichrono.droid.Widgets.ActionButton.ActionButtonWidgetConfigActivity";
                    else if (cfg.WidgetConfig is WidgetCfg_Lifetime)
                        settingsUri = "me.ichrono.droid/me.ichrono.droid.Widgets.Lifetime.LifetimeWidgetConfigActivity";

                    if (string.IsNullOrEmpty(settingsUri))
                    {
                        Tools.ShowToast(Context, "internal error");
                        return;
                    }

                    Context.StartActivity(MainWidgetBase.GetClickActionIntent(Context, new ClickAction(ClickActionType.OpenSettings), cfg.WidgetConfig.WidgetId, settingsUri));
                }
            });
        }

#if DEBUG
        private void btnLiveWallpaper_Click(object sender, EventArgs e)
        {
            //Intent intent = new Intent(Android.App.WallpaperManager.ActionChangeLiveWallpaper);
            //intent.PutExtra(Android.App.WallpaperManager.ExtraLiveWallpaperComponent, new ComponentName(Context, Java.Lang.Class.FromType(typeof(LiveWallpapers.WallpaperClockService))));
            //Context.StartActivity(intent);
        }
#endif

        private void btnSystemTest_Click(object sender, EventArgs e)
        {
            var view = Activity.LayoutInflater.Inflate(Resource.Layout.fragment_service_system_test, null);
            var listView = view.FindViewById<ListView>(Resource.Id.ListView);
            var adapter = new SystemTestAdapter(Activity);
            listView.Adapter = adapter;

            AlertDialog dialog = new AlertDialog.Builder(Context)
            .SetTitle(Resource.String.progress_systemtest_title)
            .SetView(view)
            .SetPositiveButton(Resource.String.action_send_testlog, (s, e) =>
            {
                adapter.SendTestLog();
            })
            .SetNegativeButton(Resource.String.action_close, (s, e) => { })
            .Create();

            dialog.Show();
            adapter.SetDialog(dialog);
            adapter.StartSystemTest();
        }

        private void btnClearCache_Click(object sender, EventArgs e)
        {
            new AlertDialog.Builder(Context)
                .SetTitle(Resource.String.progress_clearcache_title)
                .SetMessage(Resource.String.progress_clearcache_message)
                .SetPositiveButton(Resource.String.action_continue, (s, e) =>
                {
                    ImageLoader.ClearCache(ImageLoader.filter_clockfaces);
                    ClockHandConfig.ClearCache();

                    try 
                    {
                        db.dbAreaCache.Close();
                        Directory.Delete(sys.PathCache, true); 
                    }
                    catch (Exception ex)
                    {
                        Tools.ShowToast(Context, ex.Message);
                        return;
                    }
                    try
                    {
                        var cfg = AppConfigHolder.LocationConfig;
                        cfg.AreaName = string.Empty;
                        cfg.CountryName = string.Empty;
                        AppConfigHolder.SaveLocationConfig();
                    } catch { }

                    Activity.MoveTaskToBack(true);
                    Process.KillProcess(Process.MyPid());
                    Java.Lang.JavaSystem.Exit(0);
                })
                .SetNegativeButton(Resource.String.action_abort, (s, e) => { })
                .Create().Show();
        }

        private void Binder_UserChangedProperty(object sender, UserChangedPropertyEventArgs e)
        {
            if (e.PropertyName == nameof(MainConfig.AlwaysShowForegroundNotification))
                BackgroundService.RestartService(Context, AppWidgetManager.ActionAppwidgetConfigure);
        }

        private void btnNotifyCfg_Click(object sender, EventArgs e)
        {
            var view = (ViewGroup)LayoutInflater.Inflate(Resource.Layout.fragment_setting_backgroundservice, null);

            var model = new ClockNotificationViewModel(Activity);
            view.FindViewById<Spinner>(Resource.Id.sp_clickaction).Adapter = model.ClickActionTypeAdapter;
            var dlgBinder = model.GetDataBinder(view);

            var dlg = new AlertDialog.Builder(Context)
                .SetTitle("ConfigNotification")
                .SetView(view)
                .SetPositiveButton(Resources.GetString(Resource.String.action_close), (s, e) => { })
                .SetOnDismissListener(new myDlgDismisListener(binder))
                .Create();

            view.FindViewById<Button>(Resource.Id.btn_show_info).Click += model.ShowBackgroundServiceInfo;
            view.FindViewById<Button>(Resource.Id.btn_select_location).Click += model.ShowLocationSelector;
            dlgBinder.Start();
            dlg.Show();
        }

        public override void OnResume()
        {
            base.OnResume();

            binder.Start();
        }

        public override void OnPause()
        {
            base.OnPause();

            binder.Stop();
        }

        class myDlgDismisListener : Java.Lang.Object, IDialogInterfaceOnDismissListener
        {
            DataBinder Binder;

            public myDlgDismisListener(DataBinder binder)
            {
                Binder = binder;
            }

            public void OnDismiss(IDialogInterface dialog)
            {
                Binder.ProcessBindable_PropertyChanged(null, "*");
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                Binder = null;
            }
        }
    }
}