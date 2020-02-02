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
using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.Widgets;

namespace iChronoMe.Droid.Adapters
{
    public class TimeTypeAdapter : BaseAdapter<SimpleObject>
    {
        List<SimpleObject> items;
        Activity context;

        public TimeTypeAdapter(Activity context) : base()
        {
            this.items = new List<SimpleObject>();
            this.items.Add(new SimpleObject() { Tag = TimeType.RealSunTime, Title1 = context.Resources.GetString(Resource.String.TimeType_RealSunTime), Description1 = context.Resources.GetString(Resource.String.TimeType_RealSunTime_Desc) });
            this.items.Add(new SimpleObject() { Tag = TimeType.MiddleSunTime, Title1 = context.Resources.GetString(Resource.String.TimeType_MiddleSunTime), Description1 = context.Resources.GetString(Resource.String.TimeType_MiddleSunTime_Desc) });
            this.items.Add(new SimpleObject() { Tag = TimeType.TimeZoneTime, Title1 = context.Resources.GetString(Resource.String.TimeType_TimeZoneTime), Description1 = context.Resources.GetString(Resource.String.TimeType_TimeZoneTime_Desc) });
            this.context = context;
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
            var item = items[position];


            if (convertView == null)
            {
                convertView = context.LayoutInflater.Inflate(Resource.Layout.listitem_title_detail, null);
            }

            convertView.FindViewById<ImageView>(Resource.Id.icon).SetImageResource(MainWidgetBase.GetTimeTypeIcon((TimeType)item.Tag));
            convertView.FindViewById<TextView>(Resource.Id.title).Text = item.Title1;
            convertView.FindViewById<TextView>(Resource.Id.description).Text = item.Description1;

            return convertView;
        }
    }    
}