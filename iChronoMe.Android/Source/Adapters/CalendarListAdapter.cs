using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Android.App;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using iChronoMe.Core.Types;
using iChronoMe.DeviceCalendar;

namespace iChronoMe.Droid.Adapters
{
    public class CalendarListAdapter : BaseAdapter
    {
        protected Activity mContext { get; }
        protected Dictionary<string, Calendar> Items = new Dictionary<string, Calendar>();
        public bool IsReady { get; private set; } = false;

        private int _secondaryCount = 0;
        public bool HasSecondary { get => _secondaryCount > 0; }

        private CalendarFilter _filter;
        public CalendarFilter Filter
        {
            get => _filter;
            set
            {
                if (_filter != value)
                {
                    _filter = value;
                    Refresh();
                }
            }
        }

        public CalendarListAdapter(Activity context, CalendarFilter initFilter = CalendarFilter.PrimaryOnly)
        {
            mContext = context;
            _filter = initFilter;
            Refresh();
        }

        public void Refresh()
        {
            Task.Factory.StartNew(async () =>
            {
                _secondaryCount = 0;
                var cals = await DeviceCalendar.DeviceCalendar.GetCalendarsAsync();
                List<string> cS = new List<string>();
                lock (Items)
                {
                    Items.Clear();
                    foreach (var cal in cals)
                    {
                        if (cal.IsPrimary)
                            Items.Add(cal.AccountName + "_" + cal.Name, cal);
                    }
                    if (Filter == CalendarFilter.AllEditable || Filter == CalendarFilter.AllCalendars)
                    {
                        foreach (var cal in cals)
                        {
                            if (cal.CanEditEvents && !Items.ContainsValue(cal))
                            {
                                Items.Add(cal.AccountName + "_" + cal.Name, cal);
                                _secondaryCount++;
                            }
                        }
                    }
                    if (Filter == CalendarFilter.AllCalendars)
                    {
                        foreach (var cal in cals)
                        {
                            if (!cal.IsPrimary && !Items.ContainsValue(cal))
                            {
                                Items.Add(cal.AccountName + "_" + cal.Name, cal);
                                _secondaryCount++;
                            }
                        }
                    }

                    IsReady = true;
                    mContext.RunOnUiThread(() =>
                    {
                        this.NotifyDataSetChanged();
                        ItemsLoadet?.Invoke(this, new EventArgs());
                    });
                }
            });
        }

        public int GetCalendarPosition(string calendarId)
        {
            if (string.IsNullOrEmpty(calendarId))
                return -1;
            int i = 0;
            foreach (var cal in Items.Values)
            {
                if (cal.ExternalID.Equals(calendarId))
                    return i;
                i++;
            }
            return -1;
        }

        public event EventHandler ItemsLoadet;

        public override int Count => Items.Count;

        public override Java.Lang.Object GetItem(int position) => Items.Values.ElementAt(position).ExternalID;

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = Items.Values.ElementAt(position);

            if (convertView == null)
            {
                convertView = mContext.LayoutInflater.Inflate(Resource.Layout.listitem_title, null);
                convertView.SetMinimumHeight(sys.DpPx(48));
            }

            convertView.FindViewById<ImageView>(Resource.Id.icon).SetImageDrawable(DrawableHelper.GetIconDrawable(mContext, Resource.Drawable.circle_shape, xColor.FromHex(item.Color, xColor.MaterialBlue)));
            convertView.FindViewById<TextView>(Resource.Id.title).Text = item.Name;

            return convertView;
        }
        public enum CalendarFilter
        {
            PrimaryOnly,
            AllEditable,
            AllCalendars
        }
    }
}