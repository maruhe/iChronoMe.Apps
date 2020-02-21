using System;
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

            cEventId = Intent.HasExtra("EventId") ? Intent.GetStringExtra("EventId") : "";

            mModel = new CalendarEventPopupViewModel(cEventId, this);
            mBinder = new DataBinder(this, FindViewById<ViewGroup>(Resource.Id.rootView));

            mBinder.BindViewProperty(Resource.Id.Subject, nameof(TextView.Text), mModel, nameof(CalendarEventPopupViewModel.Title), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.StartDate, nameof(TextView.Text), mModel, nameof(CalendarEventPopupViewModel.StartDate), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.StartTime, nameof(TextView.Text), mModel, nameof(CalendarEventPopupViewModel.StartTime), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.EndDate, nameof(TextView.Text), mModel, nameof(CalendarEventPopupViewModel.EndDate), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.EndTime, nameof(TextView.Text), mModel, nameof(CalendarEventPopupViewModel.EndTime), BindMode.TwoWay);
        }

        private void DateStart_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                ShowDateDialog(nameof(CalendarEventPopupViewModel.DisplayStart));
        }

        private void DateStart_Click(object sender, EventArgs e)
        {
            ShowDateDialog(nameof(CalendarEventPopupViewModel.DisplayStart));
        }

        private void DateEnd_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                ShowDateDialog(nameof(CalendarEventPopupViewModel.DisplayEnd));
        }

        private void DateEnd_Click(object sender, EventArgs e)
        {
            ShowDateDialog(nameof(CalendarEventPopupViewModel.DisplayEnd));
        }

        private void TimeStart_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                ShowTimeDialog(nameof(CalendarEventPopupViewModel.DisplayStart));
        }

        private void TimeStart_Click(object sender, EventArgs e)
        {
            ShowTimeDialog(nameof(CalendarEventPopupViewModel.DisplayStart));
        }

        private void TimeEnd_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                ShowTimeDialog(nameof(CalendarEventPopupViewModel.DisplayEnd));
        }

        private void TimeEnd_Click(object sender, EventArgs e)
        {
            ShowTimeDialog(nameof(CalendarEventPopupViewModel.DisplayEnd));
        }

        public void ShowDateDialog(string property)
        {
            var prop = mModel.GetType().GetProperty(property);
            var date = (DateTime)prop.GetValue(mModel);
            var dlg = new DatePickerDialog(this, DateDlgChaged, date.Year, date.Month, date.Day);
            dlg.Show();

        }
        public void ShowTimeDialog(string property)
        {
            var prop = mModel.GetType().GetProperty(property);
            var date = (DateTime)prop.GetValue(mModel);
            var dlg = new TimePickerDialog(this, TimeDlgChanged, date.Hour, date.Minute, CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.StartsWith("HH"));
            dlg.Show();

        }

        protected void DateDlgChaged(object sender, DateSetEventArgs e)
        {
            Tools.ShowToast(this, e.Date.ToLongDateString(), true);
        }

        protected void TimeDlgChanged(object sender, TimeSetEventArgs e)
        {
            Tools.ShowToast(this, e.HourOfDay + ":" + e.Minute, true);
        }

        protected override void OnResume()
        {
            base.OnResume();
            mBinder.Start();
        }

        protected override void OnPause()
        {
            mBinder.Stop();
            base.OnPause();
        }
    }
}