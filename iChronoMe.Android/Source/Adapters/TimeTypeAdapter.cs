using System.Collections.Generic;

using Android.App;
using Android.Views;
using Android.Widget;

using iChronoMe.Core;

namespace iChronoMe.Droid.Adapters
{
    public class TimeTypeAdapter : BaseAdapter<SimpleObject>
    {
        List<SimpleObject> items;
        Activity mContext;
        bool IsSpinner;
        LocationTimeHolder lth = null;
        public LocationTimeHolder LocationTimeHolder { get => lth; set { lth = value; NotifyDataSetChanged(); } }

        public TimeTypeAdapter(Activity context, bool bIsSpinner = false) : base()
        {
            this.items = new List<SimpleObject>();
            this.items.Add(new SimpleObject() { Tag = TimeType.RealSunTime, Title1 = context.Resources.GetString(Resource.String.TimeType_RealSunTime), Description1 = context.Resources.GetString(Resource.String.TimeType_RealSunTime_Desc) });
            this.items.Add(new SimpleObject() { Tag = TimeType.MiddleSunTime, Title1 = context.Resources.GetString(Resource.String.TimeType_MiddleSunTime), Description1 = context.Resources.GetString(Resource.String.TimeType_MiddleSunTime_Desc) });
            this.items.Add(new SimpleObject() { Tag = TimeType.TimeZoneTime, Title1 = context.Resources.GetString(Resource.String.TimeType_TimeZoneTime), Description1 = context.Resources.GetString(Resource.String.TimeType_TimeZoneTime_Desc) });
            mContext = context;
            IsSpinner = bIsSpinner;
        }


        //indexer 
        public override SimpleObject this[int position]
        {
            get
            {
                return items[position];
            }
        }

        public override int Count
        {
            get
            {
                return items.Count;
            }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (!IsSpinner)
                return GetDropDownView(position, convertView, parent);

            var item = items[position];

            if (convertView == null)
            {
                convertView = mContext.LayoutInflater.Inflate(Resource.Layout.listitem_title, null);
            }

            convertView.FindViewById<ImageView>(Resource.Id.icon).SetImageResource(Tools.GetTimeTypeIconID((TimeType)item.Tag, lth));
            convertView.FindViewById<TextView>(Resource.Id.title).Text = item.Title1;

            return convertView;
        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            var item = items[position];

            if (convertView == null)
            {
                convertView = mContext.LayoutInflater.Inflate(Resource.Layout.listitem_title_detail, null);
            }

            convertView.FindViewById<ImageView>(Resource.Id.icon).SetImageResource(Tools.GetTimeTypeIconID((TimeType)item.Tag, lth));
            convertView.FindViewById<TextView>(Resource.Id.title).Text = item.Title1;
            convertView.FindViewById<TextView>(Resource.Id.description).Text = item.Description1;

            return convertView;
        }
    }
}