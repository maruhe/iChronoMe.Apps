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

namespace iChronoMe.Droid.Adapters
{
    public class TitleSpinnerAdapter : BaseAdapter<string>
    {
        Activity mContext;
        List<string> Items;

        public TitleSpinnerAdapter(Activity context, string item) : this (context, new string[] { item }) { }

        public TitleSpinnerAdapter(Activity context, ICollection<string> items)
        {
            mContext = context;
            Items = new List<string>(items);
        }

        public void UpdateItems(ICollection<string> items)
        {
            Items.Clear();
            Items.AddRange(items);
            NotifyDataSetChanged();
        }
        public void UpdateItems(string item)
        {
            Items.Clear();
            Items.Add(item);
            NotifyDataSetChanged();
        }

        public override string this[int position] => Items[position];

        public override int Count => Items.Count;

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = mContext.LayoutInflater.Inflate(Android.Resource.Layout.SimpleSpinnerDropDownItem, null);
            (view as TextView).Text = Items[position]; 
            (view as TextView).SetTextColor(Tools.GetThemeColor(mContext.Theme, Resource.Attribute.titleTextColor).Value);
            return view;
        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            var view = mContext.LayoutInflater.Inflate(Resource.Layout.listitem_title, null);
            view.FindViewById<TextView>(Resource.Id.title).Text = Items[position];
            view.FindViewById<TextView>(Resource.Id.title).SetTextColor(Tools.GetThemeColor(mContext.Theme, Android.Resource.Attribute.ActionMenuTextColor).Value);
            return view;
        }
    }
}