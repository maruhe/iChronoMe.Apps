using System.Collections.Generic;

using Android.App;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.Adapters
{
    public class ThemeAdapter : BaseAdapter<SimpleObject>
    {
        private List<SimpleObject> Items = new List<SimpleObject>();
        Activity mContext;

        public ThemeAdapter(Activity context)
        {
            mContext = context;
            foreach (var prop in typeof(Resource.Style).GetFields())
            {
                if (prop.Name.StartsWith("AppTheme_iChronoMe_"))
                {
                    var o = new SimpleObject { Title1 = prop.Name.Replace("AppTheme_iChronoMe_", ""), Tag = prop.GetValue(null) };
                    Items.Add(o);
                }
            }
        }

        public override SimpleObject this[int position] => Items[position];

        public override int Count => Items.Count;

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = Items[position];

            //var view = new LinearLayout(new ContextThemeWrapper(mContext, (int)item.Tag));

            var view = (TableLayout)mContext.LayoutInflater.Inflate(Resource.Layout.listitem_themetemplate, null);

            view.FindViewById<Button>(Resource.Id.button1).SetTextAppearance((int)item.Tag);



            return view;
        }
    }
}