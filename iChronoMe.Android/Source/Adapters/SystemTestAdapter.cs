﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.DeviceCalendar;
using static iChronoMe.Core.Classes.GeoInfo;

namespace iChronoMe.Droid.Source.Adapters
{
    public class SystemTestAdapter : BaseAdapter
    {
        Activity mContext;
        List<SystemTest> Items = new List<SystemTest>();
        Dictionary<SystemTest, TestStatus> ItemStatus = new Dictionary<SystemTest, TestStatus>();
        Dictionary<SystemTest, string> ItemInfos = new Dictionary<SystemTest, string>();

        public SystemTestAdapter(Activity context)
        {
            mContext = context;

            foreach (var e in Enum.GetValues(typeof(SystemTest)))
                Items.Add((SystemTest)e);
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
            var item = Items[position];

            if (convertView == null)
            {
                convertView = mContext.LayoutInflater.Inflate(Resource.Layout.listitem_process, null);
            }

            convertView.FindViewById<TextView>(Resource.Id.title).Text = item.ToString();

            if (ItemStatus.ContainsKey(item))
            {
                var status = ItemStatus[item];
                if (status != TestStatus.None)
                {
                    var progress = convertView.FindViewById<ProgressBar>(Resource.Id.progress);
                    var icon = convertView.FindViewById<ImageView>(Resource.Id.icon);
                    if (status == TestStatus.Running)
                    {
                        progress.Visibility = ViewStates.Visible;
                        icon.Visibility = ViewStates.Gone;
                    } 
                    else
                    {
                        icon.Visibility = ViewStates.Visible;
                        progress.Visibility = ViewStates.Gone;
                        int imgRes = Resource.Drawable.icons8_summer;
                        if (status == TestStatus.OK)
                            imgRes = Resource.Drawable.icons8_add_green;
                        else if (status == TestStatus.Error)
                            imgRes = Resource.Drawable.icons8_delete;
                        icon.SetImageResource(imgRes);
                    }
                }
                var desc = convertView.FindViewById<TextView>(Resource.Id.description);
                if (ItemInfos.ContainsKey(item))
                {
                    desc.Text = ItemInfos[item];
                    desc.Visibility = ViewStates.Visible;
                }
                else
                    desc.Visibility = ViewStates.Gone;
            }
            return convertView;
        }

        public void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            ItemClick(sender, e.Position);
        }

        public void ItemClick(object sender, int position)
        {

        }

        internal void StartSystemTest()
        {
            Task.Factory.StartNew(async() =>
            {
                foreach (var test in Items)
                    await RunSystemTest(test);
            });
        }

        Location lastLocation;

        private async Task RunSystemTest(SystemTest test)
        {
            if (ItemStatus.ContainsKey(test))
                ItemStatus.Remove(test);
            if (ItemInfos.ContainsKey(test))
                ItemInfos.Remove(test);
            ItemStatus.Add(test, TestStatus.Running);
            ItemInfos.Add(test, "init...");
            mContext.RunOnUiThread(() => NotifyDataSetChanged());
            List<string> cErrors = new List<string>();

            var locationManager = (LocationManager)mContext.GetSystemService(Context.LocationService);

            try
            {
                switch (test)
                {
                    case SystemTest.LocationState:
                        SetInfo(test, "check location...");
                        float nMin = 100000;
                        lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);
                        if (lastLocation == null)
                        {
                            SetInfo(test, "unable to get location from network!");
                            cErrors.Add("unable to get location from network");
                            await Task.Delay(1500);
                        }
                        else
                        {
                            nMin = Math.Min(nMin, lastLocation.Accuracy);
                            SetInfo(test, "1/3 got network location +-" + lastLocation.Accuracy + "meters");
                            await Task.Delay(500);
                            lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);
                            nMin = Math.Min(nMin, lastLocation.Accuracy);
                            SetInfo(test, "2/3 got network location +-" + lastLocation.Accuracy + "meters");
                            await Task.Delay(500);
                            lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);
                            nMin = Math.Min(nMin, lastLocation.Accuracy);
                            SetInfo(test, "3/3 got network location +-" + lastLocation.Accuracy + "meters");
                            await Task.Delay(500);
                        }
                        SetInfo(test, "try gps...");
                        lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);
                        if (lastLocation == null)
                        {
                            SetInfo(test, "unable to get location from gps!");
                            cErrors.Add("unable to get location from gps");
                            await Task.Delay(1500);
                            break;
                        }
                        else
                        {
                            nMin = Math.Min(nMin, lastLocation.Accuracy);
                            SetInfo(test, "1/3 got gps location +-" + lastLocation.Accuracy + "meters");
                            await Task.Delay(500);
                            lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);
                            nMin = Math.Min(nMin, lastLocation.Accuracy);
                            SetInfo(test, "2/3 got gps location +-" + lastLocation.Accuracy + "meters");
                            await Task.Delay(500);
                            lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);
                            nMin = Math.Min(nMin, lastLocation.Accuracy);
                            SetInfo(test, "3/3 got gps location +-" + lastLocation.Accuracy + "meters");
                            await Task.Delay(500);
                        }
                        if (nMin < 1000)
                        {
                            SetInfo(test, "best location  was +-" + nMin + "meters");
                            await Task.Delay(500);
                        }
                        break;

                    case SystemTest.AreaInfo:
                        SetInfo(test, "check area...");
                        lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);
                        if (lastLocation == null)
                            lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);

                        if (lastLocation == null)
                        {
                            SetInfo(test, "no location, no area...");
                            cErrors.Add("no location, no area...");
                            await Task.Delay(1500);
                            break;
                        }
                        var ai = GeoInfo.GetAreaInfo(lastLocation.Latitude, lastLocation.Longitude, false);
                        if (ai == null)
                        {
                            SetInfo(test, "unable to get area info...");
                            cErrors.Add("unable to get area info...");
                            await Task.Delay(1500);
                            break;
                        }
                        SetInfo(test, "area found: " + ai.toponymName + ", " + ai.adminArea1 + ", " + ai.adminArea2 + ", " + ai.countryName);
                        await Task.Delay(500);
                        break;

                    case SystemTest.TimeZoneInfo:
                        SetInfo(test, "check timezone...");                        
                        lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);
                        if (lastLocation == null)
                            lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);

                        if (lastLocation == null)
                        {
                            SetInfo(test, "no location, no area...");
                            cErrors.Add("no location, no area...");
                            await Task.Delay(1500);
                            break;
                        }
                        var ti = TimeZoneInfoCache.OnlineFromLocation(lastLocation.Latitude, lastLocation.Longitude);
                        if (ti == null)
                        {
                            SetInfo(test, "unable to get timezone info...");
                            cErrors.Add("unable to get timezone info...");
                            await Task.Delay(1500);
                            break;
                        }

                        var tiSys = TimeZoneInfo.FindSystemTimeZoneById(ti.timezoneId);
                        if (tiSys == null)
                        {
                            SetInfo(test, "no system-timezone found for "+ti.timezoneId);
                            cErrors.Add("no system-timezone found for " + ti.timezoneId);
                            await Task.Delay(1500);
                            break;
                        }

                        var lth = LocationTimeHolder.NewInstanceOffline(lastLocation.Latitude, lastLocation.Longitude, ti.timezoneId);
                        var tsDiff = lth.TimeZoneTime - DateTime.Now;
                        long ticks = tsDiff.Ticks;
                        if (ticks < 0)
                            ticks = ticks * -1;
                        long offsetTicks = TimeHolder.mLastNtpDiff.Ticks;
                        if (offsetTicks < 0)
                            offsetTicks = offsetTicks * -1;
                        long lthOffset = ticks - offsetTicks;
                        if (lthOffset < 0)
                            lthOffset = lthOffset * -1;

                        if (TimeSpan.FromTicks(lthOffset) > TimeSpan.FromMinutes(5))
                        {
                            SetInfo(test, "timezone looks fine but offset is " + TimeSpan.FromTicks(lthOffset));
                            cErrors.Add("timezone looks fine but offset is " + TimeSpan.FromTicks(lthOffset));
                            await Task.Delay(1500);
                        }
                        SetInfo(test, "timezone found: " + tiSys.DisplayName + ", GMT " + ti.gmtOffset.ToString("+#;-#;0") + " DST " + ti.dstOffset.ToString("+#;-#;0"));
                        await Task.Delay(2500);
                        break;

                    case SystemTest.CalendarAccess:
                        SetInfo(test, "check calendars...");

                        var calendars = await DeviceCalendar.DeviceCalendar.GetCalendarsAsync();
                        var calendarsw = await DeviceCalendar.DeviceCalendar.GetEditableCalendarsAsync();
                        var def = await DeviceCalendar.DeviceCalendar.GetDefaultCalendar();

                        if (calendars.Count == 0)
                        {
                            SetInfo(test, "no calendar found...");
                            cErrors.Add("no calendar found...");
                            await Task.Delay(1500);
                            break;
                        }
                        if (calendarsw.Count == 0)
                        {
                            SetInfo(test, calendars.Count+" calendars found, but no editable...");
                            cErrors.Add(calendars.Count + " calendars found, but no editable...");
                            await Task.Delay(1500);
                            break;
                        }
                        if (def == null)
                        {
                            SetInfo(test, calendars.Count + " calendars found, "+ calendarsw.Count+ " editable, but no default...");
                            cErrors.Add(calendars.Count + " calendars found, " + calendarsw.Count + " editable, but no default...");
                            await Task.Delay(1500);
                            break;
                        }
                        SetInfo(test, calendars.Count + " calendars found, " + calendarsw.Count + " editable, default is " + def.Name);
                        await Task.Delay(500);
                        break;

                    case SystemTest.CalendarData:
                        SetInfo(test, "check calendar data...");

                        var cals = await DeviceCalendar.DeviceCalendar.GetCalendarsAsync();

                        if (cals.Count == 0)
                        {
                            SetInfo(test, "no calendar found...");
                            cErrors.Add("no calendar found...");
                            await Task.Delay(1500);
                            break;
                        }
                        int iEventCount = 0;
                        foreach (var cal in cals)
                        {
                            var events = await DeviceCalendar.DeviceCalendar.GetEventsAsync(cal, DateTime.Today.AddDays(-90), DateTime.Today.AddDays(90));
                            iEventCount += events.Count;
                        }
                        if (iEventCount == 0)
                        {
                            SetInfo(test, "no events found in " + cals.Count + " calendars...");
                            cErrors.Add("no events found in " + cals.Count + " calendars...");
                            await Task.Delay(1500);
                            break;
                        }
                        SetInfo(test, iEventCount+" events found in " + cals.Count + " calendars...");
                        await Task.Delay(500);

                        break;
                }
            }
            catch (Exception ex)
            {
                SetInfo(test, ex.Message);
                cErrors.Add(ex.Message);
                await Task.Delay(1500);
            }

            if (cErrors.Count == 0)
            {
                ItemStatus[test] = TestStatus.OK;
            }
            else
            {
                ItemStatus[test] = TestStatus.Error;
                ItemInfos[test] = cErrors.Count+" Errors :-(";
            }
            mContext.RunOnUiThread(() => NotifyDataSetChanged());
        }

        private void SetInfo(SystemTest test, string info)
        {
            ItemInfos[test] = info;
            mContext.RunOnUiThread(() => NotifyDataSetChanged());
        }

        enum SystemTest
        {
            LocationState,
            AreaInfo,
            TimeZoneInfo,
            CalendarAccess,
            CalendarData
        }

        enum TestStatus
        {
            None = 0,
            Running = 10,
            OK = 15,
            Error = 20
        }
    }
}