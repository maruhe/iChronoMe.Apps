using System;
using System.Collections.Generic;

using Android.App;
using Android.Views;
using Android.Widget;

using iChronoMe.Widgets;

namespace iChronoMe.Droid.Adapters
{
    public class ClickActionTypeAdapter : BaseAdapter<string>
    {
        List<string> items { get; } = new List<string>();
        Dictionary<ClickActionType, int> positions = new Dictionary<ClickActionType, int>();
        Dictionary<int, ClickActionType> values = new Dictionary<int, ClickActionType>();
        Activity mContext;

        public ClickActionTypeAdapter(Activity context, bool allowSettings = true, bool allowAnimate = false)
        {
            mContext = context;
            int pos = 0;

            foreach (ClickActionType ca in Enum.GetValues(typeof(ClickActionType)))
            {
                if (ca == ClickActionType.OpenSettings && !allowSettings)
                    continue;
                if (ca == ClickActionType.Animate && !allowAnimate)
                    continue;
                if (ca == ClickActionType.CreateAlarm)
                    continue;
                string c = ca.ToString();
                var res = typeof(Resource.String).GetField("ClickActionType_" + c);
                if (res != null)
                    c = context.Resources.GetString((int)res.GetValue(null));
                items.Add(c);
                values.Add(pos, ca);
                positions.Add(ca, pos);
                pos++;
            }
        }

        public override string this[int position] => items[position];

        public override int Count => items.Count;

        public override long GetItemId(int position)
            => position;

        public int GetPos(ClickActionType type)
        {
            if (!positions.ContainsKey(type))
                return -1;
            return positions[type];
        }

        public ClickActionType GetValue(int pos)
        {
            if (!values.ContainsKey(pos))
                return ClickActionType.None;
            return values[pos];
        }

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
    }
}