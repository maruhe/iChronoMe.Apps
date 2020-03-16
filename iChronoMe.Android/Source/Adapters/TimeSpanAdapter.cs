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

namespace iChronoMe.Droid.Adapters
{
    public class TimeSpanAdapter : BaseAdapter<TimeSpan>
    {
        Activity mContext;
        List<TimeSpan> items = new List<TimeSpan>();

        public TimeSpanAdapter(Activity context, Mode mode, TimeSpan? current)
        {
            mContext = context;

            if (mode == Mode.EventReminders)
            {
                items.Add(TimeSpan.FromTicks(0));
                items.Add(TimeSpan.FromMinutes(5));
                items.Add(TimeSpan.FromMinutes(10));
                items.Add(TimeSpan.FromMinutes(15));
                items.Add(TimeSpan.FromMinutes(30));
                items.Add(TimeSpan.FromMinutes(45));
                items.Add(TimeSpan.FromMinutes(60));
                items.Add(TimeSpan.FromMinutes(90));
                items.Add(TimeSpan.FromHours(2));
                items.Add(TimeSpan.FromHours(3));
                items.Add(TimeSpan.FromHours(4));
                items.Add(TimeSpan.FromHours(6));
                items.Add(TimeSpan.FromDays(1));
                items.Add(TimeSpan.FromDays(2));
                items.Add(TimeSpan.FromDays(3));
                items.Add(TimeSpan.FromDays(7));
                //items.Add(TimeSpan.FromTicks(-1)); //Ask User
            }
            else
                throw new NotImplementedException();

            if (current.HasValue && !items.Contains(current.Value))
                items.Add(current.Value);
        }

        public void SetCurrent(TimeSpan? current)
        {
            while (items.Count > 0 && items[items.Count - 1].Ticks >= 0)
                items.RemoveAt(items.Count - 1);
            if (current.HasValue && !items.Contains(current.Value))
                items.Add(current.Value);
        }

        public override TimeSpan this[int position] => items[position];

        public int IndexOf(TimeSpan ts) => items.IndexOf(ts);

        public override int Count => items.Count;

        public override long GetItemId(int position) => position;
        
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = (convertView as TextView) ?? new TextView(mContext);
            view.SetPadding(sys.DpPx(5), 0, 0, 0);
            view.Gravity = GravityFlags.CenterVertical;
            view.LayoutParameters = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, sys.DpPx(40))
            {
                Gravity = GravityFlags.CenterVertical
            };

            var item = items[position];
            string cSpan = item.ToString();
            if (item.Ticks == 0)
                cSpan = "at begin";
            else if (item.Ticks < 0)
                cSpan = "custom";
            else
            {
                if (item.TotalHours < 1 || item.TotalMinutes == 90)
                    cSpan = string.Format("{0} minutes", (int)item.TotalMinutes);
                else if (item.TotalHours == item.Hours)
                    cSpan = string.Format("{0} hours", (int)item.TotalHours);
                else if (item.TotalDays == item.Days)
                    cSpan = string.Format("{0} days", (int)item.TotalDays);
                else
                {
                    cSpan = "";
                    if (item.TotalDays > 1)
                        cSpan += string.Format("{0} days", (int)item.TotalDays)+", ";
                    if (item.TotalHours % 24 > 1)
                        cSpan += string.Format("{0} hours", (int)item.TotalHours % 24) + ", ";
                    if (item.TotalMinutes % 60 > 1)
                        cSpan += string.Format("{0} minutes", (int)item.TotalHours % 60);
                    cSpan = cSpan.TrimEnd().TrimEnd(',');
                }
            }
            view.Text = cSpan;
            return view;
        }

        public enum Mode
        {
            Undefined,
            EventReminders
        }
    }
}