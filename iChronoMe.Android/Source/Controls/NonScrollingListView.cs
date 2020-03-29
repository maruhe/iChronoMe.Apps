using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.Controls
{
    [Register("me.ichrono.droid.Controls.NonScrollingListView")]

    public class NonScrollingListView : ListView
    {

        public NonScrollingListView(Context context) : base(context)
        {
        }
        public NonScrollingListView(Context context, IAttributeSet attrs) : base(context, attrs)
        {

        }
        public NonScrollingListView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {

        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            int heightMeasureSpec_custom = MeasureSpec.MakeMeasureSpec(
                    int.MaxValue >> 2, MeasureSpecMode.AtMost);
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec_custom);
            this.LayoutParameters.Height = MeasuredHeight;
        }
    }
}