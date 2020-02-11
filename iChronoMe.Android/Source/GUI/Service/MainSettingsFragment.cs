using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.DataBinding;
using iChronoMe.Core.ViewModels;
using iChronoMe.Droid.Adapters;
using iChronoMe.Droid.GUI;
using iChronoMe.Droid.Source.Adapters;
using iChronoMe.Droid.Source.GUI.Dialogs;
using iChronoMe.Droid.Source.ViewModels;

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

            RootView.FindViewById<Spinner>(Resource.Id.sp_default_timetype).Adapter = new TimeTypeAdapter(Activity, true);
            RootView.FindViewById<Spinner>(Resource.Id.sp_calendar_timetype).Adapter = new TimeTypeAdapter(Activity, true);
            
            RootView.FindViewById<Button>(Resource.Id.btn_notification_config).Click += btnNotifyCfg_Click;
            RootView.FindViewById<Button>(Resource.Id.btn_clear_cache).Click += btnClearCache_Click;
            RootView.FindViewById<Button>(Resource.Id.btn_system_test).Click += btnSystemTest_Click;

            binder.UserChangedProperty += Binder_UserChangedProperty;

            return RootView;
        }

        private void btnSystemTest_Click(object sender, EventArgs e)
        {
            var view = Activity.LayoutInflater.Inflate(Resource.Layout.fragment_service_system_test, null);
            var listView = view.FindViewById<ListView>(Resource.Id.ListView);
            var adapter = new SystemTestAdapter(Activity);
            listView.Adapter = adapter;

            AlertDialog dialog = new AlertDialog.Builder(Context)
            .SetTitle("System-Test")
            .SetView(view)
            .SetNegativeButton(Resource.String.action_close, (senderAlert, args) =>
            {
            })
            .Create();
            dialog.Show();
            adapter.StartSystemTest();
        }

        private void btnClearCache_Click(object sender, EventArgs e)
        {
            new AlertDialog.Builder(Context)
                .SetTitle("clear cache?")
                .SetMessage("you will have to restart iChronome continue")
                .SetPositiveButton(Resource.String.action_continue, (s, e) =>
                {
                    db.dbAreaCache.Close();
                    try { Directory.Delete(sys.PathCache, true); } catch { };
                    Activity.MoveTaskToBack(true);
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
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