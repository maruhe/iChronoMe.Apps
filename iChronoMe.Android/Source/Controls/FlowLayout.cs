using System;

using Android.Content;
using Android.Util;
using Android.Views;

namespace iChronoMe.Droid.Controls
{
    // Custom layout that wraps child views to a new line
    public class FlowLayout : ViewGroup
    {

        private int marginHorizontal;
        private int marginVertical;

        public FlowLayout(Context context) : base(context)
        {
            init();
        }

        public FlowLayout(Context context, IAttributeSet attrs) : this(context, attrs, 0)
        {

        }

        public FlowLayout(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            init();
        }

        private void init()
        { // Specify the margins for the children
            marginHorizontal = 5;
            marginVertical = 5;
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            int childLeft = PaddingLeft;
            int childTop = PaddingTop;
            int lowestBottom = 0;
            int lineHeight = 0;
            int myWidth = ResolveSize(100, widthMeasureSpec);
            int wantedHeight = 0;

            for (int i = 0; i < ChildCount; i++)
            {
                View child = GetChildAt(i);
                if (child.Visibility == ViewStates.Gone)
                {
                    continue;
                }

                child.Measure(GetChildMeasureSpec(widthMeasureSpec, 0, child.LayoutParameters.Width),
                        GetChildMeasureSpec(heightMeasureSpec, 0, child.LayoutParameters.Height));
                int childWidth = child.MeasuredWidth;
                int childHeight = child.MeasuredHeight;
                lineHeight = Math.Max(childHeight, lineHeight);

                if (childWidth + childLeft + PaddingRight > myWidth)
                { // Wrap this line
                    childLeft = PaddingLeft;
                    childTop = marginVertical + lowestBottom; // Spaced below the previous lowest point
                    lineHeight = childHeight;
                }
                childLeft += childWidth + marginHorizontal;

                if (childHeight + childTop > lowestBottom)
                { // New lowest point
                    lowestBottom = childHeight + childTop;
                }
            }

            wantedHeight += childTop + lineHeight + PaddingBottom;
            SetMeasuredDimension(myWidth, ResolveSize(wantedHeight, heightMeasureSpec));
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            int childLeft = PaddingLeft;
            int childTop = PaddingTop;
            int lowestBottom = 0;
            int myWidth = right - left;
            for (int i = 0; i < ChildCount; i++)
            {
                View child = GetChildAt(i);
                if (child.Visibility == ViewStates.Gone)
                {
                    continue;
                }
                int childWidth = child.MeasuredWidth;
                int childHeight = child.MeasuredHeight;

                if (childWidth + childLeft + PaddingRight > myWidth)
                { // Wrap this line
                    childLeft = PaddingLeft;
                    childTop = marginVertical + lowestBottom; // Spaced below the previous lowest point
                }
                child.Layout(childLeft, childTop, childLeft + childWidth, childTop + childHeight);
                childLeft += childWidth + marginHorizontal;

                if (childHeight + childTop > lowestBottom)
                { // New lowest point
                    lowestBottom = childHeight + childTop;
                }
            }
        }
    }
}