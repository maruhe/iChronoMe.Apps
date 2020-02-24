using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using Java.Interop;

namespace iChronoMe.Droid.Adapters
{
    public class CalendarEventTitleAdapter : CursorAdapter
    {
        ContentResolver mContent;

        public CalendarEventTitleAdapter(Context context, ICursor c) : base(context, c, true)
        {
            mContent = context.ContentResolver;
        }

        public override Java.Lang.ICharSequence ConvertToStringFormatted(ICursor cursor)
        {
            return new Java.Lang.String(cursor.GetString(1));
        }

        public override void BindView(View view, Context context, ICursor cursor)
        {
            ((TextView)view).Text = cursor.GetString(1);
        }

        public override View NewView(Context context, ICursor cursor, ViewGroup parent)
        {
            var inflater = LayoutInflater.From(context);
            var view = (TextView)inflater.Inflate(Android.Resource.Layout.SimpleDropDownItem1Line, parent, false);
            view.Text = cursor.GetString(1);
            return view;
        }

        public override ICursor RunQueryOnBackgroundThread(Java.Lang.ICharSequence constraint)
        {
            return doRunQueryOnBackgroundThread(constraint == null ? null : constraint.ToString());
        }

        public ICursor doRunQueryOnBackgroundThread(string constraint)
        {
            if (FilterQueryProvider != null)
            {
                this.ToString();
                ;//return getFilterQueryProvider().runQuery(constraint);
            }

            System.Text.StringBuilder buffer = null;
            System.String[] args = null;

            if (string.IsNullOrEmpty(constraint))
                return null;

            if (constraint != null)
            {
                try
                {
                    constraint = constraint.ToString().Trim();
                    buffer = new System.Text.StringBuilder();
                    buffer.Append("UPPER(").Append(CalendarContract.Events.InterfaceConsts.Title).Append(") GLOB ? ");
                    buffer.Append(")) GROUP BY ((").Append(CalendarContract.Events.InterfaceConsts.Title);

                    System.String cSearch = constraint.ToUpper();
                    cSearch = cSearch.Replace("  ", " ");
                    cSearch = cSearch.Replace(" ", "* *");
                    cSearch = "*" + cSearch + "*";

                    args = new System.String[] { cSearch };

                    var cur = mContent.Query(CalendarContract.Events.ContentUri, new string[] { CalendarContract.Events.InterfaceConsts.Id, CalendarContract.Events.InterfaceConsts.Title }, buffer == null ? null : buffer.ToString(), args, CalendarContract.Events.InterfaceConsts.Title);
                    return cur;
                } 
                catch (System.Exception ex)
                {
                    sys.LogException(ex);
                }
            }

            return null;
        }
    }
}