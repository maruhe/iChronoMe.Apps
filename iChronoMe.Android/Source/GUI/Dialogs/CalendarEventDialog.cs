using System;

using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Widget;

using iChronoMe.Core.DataBinding;
using iChronoMe.Core.Interfaces;
using iChronoMe.Core.ViewModels;

namespace iChronoMe.Droid.GUI.Dialogs
{
    public class CalendarEventDialog : DialogFragment
    {
        CalendarEventEditViewModel mViewModel;
        DataBinder mBinder;
        string cEventId;

        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            try
            {
                LinearLayout vRoot = (LinearLayout)Activity.LayoutInflater.Inflate(Resource.Layout.fragment_calendar_event_popup, null);

                mViewModel = new CalendarEventEditViewModel(cEventId, Activity as IUserIO);
                mBinder = new DataBinder(Activity, vRoot);

                mBinder.BindViewProperty(Resource.Id.title, nameof(TextView.Text), mViewModel, nameof(CalendarEventEditViewModel.Title), BindMode.TwoWay);
                mBinder.BindViewProperty(Resource.Id.StartDate, nameof(TextView.Text), mViewModel, nameof(CalendarEventEditViewModel.DisplayStartDate), BindMode.TwoWay);
                mBinder.BindViewProperty(Resource.Id.StartTime, nameof(TextView.Text), mViewModel, nameof(CalendarEventEditViewModel.DisplayStartTime), BindMode.TwoWay);
                mBinder.BindViewProperty(Resource.Id.EndDate, nameof(TextView.Text), mViewModel, nameof(CalendarEventEditViewModel.DisplayEndDate), BindMode.TwoWay);
                mBinder.BindViewProperty(Resource.Id.EndTime, nameof(TextView.Text), mViewModel, nameof(CalendarEventEditViewModel.DisplayEndTime), BindMode.TwoWay);

                vRoot.FindViewById<TableRow>(Resource.Id.row_start).Click += CalendarEventDialog_Click;

                AlertDialog dialog = new AlertDialog.Builder(Context)
                .SetView(vRoot)
                .Create();

                mBinder.Start();

                return dialog;
            }
            catch (Exception ex)
            {
                xLog.Error(ex);
                return null;
            }
        }

        private void CalendarEventDialog_Click(object sender, EventArgs e)
        {
            Tools.ShowToast(Context, "klick");
        }

        public override void Show(FragmentManager manager, string tag)
        {
            base.Show(manager, tag);
            cEventId = tag;
        }
    }
}