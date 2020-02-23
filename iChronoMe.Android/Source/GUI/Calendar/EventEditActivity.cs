﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.DataBinding;
using iChronoMe.Core.ViewModels;
using iChronoMe.Droid.Adapters;
using static Android.App.DatePickerDialog;
using static Android.App.TimePickerDialog;

namespace iChronoMe.Droid.GUI.Calendar
{

    [Activity(Label = "EventEditActivity", Name = "me.ichrono.droid.GUI.Calendar.EventEditActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "")]
    public class EventEditActivity : BaseActivity
    {
        TextView dateStart, dateEnd;
        TextView timeStart, timeEnd;
        CalendarEventPopupViewModel mModel;
        DataBinder mBinder;
        string cEventId;
        TimeTypeAdapter ttAdapter = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            LoadAppTheme();
            SetContentView(Resource.Layout.activity_calendar_event);
            Toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(Toolbar);

            dateStart = FindViewById<TextView>(Resource.Id.StartDate);
            timeStart = FindViewById<TextView>(Resource.Id.StartTime);
            dateEnd = FindViewById<TextView>(Resource.Id.EndDate);
            timeEnd = FindViewById<TextView>(Resource.Id.EndTime);

            dateStart.FocusChange += DateStart_FocusChange;
            dateStart.Click += DateStart_Click;
            timeStart.FocusChange += TimeStart_FocusChange;
            timeStart.Click += TimeStart_Click;
            dateEnd.FocusChange += DateEnd_FocusChange;
            dateEnd.Click += DateEnd_Click;
            timeEnd.FocusChange += TimeEnd_FocusChange;
            timeEnd.Click += TimeEnd_Click;
        }

        protected override void OnResume()
        {
            base.OnResume();
    
            cEventId = Intent.HasExtra("EventId") ? Intent.GetStringExtra("EventId") : "";

            mModel = new CalendarEventPopupViewModel(cEventId, this);
            mBinder = new DataBinder(this, FindViewById<ViewGroup>(Resource.Id.rootView));
            ttAdapter = new TimeTypeAdapter(this, true);
            ttAdapter.LocationTimeHolder = mModel.LocationTimeHolder;
            mModel.PropertyChanged += MModel_PropertyChanged;

            mBinder.BindViewProperty(Resource.Id.Subject, nameof(EditText.Text), mModel, nameof(CalendarEventPopupViewModel.Title), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.StartDate, nameof(EditText.Text), mModel, nameof(CalendarEventPopupViewModel.DisplayStartDate), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.StartTime, nameof(EditText.Text), mModel, nameof(CalendarEventPopupViewModel.DisplayStartTime), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.EndDate, nameof(EditText.Text), mModel, nameof(CalendarEventPopupViewModel.DisplayEndDate), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.EndTime, nameof(EditText.Text), mModel, nameof(CalendarEventPopupViewModel.DisplayEndTime), BindMode.TwoWay);

            mBinder.BindViewProperty(Resource.Id.StartHelper, nameof(TextView.Text), mModel, nameof(CalendarEventPopupViewModel.StartTimeHelper));
            mBinder.BindViewProperty(Resource.Id.EndHelper, nameof(TextView.Text), mModel, nameof(CalendarEventPopupViewModel.EndTimeHelper));

            mBinder.BindViewProperty(Resource.Id.AllDay, nameof(Switch.Checked), mModel, nameof(CalendarEventPopupViewModel.AllDay), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.StartTime, nameof(EditText.Visibility), mModel, nameof(CalendarEventPopupViewModel.NotAllDay));
            mBinder.BindViewProperty(Resource.Id.EndTime, nameof(EditText.Visibility), mModel, nameof(CalendarEventPopupViewModel.NotAllDay));
            
            mBinder.BindViewProperty(Resource.Id.spTimeType, nameof(Spinner.SelectedItemPosition), mModel, nameof(CalendarEventPopupViewModel.TimeTypeSpinnerPos), BindMode.TwoWay);
            FindViewById<Spinner>(Resource.Id.spTimeType).Adapter = ttAdapter;
            
            mBinder.BindViewProperty(Resource.Id.location, nameof(EditText.Text), mModel, nameof(CalendarEventPopupViewModel.Location), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.location_progress, nameof(View.Visibility), mModel, nameof(CalendarEventPopupViewModel.IsSearchingForLocation));
            mBinder.BindViewProperty(Resource.Id.location_helper, nameof(TextView.Text), mModel, nameof(CalendarEventPopupViewModel.LocationHelper));

            mBinder.BindViewProperty(Resource.Id.description, nameof(EditText.Text), mModel, nameof(CalendarEventPopupViewModel.Description), BindMode.TwoWay);

            mBinder.BindViewProperty(Resource.Id.row_start_helper, nameof(View.Visibility), mModel, nameof(CalendarEventPopupViewModel.ShowTimeHelpers));
            mBinder.BindViewProperty(Resource.Id.row_end_helper, nameof(View.Visibility), mModel, nameof(CalendarEventPopupViewModel.ShowTimeHelpers));
            mBinder.BindViewProperty(Resource.Id.row_location_helper, nameof(View.Visibility), mModel, nameof(CalendarEventPopupViewModel.ShowLocationHelper));

            mBinder.Start();
        }

        private void MModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (nameof(CalendarEventPopupViewModel.Location).Equals(e.PropertyName))
                ttAdapter.LocationTimeHolder = mModel.LocationTimeHolder;
        }

        private void DateStart_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                ShowDateDialog(nameof(CalendarEventPopupViewModel.DisplayStartDate));
        }

        private void DateStart_Click(object sender, EventArgs e)
        {
            ShowDateDialog(nameof(CalendarEventPopupViewModel.DisplayStartDate));
        }

        private void DateEnd_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                ShowDateDialog(nameof(CalendarEventPopupViewModel.DisplayEndDate));
        }

        private void DateEnd_Click(object sender, EventArgs e)
        {
            ShowDateDialog(nameof(CalendarEventPopupViewModel.DisplayEndDate));
        }

        private void TimeStart_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                ShowTimeDialog(nameof(CalendarEventPopupViewModel.DisplayStartTime));
        }

        private void TimeStart_Click(object sender, EventArgs e)
        {
            ShowTimeDialog(nameof(CalendarEventPopupViewModel.DisplayStartTime));
        }

        private void TimeEnd_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                ShowTimeDialog(nameof(CalendarEventPopupViewModel.DisplayEndTime));
        }

        private void TimeEnd_Click(object sender, EventArgs e)
        {
            ShowTimeDialog(nameof(CalendarEventPopupViewModel.DisplayEndTime));
        }

        string dateDlgProp = string.Empty;
        string timeDlgProp = string.Empty;
        public void ShowDateDialog(string property)
        {
            dateDlgProp = property;
            var prop = mModel.GetType().GetProperty(property);
            var date = (DateTime)prop.GetValue(mModel);
            var dlg = new DatePickerDialog(this, DateDlgChaged, date.Year, date.Month, date.Day);
            dlg.Show();
        }

        public void ShowTimeDialog(string property)
        {
            timeDlgProp = property;
            var prop = mModel.GetType().GetProperty(property);
            var time = (TimeSpan)prop.GetValue(mModel);
            var dlg = new TimePickerDialog(this, TimeDlgChanged, time.Hours, time.Minutes, CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.StartsWith("HH"));
            dlg.Show();
        }

        protected void DateDlgChaged(object sender, DateSetEventArgs e)
        {
            Tools.ShowToast(this, dateDlgProp + ": " + e.Date.ToLongDateString(), true);

            var prop = mModel.GetType().GetProperty(dateDlgProp);
            prop.SetValue(mModel, e.Date);

            dateDlgProp = string.Empty;
        }

        protected void TimeDlgChanged(object sender, TimeSetEventArgs e)
        {
            Tools.ShowToast(this, timeDlgProp + ": " + e.HourOfDay + ":" + e.Minute, true);

            var prop = mModel.GetType().GetProperty(timeDlgProp);
            prop.SetValue(mModel, new TimeSpan(e.HourOfDay, e.Minute, 0));

            timeDlgProp = string.Empty;
        }
        
        protected override void OnPause()
        {
            mBinder.Stop();
            base.OnPause();
        }
    }
}