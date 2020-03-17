using System;
using System.Linq;

using Android.App;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.Adapters
{
    public class ActiveCalendarListAdapter : CalendarListAdapter
    {
        public ActiveCalendarListAdapter(Activity context) : base(context)
        {

        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = Items.Values.ElementAt(position);

            if (convertView == null)
            {
                convertView = mContext.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItemChecked, null);
            }

            bool bIsActive = !AppConfigHolder.CalendarViewConfig.HideCalendars.Contains(item.ExternalID);
            convertView.FindViewById<CheckedTextView>(Android.Resource.Id.Text1).Text = item.Name;
            convertView.FindViewById<CheckedTextView>(Android.Resource.Id.Text1).Checked = bIsActive;
            if (parent is ListView)
                ((ListView)parent).SetItemChecked(position, bIsActive);

            if (parentListView == null && parent is ListView)
                parentListView = (ListView)parent;
            /*
            try
            {
                convertView.FindViewById<ImageView>(Resource.Id.icon).SetImageDrawable(
                    mContext.PackageManager.GetApplicationIcon(item.OwnerAccount));
            } catch { }
            convertView.FindViewById<CheckedTextView>(Resource.Id.title).Text = item.Name;
            convertView.FindViewById<CheckedTextView>(Resource.Id.title).Checked = bIsActive;
            if (parent is ListView)
                ((ListView)parent).SetItemChecked(position, bIsActive);
                */

            return convertView;
        }

        public void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            //ItemClick(sender, e.Position);
        }

        public void ItemClick(object sender, int position)
        {
            var cal = Items.Values.ElementAt(position);
            if (AppConfigHolder.CalendarViewConfig.HideCalendars.Contains(cal.ExternalID))
                AppConfigHolder.CalendarViewConfig.HideCalendars.Remove(cal.ExternalID);
            else
                AppConfigHolder.CalendarViewConfig.HideCalendars.Add(cal.ExternalID);
            AppConfigHolder.SaveCalendarViewConfig();
            NotifyDataSetChanged();
            HiddenCalendarsChanged?.Invoke(sender, new EventArgs());
        }

        public event EventHandler HiddenCalendarsChanged;
        private ListView parentListView;
        private ImageView btnExpand;
    }
}