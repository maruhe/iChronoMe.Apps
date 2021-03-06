﻿using System;
using System.Globalization;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.DataBinding;
using iChronoMe.Core.Types;
using iChronoMe.Core.ViewModels;
using iChronoMe.Droid.Adapters;
using iChronoMe.Droid.GUI.Dialogs;
using iChronoMe.Droid.Widgets;

using Net.ArcanaStudio.ColorPicker;

using Xamarin.Essentials;

using static Android.App.DatePickerDialog;
using static Android.App.TimePickerDialog;

namespace iChronoMe.Droid.GUI.Calendar
{

    [Activity(Label = "EventEditActivity", Name = "me.ichrono.droid.GUI.Calendar.EventEditActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "")]
    public class EventEditActivity : BaseActivity, IMenuItemOnMenuItemClickListener
    {
        public const string extra_EventID = "EventId";
        public const string extra_StartTime = "StartTime";
        public const string extra_TimeType = "TimeType";

        ViewGroup rootView;
        AutoCompleteTextView eSubject, eLocation;
        EditText dateStart, dateEnd;
        EditText timeStart, timeEnd;
        Spinner spCalendar;
        CalendarListAdapter calendarAdapter;
        CalendarEventEditViewModel mModel;
        Button btnAddReminder;
        DataBinder mBinder;
        string cEventId;
        TimeTypeAdapter ttAdapter = null;
        CalendarEventRemindersAdapter reminderAdapter = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            LoadAppTheme();
            SetContentView(Resource.Layout.activity_calendar_event);
            Toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(Toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            rootView = FindViewById<ViewGroup>(Resource.Id.rootView);
            eSubject = FindViewById<AutoCompleteTextView>(Resource.Id.Subject);
            dateStart = FindViewById<EditText>(Resource.Id.StartDate);
            timeStart = FindViewById<EditText>(Resource.Id.StartTime);
            dateEnd = FindViewById<EditText>(Resource.Id.EndDate);
            timeEnd = FindViewById<EditText>(Resource.Id.EndTime);
            eLocation = FindViewById<AutoCompleteTextView>(Resource.Id.location);

            dateStart.FocusChange += DateStart_FocusChange;
            dateStart.Click += DateStart_Click;
            timeStart.FocusChange += TimeStart_FocusChange;
            timeStart.Click += TimeStart_Click;
            dateEnd.FocusChange += DateEnd_FocusChange;
            dateEnd.Click += DateEnd_Click;
            timeEnd.FocusChange += TimeEnd_FocusChange;
            timeEnd.Click += TimeEnd_Click;

            cEventId = Intent.HasExtra(extra_EventID) ? Intent.GetStringExtra(extra_EventID) : "";

            eSubject.Adapter = new CalendarEventTitleAdapter(this, null);
            eSubject.Threshold = 3;
            eLocation.Adapter = new CalendarEventLocationAdapter(this, null);
            eLocation.Threshold = 3;

            calendarAdapter = new CalendarListAdapter(this, CalendarListAdapter.CalendarFilter.AllEditable);

            if (string.IsNullOrEmpty(cEventId))
            {
                var tt = AppConfigHolder.CalendarViewConfig.TimeType;
                try
                {
                    tt = (TimeType)Enum.ToObject(typeof(TimeType), Intent.GetIntExtra(extra_TimeType, (int)AppConfigHolder.CalendarViewConfig.TimeType));
                }
                catch { }
                DateTime? tStart = null;
                if (Intent.HasExtra(extra_StartTime))
                {
                    long start = Intent.GetLongExtra(extra_StartTime, -1);
                    tStart = new DateTime(start);
                    if (tStart < DateTime.Now.AddYears(-100) || tStart > DateTime.Now.AddYears(150))
                    {
                        tStart = null;
                    }
                }
                mModel = new CalendarEventEditViewModel(string.Empty, this, tt, tStart);
            }
            if (mModel == null)
                mModel = new CalendarEventEditViewModel(cEventId, this);

            mBinder = new DataBinder(this, FindViewById<ViewGroup>(Resource.Id.rootView));
            ttAdapter = new TimeTypeAdapter(this, true);
            ttAdapter.LocationTimeHolder = mModel.LocationTimeHolder;
            mModel.PropertyChanged += MModel_PropertyChanged;
            mModel.LocationTimeHolderChanged += MModel_LocationTimeHolderChanged;

            if (string.IsNullOrEmpty(cEventId))
                SetTitle(Resource.String.title_new_event);
            else
                SetTitle(Resource.String.title_edit_event);

            spCalendar = FindViewById<Spinner>(Resource.Id.spCalendar);
            spCalendar.Adapter = calendarAdapter;
            spCalendar.ItemSelected += SpCalendar_ItemSelected;
            calendarAdapter.ItemsLoadet += CalendarAdapter_ItemsLoadet;

            mBinder.BindViewProperty(Resource.Id.Subject, nameof(EditText.Text), mModel, nameof(CalendarEventEditViewModel.Title), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.StartDate, nameof(EditText.Text), mModel, nameof(CalendarEventEditViewModel.DisplayStartDate), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.StartTime, nameof(EditText.Text), mModel, nameof(CalendarEventEditViewModel.DisplayStartTime), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.EndDate, nameof(EditText.Text), mModel, nameof(CalendarEventEditViewModel.DisplayEndDate), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.EndTime, nameof(EditText.Text), mModel, nameof(CalendarEventEditViewModel.DisplayEndTime), BindMode.TwoWay);

            mBinder.BindViewProperty(Resource.Id.StartHelper, nameof(TextView.Text), mModel, nameof(CalendarEventEditViewModel.StartTimeHelper));
            mBinder.BindViewProperty(Resource.Id.EndHelper, nameof(TextView.Text), mModel, nameof(CalendarEventEditViewModel.EndTimeHelper));

            mBinder.BindViewProperty(Resource.Id.AllDay, nameof(Switch.Checked), mModel, nameof(CalendarEventEditViewModel.AllDay), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.StartTime, nameof(EditText.Visibility), mModel, nameof(CalendarEventEditViewModel.NotAllDay));
            mBinder.BindViewProperty(Resource.Id.EndTime, nameof(EditText.Visibility), mModel, nameof(CalendarEventEditViewModel.NotAllDay));

            mBinder.BindViewProperty(Resource.Id.row_timetype, nameof(View.Visibility), mModel, nameof(CalendarEventEditViewModel.NotAllDay));
            mBinder.BindViewProperty(Resource.Id.spTimeType, nameof(Spinner.SelectedItemPosition), mModel, nameof(CalendarEventEditViewModel.TimeTypeSpinnerPos), BindMode.TwoWay);
            FindViewById<Spinner>(Resource.Id.spTimeType).Adapter = ttAdapter;
            //FindViewById<Spinner>(Resource.Id.spTimeType).DispatchSetSelected(false);
            //FindViewById<Spinner>(Resource.Id.spTimeType).SetSelection(0, false);

            mBinder.BindViewProperty(Resource.Id.location, nameof(EditText.Text), mModel, nameof(CalendarEventEditViewModel.Location), BindMode.TwoWay);
            mBinder.BindViewProperty(Resource.Id.location_progress, nameof(View.Visibility), mModel, nameof(CalendarEventEditViewModel.IsSearchingForLocation));
            mBinder.BindViewProperty(Resource.Id.location_helper, nameof(TextView.Text), mModel, nameof(CalendarEventEditViewModel.LocationHelper));
            mBinder.BindViewProperty(Resource.Id.location_time_info, nameof(TextView.Text), mModel, nameof(CalendarEventEditViewModel.LocationTimeInfo));

            mBinder.BindViewProperty(Resource.Id.description, nameof(EditText.Text), mModel, nameof(CalendarEventEditViewModel.Description));

            mBinder.BindViewProperty(Resource.Id.row_start_helper, nameof(View.Visibility), mModel, nameof(CalendarEventEditViewModel.ShowTimeHelpers));
            mBinder.BindViewProperty(Resource.Id.row_end_helper, nameof(View.Visibility), mModel, nameof(CalendarEventEditViewModel.ShowTimeHelpers));
            mBinder.BindViewProperty(Resource.Id.row_location_helper, nameof(View.Visibility), mModel, nameof(CalendarEventEditViewModel.ShowLocationHelper));

            FindViewById<LinearLayout>(Resource.Id.row_location_helper).Click += llLocationHelper_Click;
            FindViewById<ImageButton>(Resource.Id.btn_select_location).Click += btnSelectLocation_Click;

            btnAddReminder = FindViewById<Button>(Resource.Id.btn_add_reminder);
            btnAddReminder.Click += btnAddReminder_Click;
        }

        private async void btnSelectLocation_Click(object sender, EventArgs e)
        {
            Location current = null;
            if (mModel.Extention.GotCorrectPosition)
            {
                current = new Location(mModel.Extention.Latitude, mModel.Extention.Longitude);
            }
            var pos = await LocationPickerDialog.SelectLocation(this, null, current);
            if (pos != null)
            {
                mModel.UpdateLocation(sys.DezimalGradToString(pos.Latitude, pos.Longitude), pos.Latitude, pos.Longitude);
            }
        }

        private void btnAddReminder_Click(object sender, EventArgs e)
        {
            var rem = new DeviceCalendar.CalendarEventReminder() { TimeBefore = TimeSpan.FromMinutes(15), Method = DeviceCalendar.CalendarReminderMethod.Alert };
            if (mModel.Reminders.Count > 0 && mModel.Reminders[mModel.Reminders.Count - 1].TimeBefore.Ticks > 0)
            {
                if (mModel.Reminders[mModel.Reminders.Count - 1].TimeBefore.TotalDays < 3)
                    rem.TimeBefore = mModel.Reminders[mModel.Reminders.Count - 1].TimeBefore * 2;
                else
                    rem.TimeBefore = mModel.Reminders[mModel.Reminders.Count - 1].TimeBefore + TimeSpan.FromDays(1);
            }
            mModel.Reminders.Add(rem);
            reminderAdapter.NotifyDataSetChanged();
        }

        public override bool OnSupportNavigateUp()
        {
            OnBackPressed();
            return true;
        }

        protected override void OnResume()
        {

            base.OnResume();
            Task.Factory.StartNew(async () =>
            {
                await mModel.WaitForReady();
                RunOnUiThread(() =>
                {
                    try
                    {
                        if (mModel.HasErrors)
                            throw new Exception();
                        if (string.IsNullOrEmpty(mModel.ExternalID))
                            SetTitle(Resource.String.title_new_event);
                        else
                            SetTitle(Resource.String.title_edit_event);
                        mBinder.Start();
                        InvalidateOptionsMenu();
                        reminderAdapter = new CalendarEventRemindersAdapter(this, mModel);
                        reminderAdapter.CountChanged += ReminderAdapter_CountChanged;
                        ReminderAdapter_CountChanged(null, null);
                        FindViewById<ListView>(Resource.Id.lv_reminders).Adapter = reminderAdapter;
                        if (calendarAdapter.IsReady)
                            CalendarAdapter_ItemsLoadet(this, new EventArgs());
                    }
                    catch
                    {
                        StartActivity(MainWidgetBase.GetClickActionIntent(this, new iChronoMe.Widgets.ClickAction(iChronoMe.Widgets.ClickActionType.OpenCalendar), -1, null));
                        FinishAndRemoveTask();
                    }
                });
            });
        }

        private void ReminderAdapter_CountChanged(object sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                //FindViewById<TableRow>(Resource.Id.row_reminders).SetMinimumHeight(sys.DpPx(41 * reminderAdapter.Count));
                if (sender != null)
                    FindViewById(Resource.Id.row_decsription).RequestFocus();
                if (reminderAdapter.Count < 5)
                    FindViewById(Resource.Id.row_reminder_add).Visibility = ViewStates.Visible;
                else
                    FindViewById(Resource.Id.row_reminder_add).Visibility = ViewStates.Gone;
            });
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            base.OnPrepareOptionsMenu(menu);

            if (!mModel.IsReady)
                return true;

            var item = menu.Add(0, 10, 0, Resource.String.action_colors);
            item.SetIcon(DrawableHelper.GetIconDrawable(this, Resource.Drawable.circle_shape, mModel.DisplayColor));
            item.SetShowAsAction(ShowAsAction.Always);
            item.SetOnMenuItemClickListener(this);

            item = menu.Add(0, 100, 0, Resource.String.action_save);
            item.SetIcon(DrawableHelper.GetIconDrawable(this, Resource.Drawable.icons8_save, Tools.GetThemeColor(this, Resource.Attribute.iconTitleTint)));
            item.SetShowAsAction(ShowAsAction.Always);
            item.SetOnMenuItemClickListener(this);

            return true;
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            HideKeyboard(rootView);
            if (item.ItemId == 10)
            {
                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(150);
                    var clrDlg = ColorPickerDialog.NewBuilder()
                        .SetDialogType(ColorPickerDialog.DialogType.Preset)
                        .SetAllowCustom(false)
                        .SetShowColorShades(true)
                        .SetColor(mModel.DisplayColor.ToAndroid())
                        .SetColorShape(ColorShape.Circle)
                        .SetShowAlphaSlider(false)
                        .SetDialogTitle(Resource.String.action_colors);

                    var clr = await clrDlg.ShowAsyncNullable(this);
                    if (clr.HasValue)
                    {
                        mModel.DisplayColor = clr.Value.ToColor();
                        InvalidateOptionsMenu();
                    }
                });
                return true;
            }

            if (item.ItemId == 100)
            {
                if (string.IsNullOrEmpty(mModel.Title) && string.IsNullOrEmpty(mModel.Description))
                {
                    Tools.ShowMessage(this, "hold on", "and enter some text");
                    return true;
                }

                Task.Factory.StartNew(async () =>
                {
                    var saved = await mModel.SaveEvent();
                    if (!AppConfigHolder.CalendarViewConfig.CalendarReminderWarningDone && mModel.Reminders.Count > 0)
                    {
                        AppConfigHolder.CalendarViewConfig.CalendarReminderWarningDone = true;
                        AppConfigHolder.SaveCalendarViewConfig();
                        await Tools.ShowMessageAndWait(this, "warning", "iChronoMe currently does no support notifications on reminders\nso be sure another calendar-app notifies you!");
                    }
                    RunOnUiThread(() =>
                    {
                        if (saved)
                            FinishAndRemoveTask();
                        else
                            Tools.ShowMessage(this, Resources.GetString(Resource.String.error_saving_event), mModel.ErrorText);
                    });
                });
                return true;
            }
            return false;
        }

        private void MModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (nameof(CalendarEventEditViewModel.CalendarId).Equals(e.PropertyName))
                RefreshCalendarSpinner();
            if (nameof(CalendarEventEditViewModel.DisplayColor).Equals(e.PropertyName))
                InvalidateOptionsMenu();
        }

        private void MModel_LocationTimeHolderChanged(object sender, EventArgs e)
        {
            ttAdapter.LocationTimeHolder = mModel.LocationTimeHolder;
        }

        private void CalendarAdapter_ItemsLoadet(object sender, EventArgs e)
        {
            RefreshCalendarSpinner();
        }

        private void RefreshCalendarSpinner()
        {
            if (!mModel.IsReady)
                return;
            if (!calendarAdapter.IsReady)
                return;
            int pos = calendarAdapter.GetCalendarPosition(mModel.CalendarId);
            if (pos < 0)
            {
                if (calendarAdapter.Filter == CalendarListAdapter.CalendarFilter.PrimaryOnly)
                {
                    calendarAdapter.Filter = CalendarListAdapter.CalendarFilter.AllEditable;
                    return;
                }
                else if (calendarAdapter.Filter == CalendarListAdapter.CalendarFilter.AllEditable)
                {
                    calendarAdapter.Filter = CalendarListAdapter.CalendarFilter.AllCalendars;
                    return;
                }
                else
                {
                    Tools.ShowToast(this, "strange calendar error", true);
                    string cList = "";
                    for (int i = 0; i < calendarAdapter.Count; i++)
                        cList += string.Concat(calendarAdapter.GetItem(i), ", ");
                    sys.LogException(new Exception(string.Concat("calendar not found in CalendarListAdapter\nid: ", mModel.CalendarId, "\nlist: ", cList)));
                }
                return;
            }

            RunOnUiThread(() =>
            {
                spCalendar.DispatchSetSelected(false);
                spCalendar.SetSelection(pos, false);
            });
        }

        private void SpCalendar_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            if (!mModel.IsReady)
                return;
            var calID = (string)calendarAdapter.GetItem(e.Position);
            if (string.IsNullOrEmpty(calID))
                return;
            if (calID.Equals(mModel.CalendarId))
                return;

            mModel.CalendarId = calID;
        }

        private void llLocationHelper_Click(object sender, EventArgs e)
        {
            Tools.ShowToast(this, "here could be some nice information");
        }

        private void DateStart_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                ShowDateDialog(nameof(CalendarEventEditViewModel.DisplayStartDate));
        }

        private void DateStart_Click(object sender, EventArgs e)
        {
            ShowDateDialog(nameof(CalendarEventEditViewModel.DisplayStartDate));
        }

        private void DateEnd_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                ShowDateDialog(nameof(CalendarEventEditViewModel.DisplayEndDate));
        }

        private void DateEnd_Click(object sender, EventArgs e)
        {
            ShowDateDialog(nameof(CalendarEventEditViewModel.DisplayEndDate));
        }

        private void TimeStart_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                ShowTimeDialog(nameof(CalendarEventEditViewModel.DisplayStartTime));
        }

        private void TimeStart_Click(object sender, EventArgs e)
        {
            ShowTimeDialog(nameof(CalendarEventEditViewModel.DisplayStartTime));
        }

        private void TimeEnd_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
                ShowTimeDialog(nameof(CalendarEventEditViewModel.DisplayEndTime));
        }

        private void TimeEnd_Click(object sender, EventArgs e)
        {
            ShowTimeDialog(nameof(CalendarEventEditViewModel.DisplayEndTime));
        }

        string dateDlgProp = string.Empty;
        string timeDlgProp = string.Empty;
        public void ShowDateDialog(string property)
        {
            dateDlgProp = property;
            var prop = mModel.GetType().GetProperty(property);
            var date = (DateTime)prop.GetValue(mModel);
            var dlg = new DatePickerDialog(this, DateDlgChaged, date.Year, date.Month - 1, date.Day);
            dlg.Show();
            HideKeyboard(rootView);
        }

        public void ShowTimeDialog(string property)
        {
            timeDlgProp = property;
            var prop = mModel.GetType().GetProperty(property);
            var time = (TimeSpan)prop.GetValue(mModel);
            var dlg = new TimePickerDialog(this, TimeDlgChanged, time.Hours, time.Minutes, CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.StartsWith("HH"));
            dlg.Show();
            HideKeyboard(rootView);
        }

        protected void DateDlgChaged(object sender, DateSetEventArgs e)
        {
            var prop = mModel.GetType().GetProperty(dateDlgProp);
            prop.SetValue(mModel, e.Date);

            dateDlgProp = string.Empty;
        }

        protected void TimeDlgChanged(object sender, TimeSetEventArgs e)
        {
            var prop = mModel.GetType().GetProperty(timeDlgProp);
            prop.SetValue(mModel, new TimeSpan(e.HourOfDay, e.Minute, 0));

            timeDlgProp = string.Empty;
        }

        protected override void OnPause()
        {
            mBinder.Stop();
            base.OnPause();
        }

        protected override void OnStop()
        {
            base.OnStop();
            FinishAndRemoveTask();
        }
    }
}