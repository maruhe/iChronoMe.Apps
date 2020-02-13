using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.App;
using Android.Util;
using Android.Views;
using Android.Widget;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.Types;
using iChronoMe.Droid.Widgets;
using static iChronoMe.Core.Classes.GeoInfo;

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
        static List<Marker> markers = new List<Marker>();
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
            markers.Add(marker);

            var item = new WorldTimeItem(marker);
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

        const int menu_typetype_Debug_AreaCache = 1001;
        const int menu_typetype_Debug_ZonesOverlay = 1002;
        const int menu_typetype_RealSunTime = 1101;
        const int menu_typetype_MiddleSunTime = 1102;
        const int menu_typetype_TimeZoneTime = 1103;

        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            base.OnPrepareOptionsMenu(menu);

#if DEBUG
            var dsub = menu.AddSubMenu(0, 0, 10, "Debug");
            dsub.Item.SetShowAsAction(ShowAsAction.Always);


            var ditem = dsub.Add(0, menu_typetype_Debug_AreaCache, 0, "AreaCache");
            ditem.SetShowAsAction(ShowAsAction.Always);
            ditem.SetOnMenuItemClickListener(this);

            ditem = dsub.Add(0, menu_typetype_Debug_ZonesOverlay, 0, "TZ-Overlay");
            ditem.SetShowAsAction(ShowAsAction.Always);
            ditem.SetOnMenuItemClickListener(this);
#endif

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
            if (item.ItemId == menu_typetype_Debug_AreaCache)
            {
                foreach (var wt in wtItems.Values)
                {
                    wt.Stop();
                }
                wtItems.Clear();
                llInfoLayout.RemoveAllViews();

                mGoogleMap.Clear();
                DrawAreaCache();
                return true;
            };
            if (item.ItemId == menu_typetype_Debug_ZonesOverlay)
            {
                DrawZonesOverlay();
                return true;
            };

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

        private void DrawZonesOverlay()
        {
            var rnd = new Random();

            Task.Factory.StartNew(async () =>
            {
                int ip = 0;
                foreach (var p in TimeZoneMap.timeZonePolygons.Keys)
                {
                    try
                    {
                        List<PolygonOptions> mPolygonsTimezone = new List<PolygonOptions>();

                        ip++;
                        //if (ip % 5 != 0)
                          //  continue;
                        //if (!p.Contains(new NetTopologySuite.Geometries.Point(markers[0].Position.Latitude, markers[0].Position.Longitude))
                        //    && !p.Contains(new NetTopologySuite.Geometries.Point(markers[1].Position.Latitude, markers[1].Position.Longitude)))
                        //continue;

                        var polygon1 = new PolygonOptions();
                        int i = 0;
                        foreach (var x in p.Coordinates)
                        {
                            i++;
                            if (i % 3 == 0)
                                polygon1.Add(new LatLng(x.X, x.Y));
                        }
                        polygon1.InvokeStrokeWidth(2f);
                        polygon1.InvokeStrokeColor(Color.HotPink);
                        polygon1.InvokeFillColor(Color.Transparent);// xColor.FromRgba(rnd.Next(200), rnd.Next(200), rnd.Next(200), 120).ToAndroid());
                        mPolygonsTimezone.Add(polygon1);

                        mActivity.RunOnUiThread(() =>
                        {

                            try
                            {
                                mActivity.Title = ip.ToString();

                                PolygonOptions pgLast = null;
                                foreach (PolygonOptions pg in mPolygonsTimezone)
                                {
                                    mGoogleMap.AddPolygon(pg);
                                    pgLast = pg;
                                }
                            }
                            catch (Exception ex)
                            {
                                Tools.ShowToast(mActivity, "Main: " + ex.Message);
                            }
                        });
                    }
                    catch(Exception exx)
                    {
                        Tools.ShowToast(mActivity, "Loader: " + exx.Message);
                    }
                }
            });
        }

        private void DrawAreaCache()
        {
            List<PolygonOptions> mPolygonsArea = new List<PolygonOptions>();
            List<PolygonOptions> mPolygonsTimezone = new List<PolygonOptions>();
            List<MarkerOptions> mPinS = new List<MarkerOptions>();

            Task.Factory.StartNew(async () =>
            {
                var cache2 = db.dbAreaCache.Query<TimeZoneInfoCache>("select * from TimeZoneInfoCache limit 0,150", new object[0]);
                foreach (TimeZoneInfoCache ti in cache2)
                {
                    var polygon1 = new PolygonOptions();
                    polygon1.Add(new LatLng(ti.boxNorth, ti.boxWest));
                    polygon1.Add(new LatLng(ti.boxNorth, ti.boxEast));
                    polygon1.Add(new LatLng(ti.boxSouth, ti.boxEast));
                    polygon1.Add(new LatLng(ti.boxSouth, ti.boxWest));
                    polygon1.Add(new LatLng(ti.boxNorth, ti.boxWest));
                    polygon1.InvokeStrokeWidth(1f);
                    polygon1.InvokeStrokeColor(Color.Blue);
                    polygon1.InvokeFillColor(Color.Transparent);
                    mPolygonsTimezone.Add(polygon1);
                    /*mPinS.Add(new Pin()
                    {
                        Position = new Position(ti.boxNorth, ti.boxWest),
                        Label = ti.timezoneId
                    });*/
                }

                var cache = db.dbAreaCache.Query<AreaInfo>("select * from AreaInfo limit 0,150", new object[0]);
                foreach (AreaInfo ai in cache)
                {

                    var polygon1 = new PolygonOptions();
                    polygon1.Add(new LatLng(ai.boxNorth, ai.boxWest));
                    polygon1.Add(new LatLng(ai.boxNorth, ai.boxEast));
                    polygon1.Add(new LatLng(ai.boxSouth, ai.boxEast));
                    polygon1.Add(new LatLng(ai.boxSouth, ai.boxWest));
                    polygon1.Add(new LatLng(ai.boxNorth, ai.boxWest));
                    polygon1.InvokeStrokeWidth(1f);
                    polygon1.InvokeStrokeColor(Color.Red);
                    polygon1.InvokeFillColor(Color.Transparent);
                    mPolygonsArea.Add(polygon1);
                    var m = new MarkerOptions();
                    m.SetPosition(new LatLng(ai.centerLat, ai.centerLong));
                    m.SetTitle(ai.toponymName);
                    mPinS.Add(m);
                }

                mActivity.RunOnUiThread(() =>
                {

                    mActivity.Title = mPolygonsArea.Count.ToString();

                    PolygonOptions pgLast = null;
                    foreach (PolygonOptions pg in mPolygonsTimezone)
                    {
                        mGoogleMap.AddPolygon(pg);
                        pgLast = pg;
                    }
                    foreach (PolygonOptions pg in mPolygonsArea)
                    {
                        mGoogleMap.AddPolygon(pg);
                        pgLast = pg;
                    }
                    foreach (var p in mPinS)
                        mGoogleMap.AddMarker(p);
                });
            });
        }

        class WorldTimeItem
        {
            public LatLng Location { get; set; }
            public string Title { get; set; }
            public LocationTimeHolder lth { get; }
            public Marker Marker { get; set; }
            public TableLayout InfoView { get; }
            TextView tvArea, tvRDT, tvMST, tvTZT, tvRDToffset, tvMSToffset, tvTZToffset;
            public string ID { get; }
            static string blinkingID;

            public WorldTimeItem(Marker marker)
            {
                ID = marker.Id;
                Marker = marker;
                Location = marker.Position;
                lth = LocationTimeHolder.NewInstanceDelay(Location.Latitude, Location.Longitude);
                lth.AreaChanged += lth_AreaChanged;

                InfoView = new TableLayout(mActivity);
                InfoView.Clickable = true;
                InfoView.Click += InfoView_Click;
                InfoView.SetBackgroundResource(Resource.Drawable.selector);
                InfoView.SetColumnStretchable(2, true);
                int iPad1 = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 10, mActivity.Resources.DisplayMetrics);
                int iPad2 = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 5, mActivity.Resources.DisplayMetrics);

                tvArea = new TextView(mActivity) { Text = sys.DezimalGradToGrad(Location.Latitude, Location.Longitude) };
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
                mActivity.RunOnUiThread(() =>
                {
                    try
                    {
                        if (Marker.Position.Latitude != lth.Latitude || Marker.Position.Longitude != lth.Longitude)
                            return;
                        string cText = string.IsNullOrEmpty(lth.AreaName) ? sys.DezimalGradToGrad(lth.Latitude, lth.Longitude) : lth.AreaName + ", " + lth.CountryName;
                        tvArea.Text = cText;
                        Marker.Title = cText;
                        Marker.ShowInfoWindow();
                    }
                    catch { }
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
                blinkingID = ID;
                isBlinking = true;
                InfoView.SetBackgroundColor(Android.Graphics.Color.ParseColor("#A0FFFFFF"));
                Task.Factory.StartNew(() =>
                {
                    DateTime tEnd = DateTime.Now.AddMilliseconds(500);
                    while (blinkingID == ID && tEnd > DateTime.Now)
                        Task.Delay(25).Wait();
                    mActivity.RunOnUiThread(() => InfoView.SetBackgroundResource(Resource.Drawable.selector));
                    isBlinking = false;
                });
            }
        }
    }
}