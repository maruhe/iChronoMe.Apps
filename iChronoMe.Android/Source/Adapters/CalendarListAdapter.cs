using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using iChronoMe.DeviceCalendar;

namespace iChronoMe.Droid.Source.Adapters
{
    public class CalendarListAdapter : BaseAdapter
    {
        Activity mContext;
        private Dictionary<string, Calendar> Items = new Dictionary<string, Calendar>();

        public CalendarListAdapter(Activity context)
        {
            mContext = context;

            Task.Factory.StartNew(async () =>
            {
                var cals = await DeviceCalendar.DeviceCalendar.GetCalendarsAsync();
                List<string> cS = new List<string>();
                foreach (var cal in cals)
                {
                    Items.Add(cal.AccountName + "_" + cal.Name, cal);
                }
                this.NotifyDataSetChanged();
            });
        }

        public override int Count => Items.Count;

        public override Java.Lang.Object GetItem(int position)
        {
            return position;
        }

        public override long GetItemId(int position)
        {
            return position;
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
            ItemClick(sender, e.Position);
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
    }
}