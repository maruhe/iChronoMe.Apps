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
using iChronoMe.Core.Classes;
using iChronoMe.Core.ViewModels;
using iChronoMe.DeviceCalendar;

namespace iChronoMe.Droid.Adapters
{
    class CalendarEventRemindersAdapter : BaseAdapter
    {

        Activity mContext;
        CalendarEventEditViewModel mModel;
        int lastCount = -1;

        public CalendarEventRemindersAdapter(Activity context, CalendarEventEditViewModel model)
        {
            mContext = context;
            mModel = model;
            lastCount = mModel.Reminders.Count;
        }

        public override Java.Lang.Object GetItem(int position) => position;

        public override long GetItemId(int position) => position;

        public override int Count {
            get
            {
                if (lastCount != mModel.Reminders.Count)
                {
                    lastCount = mModel.Reminders.Count;
                    CountChanged?.Invoke(this, new EventArgs());
                }
                return mModel.Reminders.Count;
            }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var reminder = mModel.Reminders[position];
            var view = convertView;
            CalendarEventRemindersAdapterViewHolder holder = null;

            if (view != null)
                holder = view.Tag as CalendarEventRemindersAdapterViewHolder;

            if (holder == null)
            {
                holder = new CalendarEventRemindersAdapterViewHolder();
                var inflater = mContext.GetSystemService(Context.LayoutInflaterService).JavaCast<LayoutInflater>();
                
                view = inflater.Inflate(Resource.Layout.listitem_event_reminder, parent, false);

                view.Focusable = true;
                view.FocusableInTouchMode = true;

                holder.spTimeSpan = view.FindViewById<Spinner>(Resource.Id.sp_timespan);
                holder.spMethod = view.FindViewById<Spinner>(Resource.Id.sp_method);
                holder.btnDelete = view.FindViewById<ImageButton>(Resource.Id.btn_delete);

                holder.spTimeSpan.ItemSelected += SpTimeSpan_ItemSelected;
                holder.spMethod.Visibility = ViewStates.Gone;
                //holder.spMethod.ItemSelected += SpMethod_ItemSelected;
                holder.btnDelete.Click += BtnDelete_Click;

                holder.spTimeSpan.DispatchSetSelected(false);
                holder.spTimeSpan.SetSelection(-1, false);

                holder.spMethod.DispatchSetSelected(false);
                holder.spMethod.SetSelection(-1, false);

                view.Tag = holder;
            }

            //if (holder.spTimeSpan.Adapter is TimeSpanAdapter)
            //    (holder.spTimeSpan.Adapter as TimeSpanAdapter).SetCurrent(reminder.TimeBefore);
            //else
            holder.spTimeSpan.Adapter = new TimeSpanAdapter(mContext, TimeSpanAdapter.Mode.EventReminders, reminder.TimeBefore);

            holder.spTimeSpan.ItemSelected -= SpTimeSpan_ItemSelected;
            holder.spTimeSpan.SetSelection((holder.spTimeSpan.Adapter as TimeSpanAdapter).IndexOf(reminder.TimeBefore), false);
            holder.spTimeSpan.ItemSelected += SpTimeSpan_ItemSelected;

            //holder.spMethod.DispatchSetSelected(false);
            //holder.spMethod.SetSelection((int)reminder.Method, false);

            holder.spTimeSpan.Tag = holder.spMethod.Tag = holder.btnDelete.Tag = position;            

            return view;
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                int pos = (int)(sender as ImageButton).Tag;
                mModel.Reminders.RemoveAt(pos);
            } catch { }
            NotifyDataSetChanged();
        }

        private void SpMethod_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            return;
            try
            {
                int pos = (int)(sender as Spinner).Tag;
                mModel.Reminders[pos].Method = (CalendarReminderMethod)Enum.ToObject(typeof(CalendarReminderMethod), pos);
            } catch { }
            NotifyDataSetChanged();
        }

        private void SpTimeSpan_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            try
            {
                int pos = (int)(sender as Spinner).Tag;
                var adapter = ((sender as Spinner).Adapter as TimeSpanAdapter);
                TimeSpan ts = adapter[e.Position];
                if (ts.Ticks < 0)
                {
                    ts = TimeSpan.FromMinutes(23);
                }
                if (Equals(mModel.Reminders[pos].TimeBefore, ts))
                    return;
                mModel.Reminders[pos].TimeBefore = ts;
            } catch { }
            NotifyDataSetChanged();
        }

        public event EventHandler CountChanged;
    }
            
    class CalendarEventRemindersAdapterViewHolder : Java.Lang.Object
    {
        public Spinner spTimeSpan { get; set; }
        public Spinner spMethod { get; set; }
        public ImageButton btnDelete { get; set; }
    }
}