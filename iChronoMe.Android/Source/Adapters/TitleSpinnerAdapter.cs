using System.Collections.Generic;

using Android.App;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Types;

namespace iChronoMe.Droid.Adapters
{
    public class TitleSpinnerAdapter : BaseAdapter<string>
    {
        Activity mContext;
        List<string> Items;
        List<int> Icons = new List<int>();
        xColor clTitleText;
        string Title;

        public TitleSpinnerAdapter(Activity context, string title) : this(context, title, new string[0]) { }

        public TitleSpinnerAdapter(Activity context, string title, ICollection<string> items)
        {
            mContext = context;
            Title = title;
            Items = new List<string>(items);
            clTitleText = Tools.GetThemeColor(mContext, Resource.Attribute.titleTextColor).ToColor(); //Tools.GetThemeColor(mContext.Theme, Resource.Attribute.ActionMenuTextColor).Value.ToColor();
        }

        public void UpdateIcons(ICollection<int> icons)
        {
            Icons.Clear();
            Icons.AddRange(icons);
        }

        public void xUpdateItems(ICollection<string> items)
        {
            Items.Clear();
            Items.AddRange(items);
            NotifyDataSetChanged();
        }
        public void xUpdateItems(string item)
        {
            Items.Clear();
            Items.Add(item);
            NotifyDataSetChanged();
        }

        public void UpdateTitle(string title)
        {
            Title = title;
            NotifyDataSetChanged();
        }

        public override string this[int position] => Items[position];

        public override int Count => Items.Count;

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = (TextView)mContext.LayoutInflater.Inflate(Android.Resource.Layout.SimpleSpinnerDropDownItem, null);
            view.Text = Title;
            view.SetTextColor(clTitleText.ToAndroid());
            return view;
        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            var view = mContext.LayoutInflater.Inflate(Resource.Layout.listitem_titlespinner, null);
            view.FindViewById<TextView>(Resource.Id.title).Text = Items[position];
            view.FindViewById<TextView>(Resource.Id.title).SetTextColor(clTitleText.ToAndroid());
            if (Icons.Count > position)
                view.FindViewById<ImageView>(Resource.Id.icon).SetImageDrawable(DrawableHelper.GetIconDrawable(mContext, Icons[position], clTitleText));
            //view.FindViewById<ImageView>(Resource.Id.icon).SetImageResource(Icons[position]);

            return view;
        }
    }
}
//view.FindViewById<ImageView>(Resource.Id.icon).SetImageBitmap(DrawableHelper.GetIconBitmap(mContext, Icons[position], 24, clrDropView));