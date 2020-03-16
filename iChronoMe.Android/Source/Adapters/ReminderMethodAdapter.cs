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
using iChronoMe.DeviceCalendar;

namespace iChronoMe.Droid.Source.Adapters
{
    public class ReminderMethodAdapter : BaseAdapter<int>
    {
        Activity mContext;

        static List<CalendarReminderMethod> items = new List<CalendarReminderMethod>((CalendarReminderMethod[])Enum.GetValues(typeof(CalendarReminderMethod)));

        public ReminderMethodAdapter(Activity context)
        {
            mContext = context;
        }

        public override int this[int position] => position;

        public override int Count => items.Count;

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var image = new ImageView(mContext);
            image.SetMinimumWidth(sys.DpPx(40));
            image.SetMinimumHeight(sys.DpPx(40));
            image.SetImageResource(Resource.Drawable.icons8_alarm);

            return image;
        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            var view = mContext.LayoutInflater.Inflate(Resource.Layout.listitem_title, parent, false);
            view.SetMinimumHeight(sys.DpPx(40));
            view.FindViewById<TextView>(Resource.Id.title).Text = items[position].ToString();

            return view;
        }
    }
}