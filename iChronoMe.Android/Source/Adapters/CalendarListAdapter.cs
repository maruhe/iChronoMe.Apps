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
        private bool _primaryOnly = true;
        public bool PrimaryOnly { get => _primaryOnly; set { _primaryOnly = value; refresh(); } }

        private int _secondaryCount = 0;
        public bool HasSecondary { get => _secondaryCount > 0; }

        private ListView parentListView;
        private ImageView btnExpand;

        public CalendarListAdapter(Activity context)
        {
            mContext = context;
            refresh();
        }

        private void refresh()
        {
            Task.Factory.StartNew(async () =>
            {
                _secondaryCount = 0;
                var cals = await DeviceCalendar.DeviceCalendar.GetCalendarsAsync();
                List<string> cS = new List<string>();
                foreach (var cal in cals)
                {
                    if (!_primaryOnly || cal.IsPrimary)
                        Items.Add(cal.AccountName + "_" + cal.Name, cal);
                    if (!cal.IsPrimary)
                        _secondaryCount++;
                }
                mContext.RunOnUiThread(() =>
                {
                    this.NotifyDataSetChanged();
                    return;
                    try
                    {
                        if (_secondaryCount == 0)
                            return;
                        if (parentListView == null)
                            return;
                        if (!(parentListView.Parent.Parent is RelativeLayout))
                            return;
                        if (btnExpand == null)
                        {
                            btnExpand = new ImageView(mContext);
                            var lp = new RelativeLayout.LayoutParams(30, 80);
                            lp.AddRule(LayoutRules.AlignParentEnd);
                            lp.AddRule(LayoutRules.Below, Resource.Id.lv_calendars);

                            btnExpand.SetImageResource(Resource.Drawable.icons8_delete);

                            (parentListView.Parent.Parent as RelativeLayout).AddView(btnExpand, lp);
                        }
                    } catch { }
                });
                
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