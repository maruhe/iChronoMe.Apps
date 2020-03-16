using System.Collections.Generic;

using Android.App;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;

namespace iChronoMe.Droid.Adapters
{
    public class FaqAdapter : BaseAdapter<string>
    {
        List<string> items;
        Activity mContext;

        public FaqAdapter(Activity context)
        {
            mContext = context;
            items = new List<string>(FAQ.FaqList.Keys);
        }

        public override string this[int position] => items[position];

        public override int Count => items.Count;

        public override long GetItemId(int position) => position;

        public string GetDescription(int position)
        {
            return FAQ.FaqList[items[position]];
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = items[position];

            if (convertView == null)
            {
                convertView = mContext.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem1, null);
            }

            (convertView as TextView).Text = item;

            return convertView;
        }
    }
}