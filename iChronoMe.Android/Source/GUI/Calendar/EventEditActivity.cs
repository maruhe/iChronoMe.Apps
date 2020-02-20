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

namespace iChronoMe.Droid.GUI.Calendar
{

    [Activity(Label = "EventEditActivity", Name = "me.ichrono.droid.GUI.Calendar.EventEditActivity", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "")]
    public class EventEditActivity : BaseActivity
    {
        LinearLayout llStart, llEnd;
        DatePicker dateStart, dateEnd;
        TimePicker timeStart, timeEnd;
        CalendarEventPopupViewModel mViewModel;
        DataBinder mBinder;
        LinearLayout llBottom;
        string cEventId;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_calendar_event);

            llBottom = FindViewById<LinearLayout>(Resource.Id.llBottom);

            llBottom.FindViewById<TabLayout>(Resource.Id.tabLayout).TabSelected += StartEnd_TabSelected;
            llStart = llBottom.FindViewById<LinearLayout>(Resource.Id.llStart);
            llEnd = llBottom.FindViewById<LinearLayout>(Resource.Id.llEnd);
            dateStart = llBottom.FindViewById<DatePicker>(Resource.Id.datePickerStart);
            timeStart = llBottom.FindViewById<TimePicker>(Resource.Id.timePickerStart);
            dateEnd = llBottom.FindViewById<DatePicker>(Resource.Id.datePickerEnd);
            timeEnd = llBottom.FindViewById<TimePicker>(Resource.Id.timePickerEnd);

            if (CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.StartsWith("HH"))
            {
                timeStart.SetIs24HourView(Java.Lang.Boolean.True);
                timeEnd.SetIs24HourView(Java.Lang.Boolean.True);
            }

            cEventId = Intent.HasExtra("EventId") ? Intent.GetStringExtra("EventId") : "";

            mViewModel = new CalendarEventPopupViewModel(cEventId, this);
            mBinder = new DataBinder(this, FindViewById<ViewGroup>(Resource.Id.rootView));

            mBinder.BindViewProperty(Resource.Id.title, nameof(TextView.Text), mViewModel, nameof(CalendarEventPopupViewModel.Title), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.StartDate, nameof(TextView.Text), mViewModel, nameof(CalendarEventPopupViewModel.StartDate), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.StartTime, nameof(TextView.Text), mViewModel, nameof(CalendarEventPopupViewModel.StartTime), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.EndDate, nameof(TextView.Text), mViewModel, nameof(CalendarEventPopupViewModel.EndDate), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.EndTime, nameof(TextView.Text), mViewModel, nameof(CalendarEventPopupViewModel.EndTime), BindMode.TwoWay);

            FindViewById<TableRow>(Resource.Id.row_start).Click += TableRow_Click;
        }

        private void TableRow_Click(object sender, EventArgs e)
        {
            Tools.ShowToast(this, "klick");
            llBottom.Visibility = ViewStates.Visible;
        }

        private void StartEnd_TabSelected(object sender, TabLayout.TabSelectedEventArgs e)
        {
            if (e.Tab.Position == 0)
            {
                llEnd.Visibility = ViewStates.Gone;
                llStart.Visibility = ViewStates.Visible;
            }
            else
            {
                llStart.Visibility = ViewStates.Gone;
                llEnd.Visibility = ViewStates.Visible;
            }
        }
    }
}