using System.Collections.Generic;

using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
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
                    var o = new SimpleObject { Title1 = prop.Name.Replace("AppTheme_iChronoMe_", ""), Tag = prop.GetValue(null), Text1 = prop.Name };
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

            var wrapper = new ContextThemeWrapper(mContext, (int)item.Tag);
            var inflater = mContext.LayoutInflater.CloneInContext(wrapper);
            //getContext().getTheme().applyStyle(styleId, true); can be used tu load a style into a theme before Inflate

            var view = inflater.Inflate(Resource.Layout.listitem_themetemplate, null);

            var clrBack = Tools.GetThemeColor(wrapper, Android.Resource.Attribute.WindowBackground);
            view.SetBackgroundColor(clrBack);

            var clrText = Tools.GetThemeColor(wrapper, Android.Resource.Attribute.TextColorPrimary);
            view.FindViewById<TextView>(Resource.Id.title).Text = item.Title1;
            view.FindViewById<TextView>(Resource.Id.title).SetTextColor(clrText);

            view.FindViewById<ImageView>(Resource.Id.shape_1).SetImageDrawable(GetShape(Tools.GetThemeColor(wrapper, Resource.Attribute.titleTextColor), clrText));
            view.FindViewById<ImageView>(Resource.Id.shape_2).SetImageDrawable(GetShape(Tools.GetThemeColor(wrapper, Android.Resource.Attribute.ColorPrimary), clrText));
            view.FindViewById<ImageView>(Resource.Id.shape_3).SetImageDrawable(GetShape(Tools.GetThemeColor(wrapper, Android.Resource.Attribute.TextColorPrimary), clrText));
            view.FindViewById<ImageView>(Resource.Id.shape_4).SetImageDrawable(GetShape(Tools.GetThemeColor(wrapper, Android.Resource.Attribute.ColorAccent), clrText));
            view.FindViewById<ImageView>(Resource.Id.shape_5).SetImageDrawable(GetShape(Tools.GetThemeColor(wrapper, Android.Resource.Attribute.ColorPrimaryDark), clrText));
            
            return view;
        }

        private GradientDrawable GetShape(Color clr, Color clrStroke)
        {
            GradientDrawable gd = new GradientDrawable();
            gd.SetShape(ShapeType.Rectangle);
            gd.SetColor(clr);
            gd.SetStroke(2, clrStroke);// clrBack.GetBrightness() > 0.5 ? Color.Black : Color.White);
            gd.SetCornerRadius(15.0f);
            return gd;
        }
    }
}