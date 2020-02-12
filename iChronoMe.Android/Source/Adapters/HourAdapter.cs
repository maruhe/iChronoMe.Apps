using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.Adapters
{
    public class HourAdapter : BaseAdapter<string>
    {
        List<string> items = new List<string>();
        Activity mContext;

        public HourAdapter(Activity context, int start = 0, int end = 24, Interval interval = Interval.Hour)
        {
            mContext = context;
            string cFormat = interval == Interval.Hour ? (CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.StartsWith("HH") ? "HH:mm" : "H tt") : "HH:mm";
            DateTime tStart = DateTime.Today.AddHours(start);
            DateTime tEnd = DateTime.Today.AddHours(end);
            while(tStart <= tEnd)
            {
                items.Add(tStart.ToString(cFormat));
                tStart += TimeSpan.FromMinutes((int)interval);
            }
        }

        public override string this[int position] => items[position];

        public override int Count => items.Count;

        public override long GetItemId(int position)
            => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = items[position];

            if (convertView == null)
            {
                convertView = mContext.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem1, null);
            }

            convertView.FindViewById<TextView>(Android.Resource.Id.Text1).Text = item;

            return convertView;
        }

        public enum Interval
        {
            Hour = 60,
            HalfHout = 30,
            QuaterHour = 15
        }
    }
}