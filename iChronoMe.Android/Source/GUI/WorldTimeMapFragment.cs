using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.Widgets;

namespace iChronoMe.Droid.GUI
{
    public class WorldTimeMapFragment : ActivityFragment, IOnMapReadyCallback, IMenuItemOnMenuItemClickListener
    {
        static GoogleMap mGoogleMap = null;
        static MapView mMapView = null;
        HorizontalScrollView scrollView;
        static LinearLayout llInfoLayout;
        static FragmentActivity mActivity;
        static TimeType mTimeType = sys.DefaultTimeType;
        private Dictionary<string, WorldTimeItem> wtItems = new Dictionary<string, WorldTimeItem>();
        static bool bShowMilliSeconds = false;
        
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            mActivity = Activity;
            var view = (ViewGroup)inflater.Inflate(Resource.Layout.fragment_world_time_map, null);

            scrollView = view.FindViewById<HorizontalScrollView>(Resource.Id.scrollview);
            llInfoLayout = view.FindViewById<LinearLayout>(Resource.Id.bottom_layout);

            mMapView = (MapView)view.FindViewById(Resource.Id.mapView);
            MapsInitializer.Initialize(Context);

            mMapView = (MapView)view.FindViewById(Resource.Id.mapView);
            mMapView.OnCreate(savedInstanceState);
            mMapView.OnResume();// needed to get the map to display immediately
            mMapView.GetMapAsync(this);

            return view;
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            mGoogleMap = googleMap;

            LatLng posisiabsen = new LatLng(47.2813, 13.7255); ////your lat lng
            //mGoogleMap.AddMarker(new MarkerOptions() { Position = posisiabsen, Title = "Yout title");
            mGoogleMap.MoveCamera(CameraUpdateFactory.NewLatLng(posisiabsen));

            googleMap.UiSettings.ZoomControlsEnabled = true;
            googleMap.UiSettings.CompassEnabled = true;
            googleMap.UiSettings.MyLocationButtonEnabled = true;
            googleMap.UiSettings.MapToolbarEnabled = false;
            googleMap.UiSettings.IndoorLevelPickerEnabled = false;

            mGoogleMap.MapClick += MGoogleMap_MapClick;
            mGoogleMap.MarkerClick += MGoogleMap_MarkerClick;
            mGoogleMap.MarkerDragStart += MGoogleMap_MarkerDragStart;
            mGoogleMap.MarkerDrag += MGoogleMap_MarkerDrag;
            mGoogleMap.MarkerDragEnd += MGoogleMap_MarkerDragEnd;
        }

        private void MGoogleMap_MarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
        {
            FocusItem(e.Marker.Id);
        }
        private void MGoogleMap_MarkerDragStart(object sender, GoogleMap.MarkerDragStartEventArgs e)
        {
            FocusItem(e.Marker.Id);
        }

        private void MGoogleMap_MarkerDrag(object sender, GoogleMap.MarkerDragEventArgs e)
        {
            if (!wtItems.ContainsKey(e.Marker.Id))
                return;
            lock (wtItems)
            {
                var item = wtItems[e.Marker.Id];
                if (item != null)
                {
                    item.lth.ChangePositionDelay(e.Marker.Position.Latitude, e.Marker.Position.Longitude);
                    item.Update();
                }
            }            
        }

        private void MGoogleMap_MarkerDragEnd(object sender, GoogleMap.MarkerDragEndEventArgs e)
        {
            if (!wtItems.ContainsKey(e.Marker.Id))
                return;
            lock (wtItems)
            {
                var item = wtItems[e.Marker.Id];
                if (item != null)
                {
                    item.lth.ChangePositionDelay(e.Marker.Position.Latitude, e.Marker.Position.Longitude);
                    item.Update();
                }
            }
            FocusItem(e.Marker.Id);
        }

        bool bIsFirstClick = true;
        private void MGoogleMap_MapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            if (bIsFirstClick)
            {
                llInfoLayout.RemoveAllViews();
                bIsFirstClick = false;
            }

            var passchendaeleMarker = new MarkerOptions();
            passchendaeleMarker.SetPosition(e.Point);
            passchendaeleMarker.Draggable(true);
            var marker = mGoogleMap.AddMarker(passchendaeleMarker);

            var item = new WorldTimeItem(e.Point) { Marker = marker };
            lock (wtItems)
            {
                wtItems.Add(marker.Id, item);
            }
            item.Start();
            Task.Factory.StartNew(() =>
            {
                Task.Delay(100).Wait();
                mActivity.RunOnUiThread(() => FocusItem(marker.Id));
            });
        }

        private void FocusItem(string id)
        {
            if (!wtItems.ContainsKey(id))
                return;
            var item = wtItems[id];
            scrollView.SmoothScrollTo(item.InfoView.Left - item.InfoView.Width, 0);
            item.BlinkOnce();
        }

        const int menu_typetype_RealSunTime = 1101;
        const int menu_typetype_MiddleSunTime = 1102;
        const int menu_typetype_TimeZoneTime = 1103;

        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            base.OnPrepareOptionsMenu(menu);

            var sub = menu.AddSubMenu(0, 0, 100, Resources.GetString(Resource.String.TimeType));
            sub.SetIcon(MainWidgetBase.GetTimeTypeIcon(mTimeType, LocationTimeHolder.LocalInstance));
            sub.Item.SetShowAsAction(ShowAsAction.Always);
            IMenuItem item;
            if (mTimeType != TimeType.RealSunTime)
            {
                item = sub.Add(0, menu_typetype_RealSunTime, 0, Resources.GetString(Resource.String.TimeType_RealSunTime));
                item.SetIcon(MainWidgetBase.GetTimeTypeIcon(TimeType.RealSunTime, LocationTimeHolder.LocalInstance));
                item.SetOnMenuItemClickListener(this);
            }
            if (mTimeType != TimeType.MiddleSunTime)
            {
                item = sub.Add(0, menu_typetype_MiddleSunTime, 0, Resources.GetString(Resource.String.TimeType_MiddleSunTime));
                item.SetIcon(MainWidgetBase.GetTimeTypeIcon(TimeType.MiddleSunTime, LocationTimeHolder.LocalInstance));
                item.SetOnMenuItemClickListener(this);
            }
            if (mTimeType != TimeType.TimeZoneTime)
            {
                item = sub.Add(0, menu_typetype_TimeZoneTime, 0, Resources.GetString(Resource.String.TimeType_TimeZoneTime));
                item.SetIcon(MainWidgetBase.GetTimeTypeIcon(TimeType.TimeZoneTime, LocationTimeHolder.LocalInstance));
                item.SetOnMenuItemClickListener(this);
            }
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            if (item.ItemId == menu_typetype_RealSunTime)
                mTimeType = TimeType.RealSunTime;
            else if (item.ItemId == menu_typetype_MiddleSunTime)
                mTimeType = TimeType.MiddleSunTime;
            else if (item.ItemId == menu_typetype_TimeZoneTime)
                mTimeType = TimeType.TimeZoneTime;
            mActivity.InvalidateOptionsMenu();

            foreach (var wt in wtItems.Values)
            {
                wt.Update();
            }

            return true;
        }

        class WorldTimeItem
        {
            public LatLng Location { get; set; }
            public string Title { get; set; }
            public LocationTimeHolder lth { get; }
            public Marker Marker { get; set; }
            public TableLayout InfoView { get; }
            TextView tvArea, tvRDT, tvMST, tvTZT, tvRDToffset, tvMSToffset, tvTZToffset;

            public WorldTimeItem(LatLng location)
            {
                Location = location;
                lth = LocationTimeHolder.NewInstanceDelay(location.Latitude, location.Longitude);
                lth.AreaChanged += lth_AreaChanged;

                InfoView = new TableLayout(mActivity);
                InfoView.Clickable = true;
                InfoView.Click += InfoView_Click;
                InfoView.SetBackgroundResource(Resource.Drawable.selector);
                InfoView.SetColumnStretchable(2, true);
                int iPad1 = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 10, mActivity.Resources.DisplayMetrics);
                int iPad2 = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 5, mActivity.Resources.DisplayMetrics);

                tvArea = new TextView(mActivity) { Text = sys.DezimalGradToGrad(location.Latitude, location.Longitude) };
                tvArea.SetMaxLines(1);
                tvArea.Enabled = true;

                var row = new TableRow(mActivity);
                row.AddView(tvArea, new TableRow.LayoutParams() { Span = 3 });
                InfoView.AddView(row);

                tvRDT = new TextView(mActivity) { Text = "88:88:88" };
                tvRDT.SetPadding(iPad1, 0, 0, 0);
                tvMST = new TextView(mActivity) { Text = "88:88:88" };
                tvMST.SetPadding(iPad1, 0, 0, 0);
                tvTZT = new TextView(mActivity) { Text = "88:88:88" };
                tvTZT.SetPadding(iPad1, 0, 0, 0);

                tvRDToffset = new TextView(mActivity) { Text = "+-??:??" };
                tvRDToffset.SetPadding(iPad1, 0, iPad2, 0);
                tvMSToffset = new TextView(mActivity) { Text = "-??:??" };
                tvMSToffset.SetPadding(iPad1, 0, iPad2, 0);
                tvTZToffset = new TextView(mActivity) { Text = "~??:??" };
                tvTZToffset.SetPadding(iPad1, 0, iPad2, 0);

                row = new TableRow(mActivity);
                row.AddView(new TextView(mActivity) { Text = "real" }, 0);
                row.AddView(tvRDT, 1);
                row.AddView(tvRDToffset, 2);
                InfoView.AddView(row);

                row = new TableRow(mActivity);
                row.AddView(new TextView(mActivity) { Text = "mid" }, 0);
                row.AddView(tvMST, 1);
                row.AddView(tvMSToffset, 2);
                InfoView.AddView(row);

                row = new TableRow(mActivity);
                row.AddView(new TextView(mActivity) { Text = "zone" }, 0);
                row.AddView(tvTZT, 1);
                row.AddView(tvTZToffset, 2);
                InfoView.AddView(row);

                llInfoLayout.AddView(InfoView, new LinearLayout.LayoutParams((int)TypedValue.ApplyDimension(ComplexUnitType.Sp, 170, mActivity.Resources.DisplayMetrics), LinearLayout.LayoutParams.MatchParent));
            }

            private void InfoView_Click(object sender, EventArgs e)
            {
                Marker.ShowInfoWindow();
                mGoogleMap.AnimateCamera(CameraUpdateFactory.NewLatLng(Marker.Position));
            }

            private void lth_AreaChanged(object sender, AreaChangedEventArgs e)
            {
                mActivity.RunOnUiThread(() => {
                    try
                    {
                        if (Marker.Position.Latitude != lth.Latitude || Marker.Position.Longitude != lth.Longitude)
                            return;
                        string cText = string.IsNullOrEmpty(lth.AreaName) ? sys.DezimalGradToGrad(lth.Latitude, lth.Longitude) : lth.AreaName + ", " + lth.CountryName;
                        tvArea.Text = cText;
                        Marker.Title = cText;
                        Marker.ShowInfoWindow();
                    } catch { }
                });
            }

            string cTsFormat = bShowMilliSeconds ? @"mm\:ss\.fff" : @"mm\:ss";
            public void Start()
            {
                lth.StartTimeChangedHandler(this, TimeType.RealSunTime, (s, e) =>
                {
                    mActivity.RunOnUiThread(() =>
                    {
                        UpdateTime(tvRDT, tvRDToffset, TimeType.RealSunTime);
                    });
                });
                lth.StartTimeChangedHandler(this, TimeType.MiddleSunTime, (s, e) =>
                {
                    mActivity.RunOnUiThread(() =>
                    {
                        UpdateTime(tvMST, tvMSToffset, TimeType.MiddleSunTime);
                    });
                });
                lth.StartTimeChangedHandler(this, TimeType.TimeZoneTime, (s, e) =>
                {
                    mActivity.RunOnUiThread(() =>
                    {
                        UpdateTime(tvTZT, tvTZToffset, TimeType.TimeZoneTime);
                    });
                });

            }

            public void Stop()
            {
                lth.StopTimeChangedHandler(this);
            }

            public void Update()
            {
                mActivity.RunOnUiThread(() =>
                {
                    UpdateTime(tvRDT, tvRDToffset, TimeType.RealSunTime);
                    UpdateTime(tvMST, tvMSToffset, TimeType.MiddleSunTime);
                    UpdateTime(tvTZT, tvTZToffset, TimeType.TimeZoneTime);
                });
            }

            private void UpdateTime(TextView tvTime, TextView tvOffset, TimeType typeType)
            {
                DateTime tCurrent = lth.GetTime(mTimeType);
                DateTime tInfo = lth.GetTime(typeType);
                tvTime.Text = tInfo.ToLongTimeString();
                if (sys.GetTimeWithoutMilliSeconds(tCurrent) != sys.GetTimeWithoutMilliSeconds(tInfo))
                    tvOffset.Text = (tCurrent > tInfo ? "-" : "+") + (tInfo - tCurrent).ToString(cTsFormat);
                else
                    tvOffset.Text = "";
            }

            private bool isBlinking = false;
            internal void BlinkOnce()
            {
                if (isBlinking)
                    return;
                isBlinking = true;
                InfoView.SetBackgroundColor(Android.Graphics.Color.ParseColor("#A0FFFFFF"));
                Task.Factory.StartNew(() =>
                {
                    Task.Delay(500).Wait();
                    mActivity.RunOnUiThread(() => InfoView.SetBackgroundResource(Resource.Drawable.selector));
                    isBlinking = false;
                });
            }
        }
    }
}