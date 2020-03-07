﻿using System;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Core.DataBinding;
using iChronoMe.Droid.Adapters;
using iChronoMe.Droid.GUI.Dialogs;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.ViewModels
{
    public class ClockNotificationViewModel : BaseObservable
    {
        Activity mContext;
        bool IsBackgroundServiceInfoActivity;
        private WidgetConfigHolder holder;
        private MainConfig main { get => AppConfigHolder.MainConfig; }
        private void saveMain() { AppConfigHolder.SaveMainConfig(); }

        public ClickActionTypeAdapter ClickActionTypeAdapter { get; }

        public ClockNotificationViewModel(Activity context, bool bIsBackgroundServiceInfoActivity = false)
        {
            mContext = context;
            IsBackgroundServiceInfoActivity = bIsBackgroundServiceInfoActivity;
            ClickActionTypeAdapter = new ClickActionTypeAdapter(context);
            holder = new WidgetConfigHolder();
            clock = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(-101, false);
        }

        public DataBinder GetDataBinder(ViewGroup rootView)
        {
            var binder = new DataBinder(mContext, rootView);

            binder.BindViewProperty(Resource.Id.btn_show_info, nameof(Button.Visibility), this, nameof(AllowShowBackgroundServiceInfo), BindMode.OneWay);
            binder.BindViewProperty(Resource.Id.cb_showalways, nameof(CheckBox.Checked), this, nameof(AlwaysShowForegroundNotification), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.sp_locationtype, nameof(Spinner.SelectedItemPosition), this, nameof(Locationtype_SpinnerPosition), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.btn_select_location, nameof(Button.Visibility), this, nameof(AllowSelectLocaion), BindMode.OneWay);
            binder.BindViewProperty(Resource.Id.sp_clickaction, nameof(Spinner.SelectedItemPosition), this, nameof(ClickAction_SpinnerPosition), BindMode.TwoWay);

            return binder;
        }

        private WidgetCfg_ClockAnalog clock = null;
        public bool AllowShowBackgroundServiceInfo { get => IsBackgroundServiceInfoActivity && Build.VERSION.SdkInt >= BuildVersionCodes.NMr1; }

        public bool AlwaysShowForegroundNotification
        {
            get => main.AlwaysShowForegroundNotification;
            set
            {
                main.AlwaysShowForegroundNotification = value;
                saveMain();
                OnPropertyChanged("*");
                BackgroundService.RestartService(mContext, AppWidgetManager.ActionAppwidgetOptionsChanged);
            }
        }

        public WidgetCfgPositionType Locationtype
        {
            get => clock != null ? clock.PositionType : WidgetCfgPositionType.LivePosition;
            set
            {
                clock = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(-101);
                if (value == WidgetCfgPositionType.StaticPosition)
                {
                    ShowLocationSelector(this, null);
                    return;
                }

                clock.PositionType = WidgetCfgPositionType.LivePosition;
                holder.SetWidgetCfg(clock);

                OnPropertyChanged("*");
                BackgroundService.RestartService(mContext, AppWidgetManager.ActionAppwidgetOptionsChanged);
            }
        }

        public bool AllowSelectLocaion { get => clock != null && clock.PositionType == WidgetCfgPositionType.StaticPosition; }

        public int Locationtype_SpinnerPosition
        {
            get => Locationtype == WidgetCfgPositionType.StaticPosition ? 1 : 0;
            set
            {
                if (value == 1)
                    Locationtype = WidgetCfgPositionType.StaticPosition;
                else
                    Locationtype = WidgetCfgPositionType.LivePosition;
            }
        }

        public async void ShowLocationSelector(object sender, EventArgs e)
        {
            var pos = await LocationPickerDialog.SelectLocation((BaseActivity)mContext);
            if (pos == null)
            {
                if (clock == null)
                {
                    OnPropertyChanged("*");
                }
                else
                    Locationtype = WidgetCfgPositionType.LivePosition;
                return;
            }
            clock = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(-101);
            clock.PositionType = WidgetCfgPositionType.StaticPosition;
            clock.WidgetTitle = pos.Title;
            clock.Latitude = pos.Latitude;
            clock.Longitude = pos.Longitude;
            holder.SetWidgetCfg(clock);

            OnPropertyChanged("*");
            BackgroundService.RestartService(mContext, AppWidgetManager.ActionAppwidgetUpdate);
        }

        public void ShowBackgroundServiceInfo(object sender, EventArgs e)
        {
            new AlertDialog.Builder(mContext)
                .SetMessage(mContext.Resources.GetString(Resource.String.backgroundservice_info_explenation))
                .SetPositiveButton(mContext.Resources.GetString(Resource.String.action_ok), (s, e) =>
                {

                })
                .SetNegativeButton(mContext.Resources.GetString(Resource.String.action_block), (s, e) =>
                {
                    Intent intent = new Intent();


                    if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                    {
                        //for Android 5-7
                        intent.SetAction("android.settings.APP_NOTIFICATION_SETTINGS");
                        intent.PutExtra("app_package", mContext.PackageName);
                        intent.PutExtra("app_uid", mContext.ApplicationInfo.Uid);
                    }
                    else
                    {
                        // for Android 8 and above
                        intent.SetAction("android.settings.CHANNEL_NOTIFICATION_SETTINGS");
                        intent.PutExtra("android.provider.extra.APP_PACKAGE", mContext.PackageName);
                        intent.PutExtra("android.provider.extra.CHANNEL_ID", "widget_service");
                    }

                    mContext.StartActivity(intent);
                })
                .Create().Show();
        }

        public int ClickAction_SpinnerPosition
        {
            get => ClickActionTypeAdapter.GetPos(clock?.ClickAction.Type ?? ClickActionType.OpenSettings);
            set
            {
                clock = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(-101);

                clock.ClickAction = new ClickAction(ClickActionTypeAdapter.GetValue(value));

                holder.SetWidgetCfg(clock);
                OnPropertyChanged("*");
                BackgroundService.RestartService(mContext, AppWidgetManager.ActionAppwidgetOptionsChanged);
            }
        }
    }
}