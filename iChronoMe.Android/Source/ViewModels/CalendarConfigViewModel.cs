using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Syncfusion.Schedule;
using Com.Syncfusion.Schedule.Enums;
using iChronoMe.Core.Classes;
using iChronoMe.Core.DataBinding;
using iChronoMe.Droid.Source.Adapters;

namespace iChronoMe.Droid.Source.ViewModels
{
    public class CalendarConfigViewModel : BaseObservable
    {
        Activity mContext;
        SfSchedule Schedule;
        private DataBinder binder;

        private MainConfig main { get => AppConfigHolder.MainConfig; }
        private CalendarViewConfig cal { get => AppConfigHolder.CalendarViewConfig; }

        private void saveMain() { AppConfigHolder.SaveMainConfig(); }
        private void saveCal() { AppConfigHolder.SaveCalendarViewConfig(); }

        public CalendarConfigViewModel(Activity context, SfSchedule schedule)
        {
            mContext = context;
            Schedule = schedule;
        }

        public DataBinder GetDataBinder(ViewGroup rootView)
        {
            binder?.Stop();
            binder = new DataBinder(mContext, rootView);

            binder.BindViewProperty(Resource.Id.cfg_timeline, nameof(View.Visibility), this, nameof(IsTimeLineView), BindMode.OneWay);
            binder.BindViewProperty(Resource.Id.cfg_dayview, nameof(View.Visibility), this, nameof(IsDayView), BindMode.OneWay);
            binder.BindViewProperty(Resource.Id.cfg_weekview, nameof(View.Visibility), this, nameof(IsWeekView), BindMode.OneWay);
            binder.BindViewProperty(Resource.Id.cfg_workweek, nameof(View.Visibility), this, nameof(IsWorkWeek), BindMode.OneWay);
            binder.BindViewProperty(Resource.Id.cfg_monthview, nameof(View.Visibility), this, nameof(IsMonthView), BindMode.OneWay);

            var hours = new HourAdapter(mContext);
            binder.BindViewProperty(Resource.Id.cfg_timeline_sp_hour_start, nameof(Spinner.SelectedItemPosition), this, nameof(TimeLineHourStart), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_timeline_sp_hour_end, nameof(Spinner.SelectedItemPosition), this, nameof(TimeLineHourEnd), BindMode.TwoWay);
            rootView.FindViewById<Spinner>(Resource.Id.cfg_timeline_sp_hour_start).Adapter = hours;
            rootView.FindViewById<Spinner>(Resource.Id.cfg_timeline_sp_hour_end).Adapter = hours;
            binder.BindViewProperty(Resource.Id.cfg_timeline_sp_days_count, nameof(Spinner.SelectedItemPosition), this, nameof(TimeLineDaysCount), BindMode.TwoWay);

            binder.BindViewProperty(Resource.Id.cfg_dayview_sp_hour_start, nameof(Spinner.SelectedItemPosition), this, nameof(DayViewHourStart), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_dayview_sp_hour_end, nameof(Spinner.SelectedItemPosition), this, nameof(DayViewHourEnd), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_dayview_sp_workhour_start, nameof(Spinner.SelectedItemPosition), this, nameof(DayViewWorkHourStart), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_dayview_sp_workhour_end, nameof(Spinner.SelectedItemPosition), this, nameof(DayViewWorkHourEnd), BindMode.TwoWay);
            rootView.FindViewById<Spinner>(Resource.Id.cfg_dayview_sp_hour_start).Adapter = hours;
            rootView.FindViewById<Spinner>(Resource.Id.cfg_dayview_sp_hour_end).Adapter = hours;
            rootView.FindViewById<Spinner>(Resource.Id.cfg_dayview_sp_workhour_start).Adapter = hours;
            rootView.FindViewById<Spinner>(Resource.Id.cfg_dayview_sp_workhour_end).Adapter = hours;
            binder.BindViewProperty(Resource.Id.cfg_dayview_cb_show_allday, nameof(CheckBox.Checked), this, nameof(DayViewShowAllDay), BindMode.TwoWay);

            binder.BindViewProperty(Resource.Id.cfg_weekview_sp_hour_start, nameof(Spinner.SelectedItemPosition), this, nameof(WeekViewHourStart), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_weekview_sp_hour_end, nameof(Spinner.SelectedItemPosition), this, nameof(WeekViewHourEnd), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_weekview_sp_workhour_start, nameof(Spinner.SelectedItemPosition), this, nameof(WeekViewWorkHourStart), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_weekview_sp_workhour_end, nameof(Spinner.SelectedItemPosition), this, nameof(WeekViewWorkHourEnd), BindMode.TwoWay);
            rootView.FindViewById<Spinner>(Resource.Id.cfg_weekview_sp_hour_start).Adapter = hours;
            rootView.FindViewById<Spinner>(Resource.Id.cfg_weekview_sp_hour_end).Adapter = hours;
            rootView.FindViewById<Spinner>(Resource.Id.cfg_weekview_sp_workhour_start).Adapter = hours;
            rootView.FindViewById<Spinner>(Resource.Id.cfg_weekview_sp_workhour_end).Adapter = hours;
            binder.BindViewProperty(Resource.Id.cfg_weekview_cb_show_allday, nameof(CheckBox.Checked), this, nameof(WeekViewShowAllDay), BindMode.TwoWay);

            binder.BindViewProperty(Resource.Id.cfg_workweek_sp_hour_start, nameof(Spinner.SelectedItemPosition), this, nameof(WorkWeekHourStart), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_workweek_sp_hour_end, nameof(Spinner.SelectedItemPosition), this, nameof(WorkWeekHourEnd), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_workweek_sp_workhour_start, nameof(Spinner.SelectedItemPosition), this, nameof(WorkWeekWorkHourStart), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_workweek_sp_workhour_end, nameof(Spinner.SelectedItemPosition), this, nameof(WorkWeekWorkHourEnd), BindMode.TwoWay);
            rootView.FindViewById<Spinner>(Resource.Id.cfg_workweek_sp_hour_start).Adapter = hours;
            rootView.FindViewById<Spinner>(Resource.Id.cfg_workweek_sp_hour_end).Adapter = hours;
            rootView.FindViewById<Spinner>(Resource.Id.cfg_workweek_sp_workhour_start).Adapter = hours;
            rootView.FindViewById<Spinner>(Resource.Id.cfg_workweek_sp_workhour_end).Adapter = hours;
            binder.BindViewProperty(Resource.Id.cfg_workweek_cb_show_allday, nameof(CheckBox.Checked), this, nameof(WorkWeekShowAllDay), BindMode.TwoWay);

            binder.BindViewProperty(Resource.Id.cfg_monthview_sp_appointmentview, nameof(Spinner.SelectedItemPosition), this, nameof(MonthViewAgendaType_SpinnerPosition), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_monthview_sp_appointmentmode, nameof(Spinner.SelectedItemPosition), this, nameof(MonthViewAppointmentDisplayMode_SpinnerPosition), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_monthview_sp_appointmentcount, nameof(Spinner.SelectedItemPosition), this, nameof(MonthViewAppointmentIndicatorCount_SpinnerPosition), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_monthview_sp_navigation_direction, nameof(Spinner.SelectedItemPosition), this, nameof(MonthViewNavigationDirection), BindMode.TwoWay);
            binder.BindViewProperty(Resource.Id.cfg_monthview_cb_show_weeknumbers, nameof(CheckBox.Checked), this, nameof(MonthViewShowWeekNumber), BindMode.TwoWay);

            return binder;
        }

        public bool ShowAllCalendars
        {
            get => cal.ShowAllCalendars;
            set
            {
                cal.ShowAllCalendars = value;
                saveCal();
                OnPropertyChanged();
            }
        }

        public List<string> HideCalendars { get => cal.HideCalendars; }

        public int DefaultViewType { 
            get => cal.DefaultViewType;
            set
            {
                cal.DefaultViewType = value;
                saveCal();
                OnPropertyChanged();
            }
        }

        #region TimeLine

        public int TimeLineHourStart
        {
            get => (int)Schedule.TimelineViewSettings.StartHour;
            set
            {
                var x = HoursStartEndCheck(value, Schedule.TimelineViewSettings.EndHour);
                Schedule.TimelineViewSettings.EndHour = 24; //Avoid exception
                Schedule.TimelineViewSettings.StartHour = x.Item1;
                Schedule.TimelineViewSettings.EndHour = x.Item2;
                //Schedule.TimelineViewSettings.DaysCount = 7;
                OnPropertyChanged(nameof(TimeLineHourStart));
                OnPropertyChanged(nameof(TimeLineHourEnd));

            }
        }
        public int TimeLineHourEnd
        {
            get => (int)Schedule.TimelineViewSettings.EndHour;
            set
            {
                var x = HoursStartEndCheck(Schedule.TimelineViewSettings.StartHour, value);
                Schedule.TimelineViewSettings.EndHour = 24; //Avoid exception
                Schedule.TimelineViewSettings.StartHour = x.Item1;
                Schedule.TimelineViewSettings.EndHour = x.Item2;
                OnPropertyChanged(nameof(TimeLineHourStart));
                OnPropertyChanged(nameof(TimeLineHourEnd));                
            }
        }

        public int TimeLineDaysCount
        {
            get => Schedule.TimelineViewSettings.DaysCount;
            set
            {
                Schedule.TimelineViewSettings.DaysCount = value;
                OnPropertyChanged();
            }
        }

        public ICollection<int> TimelineNonWorkingDays
        {
            get => new List<int>(Schedule.TimelineViewSettings.NonWorkingDays);
            set{
                Schedule.TimelineViewSettings.NonWorkingDays = new System.Collections.ObjectModel.ObservableCollection<int>(value);
                OnPropertyChanged();
            }
        }

        #endregion

        #region DayView
        public int DayViewHourStart
        {
            get => (int)Schedule.DayViewSettings.StartHour;
            set
            {
                var x = HoursStartEndCheck(value, Schedule.DayViewSettings.EndHour);
                Schedule.DayViewSettings.EndHour = 24; //Avoid exception
                Schedule.DayViewSettings.StartHour = x.Item1;
                Schedule.DayViewSettings.EndHour = x.Item2;
                OnPropertyChanged(nameof(DayViewHourStart));
                OnPropertyChanged(nameof(DayViewHourEnd));

            }
        }
        public int DayViewHourEnd
        {
            get => (int)Schedule.DayViewSettings.EndHour;
            set
            {
                var x = HoursStartEndCheck(Schedule.DayViewSettings.StartHour, value);
                Schedule.DayViewSettings.EndHour = 24; //Avoid exception
                Schedule.DayViewSettings.StartHour = x.Item1;
                Schedule.DayViewSettings.EndHour = x.Item2;
                OnPropertyChanged(nameof(DayViewHourStart));
                OnPropertyChanged(nameof(DayViewHourEnd));
            }
        }

        public int DayViewWorkHourStart
        {
            get => (int)Schedule.DayViewSettings.WorkStartHour;
            set
            {
                var x = HoursStartEndCheck(value, Schedule.DayViewSettings.WorkEndHour);
                Schedule.DayViewSettings.WorkEndHour = 24; //Avoid exception
                Schedule.DayViewSettings.WorkStartHour = x.Item1;
                Schedule.DayViewSettings.WorkEndHour = x.Item2;
                OnPropertyChanged(nameof(DayViewWorkHourStart));
                OnPropertyChanged(nameof(DayViewWorkHourEnd));

            }
        }
        public int DayViewWorkHourEnd
        {
            get => (int)Schedule.DayViewSettings.WorkEndHour;
            set
            {
                var x = HoursStartEndCheck(Schedule.DayViewSettings.WorkStartHour, value);
                Schedule.DayViewSettings.WorkEndHour = 24; //Avoid exception
                Schedule.DayViewSettings.WorkStartHour = x.Item1;
                Schedule.DayViewSettings.WorkEndHour = x.Item2;
                OnPropertyChanged(nameof(DayViewWorkHourStart));
                OnPropertyChanged(nameof(DayViewWorkHourEnd));
            }
        }

        public bool DayViewShowAllDay
        {
            get => Schedule.DayViewSettings.ShowAllDay;
            set
            {
                Schedule.DayViewSettings.ShowAllDay = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region WeekView
        public int WeekViewHourStart
        {
            get => (int)Schedule.WeekViewSettings.StartHour;
            set
            {
                var x = HoursStartEndCheck(value, Schedule.WeekViewSettings.EndHour);
                Schedule.WeekViewSettings.EndHour = 24; //Avoid exception
                Schedule.WeekViewSettings.StartHour = x.Item1;
                Schedule.WeekViewSettings.EndHour = x.Item2;
                OnPropertyChanged(nameof(WeekViewHourStart));
                OnPropertyChanged(nameof(WeekViewHourEnd));

            }
        }
        public int WeekViewHourEnd
        {
            get => (int)Schedule.WeekViewSettings.EndHour;
            set
            {
                var x = HoursStartEndCheck(Schedule.WeekViewSettings.StartHour, value);
                Schedule.WeekViewSettings.EndHour = 24; //Avoid exception
                Schedule.WeekViewSettings.StartHour = x.Item1;
                Schedule.WeekViewSettings.EndHour = x.Item2;
                OnPropertyChanged(nameof(WeekViewHourStart));
                OnPropertyChanged(nameof(WeekViewHourEnd));
            }
        }

        public int WeekViewWorkHourStart
        {
            get => (int)Schedule.WeekViewSettings.WorkStartHour;
            set
            {
                var x = HoursStartEndCheck(value, Schedule.WeekViewSettings.WorkEndHour);
                Schedule.WeekViewSettings.WorkEndHour = 24; //Avoid exception
                Schedule.WeekViewSettings.WorkStartHour = x.Item1;
                Schedule.WeekViewSettings.WorkEndHour = x.Item2;
                OnPropertyChanged(nameof(WeekViewWorkHourStart));
                OnPropertyChanged(nameof(WeekViewWorkHourEnd));

            }
        }
        public int WeekViewWorkHourEnd
        {
            get => (int)Schedule.WeekViewSettings.WorkEndHour;
            set
            {
                var x = HoursStartEndCheck(Schedule.WeekViewSettings.WorkStartHour, value);
                Schedule.WeekViewSettings.WorkEndHour = 24; //Avoid exception
                Schedule.WeekViewSettings.WorkStartHour = x.Item1;
                Schedule.WeekViewSettings.WorkEndHour = x.Item2;
                OnPropertyChanged(nameof(WeekViewWorkHourStart));
                OnPropertyChanged(nameof(WeekViewWorkHourEnd));
            }
        }

        public bool WeekViewShowAllDay
        {
            get => Schedule.WeekViewSettings.ShowAllDay;
            set
            {
                Schedule.WeekViewSettings.ShowAllDay = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region WorkWeek
        public int WorkWeekHourStart
        {
            get => (int)Schedule.WorkWeekViewSettings.StartHour;
            set
            {
                var x = HoursStartEndCheck(value, Schedule.WorkWeekViewSettings.EndHour);
                Schedule.WorkWeekViewSettings.EndHour = 24; //Avoid exception
                Schedule.WorkWeekViewSettings.StartHour = x.Item1;
                Schedule.WorkWeekViewSettings.EndHour = x.Item2;
                OnPropertyChanged(nameof(WorkWeekHourStart));
                OnPropertyChanged(nameof(WorkWeekHourEnd));

            }
        }
        public int WorkWeekHourEnd
        {
            get => (int)Schedule.WorkWeekViewSettings.EndHour;
            set
            {
                var x = HoursStartEndCheck(Schedule.WorkWeekViewSettings.StartHour, value);
                Schedule.WorkWeekViewSettings.EndHour = 24; //Avoid exception
                Schedule.WorkWeekViewSettings.StartHour = x.Item1;
                Schedule.WorkWeekViewSettings.EndHour = x.Item2;
                OnPropertyChanged(nameof(WorkWeekHourStart));
                OnPropertyChanged(nameof(WorkWeekHourEnd));
            }
        }

        public int WorkWeekWorkHourStart
        {
            get => (int)Schedule.WorkWeekViewSettings.WorkStartHour;
            set
            {
                var x = HoursStartEndCheck(value, Schedule.WorkWeekViewSettings.WorkEndHour);
                Schedule.WorkWeekViewSettings.WorkEndHour = 24; //Avoid exception
                Schedule.WorkWeekViewSettings.WorkStartHour = x.Item1;
                Schedule.WorkWeekViewSettings.WorkEndHour = x.Item2;
                OnPropertyChanged(nameof(WorkWeekWorkHourStart));
                OnPropertyChanged(nameof(WorkWeekWorkHourEnd));

            }
        }
        public int WorkWeekWorkHourEnd
        {
            get => (int)Schedule.WorkWeekViewSettings.WorkEndHour;
            set
            {
                var x = HoursStartEndCheck(Schedule.WorkWeekViewSettings.WorkStartHour, value);
                Schedule.WorkWeekViewSettings.WorkEndHour = 24; //Avoid exception
                Schedule.WorkWeekViewSettings.WorkStartHour = x.Item1;
                Schedule.WorkWeekViewSettings.WorkEndHour = x.Item2;
                OnPropertyChanged(nameof(WorkWeekWorkHourStart));
                OnPropertyChanged(nameof(WorkWeekWorkHourEnd));
            }
        }

        public bool WorkWeekShowAllDay
        {
            get => Schedule.WorkWeekViewSettings.ShowAllDay;
            set
            {
                Schedule.WorkWeekViewSettings.ShowAllDay = value;
                OnPropertyChanged();
            }
        }

        #endregion

        private (double, double) HoursStartEndCheck(double start, double end)
        {
            if (start > end)
                end = start + 1;
            if (end < start)
                start = end - 1;
            if (start < 0)
                start = 0;
            if (end > 24)
                end = 24;
            if (start > end)
                end = start + 1;
            if (end < start)
                start = end - 1;
            return (start, end);
        }

        public int MonthViewAppointmentDisplayMode
        {
            get => (int)Schedule.MonthViewSettings.AppointmentDisplayMode;
            set
            {
                Schedule.MonthViewSettings.AppointmentDisplayMode = (AppointmentDisplayMode)Enum.ToObject(typeof(AppointmentDisplayMode), value);
                OnPropertyChanged();
            }
        }

        public int MonthViewAppointmentIndicatorCount
        { 
            get => Schedule.MonthViewSettings.AppointmentIndicatorCount; 
            set
            {
                Schedule.MonthViewSettings.AppointmentIndicatorCount = value;
                OnPropertyChanged();
            }
        }

        public int MonthViewAppointmentDisplayMode_SpinnerPosition { get => MonthViewAppointmentDisplayMode; set => MonthViewAppointmentDisplayMode = value; }
        public int MonthViewAppointmentIndicatorCount_SpinnerPosition { get => MonthViewAppointmentIndicatorCount; set => MonthViewAppointmentIndicatorCount = value; }


        public int MonthViewAgendaType_SpinnerPosition
        {
            get
            {
                if (Schedule.MonthViewSettings.ShowAppointmentsInline)
                    return 0;
                if (Schedule.MonthViewSettings.ShowAgendaView)
                    return 1;
                return 2;
            }
            set
            {
                var sel = Java.Util.Calendar.Instance;
                sel.TimeInMillis = Schedule.VisibleDates.First().TimeInMillis;
                if (Schedule.SelectedDate == null || Schedule.SelectedDate.Before(Schedule.VisibleDates.First()) || Schedule.SelectedDate.After(Schedule.VisibleDates.Last()))
                    sel.TimeInMillis = Schedule.VisibleDates.First().TimeInMillis + ((Schedule.VisibleDates.Last().TimeInMillis - Schedule.VisibleDates.First().TimeInMillis) / 5);
                else
                    sel.TimeInMillis = Schedule.SelectedDate.TimeInMillis;
                
                Schedule.SelectedDate = null;
                Schedule.MonthViewSettings.ShowAppointmentsInline = value == 0;
                Schedule.MonthViewSettings.ShowAgendaView = value == 1;
                OnPropertyChanged();

                Schedule.SelectedDate = sel;
            }
        }

        public bool MonthViewShowWeekNumber
        {
            get => Schedule.MonthViewSettings.ShowWeekNumber;
            set
            {
                Schedule.MonthViewSettings.ShowWeekNumber = value;
                OnPropertyChanged();
            }
        }

        public int MonthViewNavigationDirection
        {
            get => (int)Schedule.MonthViewSettings.MonthNavigationDirection;
            set
            {
                Schedule.MonthViewSettings.MonthNavigationDirection = (MonthNavigationDirections)Enum.ToObject(typeof(MonthNavigationDirections), value); ;
                OnPropertyChanged();
            }
        }

        public bool IsTimeLineView { get => Schedule?.ScheduleView == ScheduleView.Timeline; }
        public bool IsDayView { get => Schedule?.ScheduleView == ScheduleView.DayView; }
        public bool IsWeekView { get => Schedule?.ScheduleView == ScheduleView.WeekView; }
        public bool IsWorkWeek { get => Schedule?.ScheduleView == ScheduleView.WorkWeekView; }
        public bool IsMonthView { get => Schedule?.ScheduleView == ScheduleView.MonthView; }
    }
}