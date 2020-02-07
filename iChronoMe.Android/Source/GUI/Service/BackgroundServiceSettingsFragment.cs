using System;
using System.Threading.Tasks;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Droid.GUI;
using iChronoMe.Droid.GUI.Dialogs;

using Xamarin.Essentials;

namespace iChronoMe.Droid.GUI.Service
{
    public class BackgroundServiceSettingsFragment : ActivityFragment
    {
        private bool bIsInfoActivity = false;

        public BackgroundServiceSettingsFragment(bool isInfoActivity = false)
        {
            bIsInfoActivity = isInfoActivity;
        }


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            RootView = (ViewGroup)inflater.Inflate(Resource.Layout.fragment_setting_backgroundservice, container, false);

            return RootView;
        }

        Button btn_ShowInfo, btn_SelectLocation;
        CheckBox cbShowAlways;
        Spinner spLocationtype, spClickAction;
        WidgetConfigHolder holder;

        public override void OnResume()
        {
            base.OnResume();

            btn_ShowInfo = RootView.FindViewById<Button>(Resource.Id.btn_show_info);
            btn_ShowInfo.Visibility = (bIsInfoActivity && Build.VERSION.SdkInt >= BuildVersionCodes.NMr1) ? ViewStates.Visible : ViewStates.Gone;
            cbShowAlways = RootView.FindViewById<CheckBox>(Resource.Id.cb_showalways);
            spLocationtype = RootView.FindViewById<Spinner>(Resource.Id.sp_locationtype);
            btn_SelectLocation = RootView.FindViewById<Button>(Resource.Id.btn_select_location);
            spClickAction = RootView.FindViewById<Spinner>(Resource.Id.sp_clickaction);

            var cfg = AppConfigHolder.MainConfig;
            holder = new WidgetConfigHolder();
            var clockcfg = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(-101, false);

            cbShowAlways.Checked = cfg.AlwaysShowForegroundNotification;

            spLocationtype.SetSelection(clockcfg != null && clockcfg.PositionType == WidgetCfgPositionType.StaticPosition ? 1 : 0);

            btn_SelectLocation.Enabled = spLocationtype.SelectedItemPosition == 1;

            spClickAction.SetSelection(-1);

            Task.Factory.StartNew(() =>
            {
                Task.Delay(100).Wait();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    btn_ShowInfo.Click += Btn_ShowInfo_Click;
                    cbShowAlways.CheckedChange += CbShowAlways_CheckedChange;
                    spLocationtype.ItemSelected += SpLocationtype_ItemSelected;
                    btn_SelectLocation.Click += Btn_SelectLocation_Click;
                    spClickAction.ItemSelected += SpClickAction_ItemSelected;
                });
            });
        }

        private void Btn_ShowInfo_Click(object sender, EventArgs e)
        {
            new AlertDialog.Builder(Context)
                .SetMessage(Resources.GetString(Resource.String.backgroundservice_info_explenation))
                .SetPositiveButton(Resources.GetString(Resource.String.action_ok), (s, e) =>
                {

                })
                .SetNegativeButton(Resources.GetString(Resource.String.action_block), (s, e) =>
                {
                    Intent intent = new Intent();


                    if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                    {
                        //for Android 5-7
                        intent.SetAction("android.settings.APP_NOTIFICATION_SETTINGS");
                        intent.PutExtra("app_package", Context.PackageName);
                        intent.PutExtra("app_uid", Context.ApplicationInfo.Uid);
                    }
                    else
                    {
                        // for Android 8 and above
                        intent.SetAction("android.settings.CHANNEL_NOTIFICATION_SETTINGS");
                        intent.PutExtra("android.provider.extra.APP_PACKAGE", Context.PackageName);
                        intent.PutExtra("android.provider.extra.CHANNEL_ID", "widget_service");
                    }

                    Context.StartActivity(intent);
                })
                .Create().Show();
        }

        private void CbShowAlways_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            AppConfigHolder.MainConfig.AlwaysShowForegroundNotification = e.IsChecked;
            AppConfigHolder.SaveMainConfig();

            BackgroundService.RestartService(Context, AppWidgetManager.ActionAppwidgetUpdate);
        }

        private void SpLocationtype_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var clockcfg = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(-101);
            if (e.Position == 1)
            {
                Btn_SelectLocation_Click(sender, e);
                return;
            }

            clockcfg.PositionType = WidgetCfgPositionType.LivePosition;
            holder.SetWidgetCfg(clockcfg);

            BackgroundService.RestartService(Context, AppWidgetManager.ActionAppwidgetUpdate);
            btn_SelectLocation.Enabled = clockcfg.PositionType == WidgetCfgPositionType.LivePosition;
        }

        private async void Btn_SelectLocation_Click(object sender, EventArgs e)
        {
            var pos = await LocationPickerDialog.SelectLocation((BaseActivity)Context);
            if (pos == null)
            {
                spLocationtype.SetSelection(0);
                return;
            }
            var clockcfg = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(-101);
            clockcfg.PositionType = WidgetCfgPositionType.StaticPosition;
            clockcfg.WidgetTitle = pos.Title;
            clockcfg.Latitude = pos.Latitude;
            clockcfg.Longitude = pos.Longitude;
            holder.SetWidgetCfg(clockcfg);

            BackgroundService.RestartService(Context, AppWidgetManager.ActionAppwidgetUpdate);
        }

        private void SpClickAction_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var clockcfg = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(-101);
            clockcfg.ClickAction = WidgetCfgClickAction.OpenSettings;
            holder.SetWidgetCfg(clockcfg);

            BackgroundService.RestartService(Context, AppWidgetManager.ActionAppwidgetUpdate);
        }

        public override void OnPause()
        {
            base.OnPause();
        }

    }
}