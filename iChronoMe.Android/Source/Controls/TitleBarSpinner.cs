using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Widget;

namespace iChronoMe.Droid.Controls
{
    [Register("me.ichrono.droid.Controls.TitleBarSpinner")]
    public class TitleBarSpinner : Spinner
    {

        public TitleBarSpinner(Context context) : base(context)
        {

        }

        public TitleBarSpinner(Context context, IAttributeSet attrs) : base(context, attrs)
        {

        }

        public TitleBarSpinner(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {

        }

        public override void SetSelection(int position, bool animate)
        {
            bool sameSelected = position == SelectedItemPosition;
            base.SetSelection(position, animate);
            if (sameSelected)
            {
                // Spinner does not call the OnItemSelectedListener if the same item is selected, so do it manually now
                OnItemSelectedListener?.OnItemSelected(this, SelectedView, position, SelectedItemId);
            }
        }

        public override void SetSelection(int position)
        {
            bool sameSelected = position == SelectedItemPosition;
            base.SetSelection(position);
            if (sameSelected)
            {
                // Spinner does not call the OnItemSelectedListener if the same item is selected, so do it manually now
                OnItemSelectedListener?.OnItemSelected(this, SelectedView, position, SelectedItemId);
            }
        }
    }
}