using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Gms.Maps.Utils.Data.GeoJson;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.App;
using Android.Util;
using Android.Views;
using Android.Widget;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.Types;

using Org.Json;

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
        static private Dictionary<string, WorldTimeItem> wtItems;
        static List<Marker> markers;
        static bool bShowMilliSeconds = false;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            wtItems = new Dictionary<string, WorldTimeItem>();
            markers = new List<Marker>();

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

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutInt("marker_count", wtItems.Count);
            int i = 0;
            foreach (var item in wtItems.Values)
            {
                outState.PutDouble("marker_" + i + "_lat", item.Location.Latitude);
                outState.PutDouble("marker_" + i + "_lng", item.Location.Longitude);
                i++;
            }
        }

        public override void OnPause()
        {
            base.OnPause();
            mGoogleMap = null;
        }

        Bundle blRestore;
        public override void OnViewStateRestored(Bundle savedInstanceState)
        {
            base.OnViewStateRestored(savedInstanceState);
            if (savedInstanceState != null)
                blRestore = new Bundle(savedInstanceState);
        }

        private void RestoreItems(Bundle savedInstanceState)
        {
            if (savedInstanceState == null)
                return;
            RemoveAllItems();
            bIsFirstClick = false;
            int iCount = savedInstanceState.GetInt("marker_count", 0);
            for (int i = 0; i < iCount; i++)
            {
                var lat = savedInstanceState.GetDouble("marker_" + i + "_lat", 0);
                var lng = savedInstanceState.GetDouble("marker_" + i + "_lng", 0);
                if (lat != 0 && lng != 0)
                    AddLocation(new LatLng(lat, lng));
            }
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            mGoogleMap = googleMap;

            LatLng posisiabsen = new LatLng(sys.lastUserLocation.Latitude, sys.lastUserLocation.Longitude);
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

            RemoveAllItems();
            if (blRestore != null)
                RestoreItems(blRestore);
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
        }

        bool bIsFirstClick = true;
        private void MGoogleMap_MapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            if (bIsFirstClick)
            {
                llInfoLayout.RemoveAllViews();
                bIsFirstClick = false;
            }
            AddLocation(e.Point);
        }

        private void AddLocation(LatLng loc)
        {
            try
            {
                var options = new MarkerOptions();
                options.SetPosition(loc);
                options.Draggable(true);
                var marker = mGoogleMap.AddMarker(options);
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
                    mActivity.RunOnUiThread(() => FocusItem(marker.Id, false));
                });
            }
            catch (Exception ex)
            {
                sys.LogException(ex);
                RemoveAllItems();
                Tools.ShowMessage(Activity, "an error happened", "try again please");
            }
        }

        private void FocusItem(string id, bool bBlink = true)
        {
            if (!wtItems.ContainsKey(id))
                return;
            var item = wtItems[id];
            scrollView.SmoothScrollTo(item.InfoView.Left - item.InfoView.Width, 0);
            if (bBlink)
                item.BlinkOnce();
        }

        const int menu_typetype_Debug_AreaCache = 1001;
        const int menu_typetype_Debug_ZonesOverlay = 1002;
        const int menu_typetype_Debug_LoadGeoJson = 1003;
        const int menu_typetype_RealSunTime = 1101;
        const int menu_typetype_MiddleSunTime = 1102;
        const int menu_typetype_TimeZoneTime = 1103;

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            base.OnCreateOptionsMenu(menu, inflater);

            //inflater.Inflate(Resource.Menu.searchbar_location, menu);
        }

        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            base.OnPrepareOptionsMenu(menu);

#if DEBUG
            var dsub = menu.AddSubMenu(0, 0, 10, "Debug");
            dsub.SetIcon(DrawableHelper.GetIconDrawable(Context, Resource.Drawable.icons8_bug_clrd, Tools.GetThemeColor(Activity.Theme, Resource.Attribute.iconTitleTint).Value));
            dsub.Item.SetShowAsAction(ShowAsAction.IfRoom);

            var ditem = dsub.Add(0, menu_typetype_Debug_AreaCache, 0, "AreaCache");
            ditem.SetShowAsAction(ShowAsAction.Always);
            ditem.SetOnMenuItemClickListener(this);

            ditem = dsub.Add(0, menu_typetype_Debug_ZonesOverlay, 0, "TZ-Overlay");
            ditem.SetShowAsAction(ShowAsAction.Always);
            ditem.SetOnMenuItemClickListener(this);

            ditem = dsub.Add(0, menu_typetype_Debug_LoadGeoJson, 0, "LoadGeoJson");
            ditem.SetShowAsAction(ShowAsAction.Always);
            ditem.SetOnMenuItemClickListener(this);
#endif

            var sub = menu.AddSubMenu(0, 0, 100, Resources.GetString(Resource.String.TimeType));
            sub.SetIcon(DrawableHelper.GetIconDrawable(Context, Tools.GetTimeTypeIconName(mTimeType, LocationTimeHolder.LocalInstance), Tools.GetThemeColor(Activity.Theme, Resource.Attribute.iconTitleTint).Value));
            sub.Item.SetShowAsAction(ShowAsAction.Always);
            IMenuItem item;
            if (mTimeType != TimeType.RealSunTime)
            {
                item = sub.Add(0, menu_typetype_RealSunTime, 0, Resources.GetString(Resource.String.TimeType_RealSunTime));
                item.SetIcon(Tools.GetTimeTypeIconID(TimeType.RealSunTime, LocationTimeHolder.LocalInstance));
                item.SetOnMenuItemClickListener(this);
            }
            if (mTimeType != TimeType.MiddleSunTime)
            {
                item = sub.Add(0, menu_typetype_MiddleSunTime, 0, Resources.GetString(Resource.String.TimeType_MiddleSunTime));
                item.SetIcon(Tools.GetTimeTypeIconID(TimeType.MiddleSunTime, LocationTimeHolder.LocalInstance));
                item.SetOnMenuItemClickListener(this);
            }
            if (mTimeType != TimeType.TimeZoneTime)
            {
                item = sub.Add(0, menu_typetype_TimeZoneTime, 0, Resources.GetString(Resource.String.TimeType_TimeZoneTime));
                item.SetIcon(Tools.GetTimeTypeIconID(TimeType.TimeZoneTime, LocationTimeHolder.LocalInstance));
                item.SetOnMenuItemClickListener(this);
            }
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            if (item.ItemId == menu_typetype_Debug_AreaCache)
            {
                RemoveAllItems();
                DrawAreaCache();
                return true;
            };
            if (item.ItemId == menu_typetype_Debug_ZonesOverlay)
            {
                DrawZonesOverlay();
                return true;
            };

            if (item.ItemId == menu_typetype_Debug_LoadGeoJson)
            {

                var intent = new Intent(Intent.ActionGetContent);

                intent.SetType("*/*");

                string[] allowedTypes = intent.GetStringArrayExtra("EXTRA_ALLOWED_TYPES")?.
                    Where(o => !string.IsNullOrEmpty(o) && o.Contains("/")).ToArray();

                if (allowedTypes != null && allowedTypes.Any())
                {
                    intent.PutExtra(Intent.ExtraMimeTypes, allowedTypes);
                }

                intent.AddCategory(Intent.CategoryOpenable);
                try
                {
                    this.StartActivityForResult(Intent.CreateChooser(intent, "Select file"), 409);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write(ex);
                }

                return true;
            }


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

        public override void OnActivityResult(int requestCode, int resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == 409 && resultCode != (int)Result.Canceled)
            {
                if (data?.Data == null)
                {
                    Tools.ShowToast(Context, "Data = null");
                }

                var uri = data.Data;

                var filePath = IOUtil.GetPath(Context, uri);

                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = IOUtil.IsMediaStore(uri.Scheme) ? uri.ToString() : uri.Path;
                }

                var fileName = IOUtil.GetFileName(Context, uri);
                Tools.ShowToast(mActivity, filePath + "\n" + fileName, true);

                var rnd = new Random();
                var pDlg = ProgressDlg.NewInstance(fileName);
                pDlg.Show(mActivity.SupportFragmentManager, "geosjon");

                Task.Factory.StartNew(() =>
                {
                    var swStart = DateTime.Now;
                    xColor[] clrS = new xColor[] { xColor.MaterialAmber, xColor.MaterialBlue, xColor.MaterialBrown, xColor.MaterialCyan, xColor.MaterialDeepOrange, xColor.MaterialDeepPurple, xColor.MaterialGreen, xColor.MaterialGrey, xColor.MaterialIndigo, xColor.MaterialOrange, xColor.MaterialPink, xColor.MaterialRed };
                    try
                    {
                        var cGeoJ = System.IO.File.ReadAllText(filePath);
                        var oJson = new JSONObject(cGeoJ);
                        GeoJsonLayer layer = new GeoJsonLayer(mGoogleMap, oJson);

                        foreach (GeoJsonFeature f in layer.Features.ToEnumerable())
                        {
                            f.PolygonStyle = new GeoJsonPolygonStyle
                            {
                                StrokeWidth = 2,
                                StrokeColor = xColor.MaterialPink.ToAndroid(),
                                FillColor = clrS[rnd.Next(clrS.Length - 1)].WithAlpha(80).ToAndroid()
                            };
                        }

                        var tsLoading = DateTime.Now - swStart;
                        swStart = DateTime.Now;

                        //layer.FeatureClick += Layer_FeatureClick;
                        mActivity.RunOnUiThread(() =>
                        {
                            layer.AddLayerToMap();
                            var tsDisplay = DateTime.Now - swStart;
                            Tools.ShowToast(mActivity, "Load: " + (int)tsLoading.TotalMilliseconds + "\nDisplay" + (int)tsDisplay.TotalMilliseconds);
                            pDlg.SetProgressDone();
                        });
                    }
                    catch (Exception ex)
                    {
                        Tools.ShowMessage(mActivity, ex.GetType().Name, ex.Message);
                        pDlg.SetProgressDone();
                    }
                    return;
                });
            }
        }

        static void RemoveItem(WorldTimeItem item)
        {
            item.Stop();
            llInfoLayout.RemoveView(item.InfoView);
            wtItems.Remove(item.ID);
            markers.Remove(item.Marker);
            item.Marker.Remove();
            item.Dispose();
        }

        static void RemoveAllItems()
        {
            foreach (var wt in wtItems?.Values)
            {
                wt.Stop();
                wt.Dispose();
            }
            wtItems?.Clear();
            markers?.Clear();
            llInfoLayout?.RemoveAllViews();
            mGoogleMap?.Clear();
        }

        private void DrawZonesOverlay()
        {
            var rnd = new Random();
            var pDlg = ProgressDlg.NewInstance(Resources.GetString(Resource.String.progress_overlay_timezones_title));
            pDlg.Show(mActivity.SupportFragmentManager, "geosjon");

            Task.Factory.StartNew(() =>
            {
                xColor[] clrS = new xColor[] { xColor.MaterialAmber, xColor.MaterialBlue, xColor.MaterialBrown, xColor.MaterialCyan, xColor.MaterialDeepOrange, xColor.MaterialDeepPurple, xColor.MaterialGreen, xColor.MaterialGrey, xColor.MaterialIndigo, xColor.MaterialOrange, xColor.MaterialPink, xColor.MaterialRed };
                try
                {
                    var cGeoJ = System.IO.File.ReadAllText(System.IO.Path.Combine(sys.PathData, "ne_10m_time_zones.geojson"));
                    GeoJsonLayer layer = new GeoJsonLayer(mGoogleMap, new JSONObject(cGeoJ));

                    foreach (GeoJsonFeature f in layer.Features.ToEnumerable())
                    {
                        f.PolygonStyle = new GeoJsonPolygonStyle
                        {
                            StrokeWidth = 2,
                            StrokeColor = clrS[int.Parse(f.GetProperty("map_color6"))].ToAndroid(),
                            FillColor = clrS[int.Parse(f.GetProperty("map_color8"))].WithAlpha(80).ToAndroid()
                        };
                    }

                    //layer.FeatureClick += Layer_FeatureClick;
                    mActivity.RunOnUiThread(() =>
                    {
                        layer.AddLayerToMap();
                        pDlg.SetProgressDone();
                    });
                }
                catch (Exception ex)
                {
                    Tools.ShowMessage(mActivity, ex.GetType().Name, ex.Message);
                    pDlg.SetProgressDone();
                }
                return;

                int ip = 0;
                foreach (var p in TimeZoneMap.timeZonePolygons.Keys)
                {
                    try
                    {

                        List<PolygonOptions> mPolygonsTimezone = new List<PolygonOptions>();

                        ip++;

                        var polygon1 = new PolygonOptions();
                        int i = 0;
                        foreach (var x in p.Coordinates)
                        {
                            i++;
                            polygon1.Add(new LatLng(x.X, x.Y));
                        }

                        var tz = TimeZoneMap.timeZonePolygons[p];

                        //polygon1.InvokeStrokeWidth(2f);
                        polygon1.InvokeStrokeColor(Color.Transparent);
                        polygon1.InvokeFillColor(clrS[tz.Color6].WithAlpha(120).ToAndroid());
                        if (string.IsNullOrEmpty(tz.timezoneId))
                            polygon1.InvokeFillColor(xColor.Black.WithAlpha(120).ToAndroid());
                        mPolygonsTimezone.Add(polygon1);

                        var polygon2 = new PolygonOptions();
                        //polygon2.Add(new LatLng(tz.bo))

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
                    catch (Exception exx)
                    {
                        Tools.ShowToast(mActivity, "Loader: " + exx.Message);
                    }
                }
            });
        }

        private void Layer_FeatureClick(object sender, Android.Gms.Maps.Utils.Data.Layer.FeatureClickEventArgs e)
        {
            Tools.ShowToast(Context, e.P0.ToString());
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

        class WorldTimeItem : IDisposable
        {
            public LatLng Location { get; set; }
            public string Title { get; set; }
            public LocationTimeHolder lth { get; private set; }
            public Marker Marker { get; set; }
            public TableLayout InfoView { get; private set; }
            TextView tvArea, tvRDT, tvMST, tvTZT, tvRDToffset, tvMSToffset, tvTZToffset;
            ImageView imgFlag, imgTZ;
            public string ID { get; }
            static string blinkingID;

            public WorldTimeItem(Marker marker)
            {
                ID = marker.Id;
                Marker = marker;
                Location = marker.Position;
                lth = LocationTimeHolder.NewInstanceDelay(Location.Latitude, Location.Longitude);
                lth.AreaChanged += lth_AreaChanged;

                InfoView = (TableLayout)mActivity.LayoutInflater.Inflate(Resource.Layout.listitem_location_times, null);

                imgFlag = InfoView.FindViewById<ImageView>(Resource.Id.img_flag);
                tvArea = InfoView.FindViewById<TextView>(Resource.Id.title);
                tvRDT = InfoView.FindViewById<TextView>(Resource.Id.time_rdt);
                tvRDToffset = InfoView.FindViewById<TextView>(Resource.Id.time_offset_rdt);
                tvMST = InfoView.FindViewById<TextView>(Resource.Id.time_mst);
                tvMSToffset = InfoView.FindViewById<TextView>(Resource.Id.time_offset_mst);
                tvTZT = InfoView.FindViewById<TextView>(Resource.Id.time_tzt);
                tvTZToffset = InfoView.FindViewById<TextView>(Resource.Id.time_offset_tzt);
                imgTZ = InfoView.FindViewById<ImageView>(Resource.Id.img_timezone);

                InfoView.Click += InfoView_Click;
                InfoView.LongClick += InfoView_LongClick;
                tvArea.Text = sys.DezimalGradToGrad(Location.Latitude, Location.Longitude);

                llInfoLayout.AddView(InfoView, new LinearLayout.LayoutParams((int)TypedValue.ApplyDimension(ComplexUnitType.Sp, 165, mActivity.Resources.DisplayMetrics), LinearLayout.LayoutParams.MatchParent));

                /*
                InfoView = new TableLayout(mActivity);
                InfoView.Clickable = true;
                InfoView.Click += InfoView_Click;
                InfoView.LongClick += InfoView_LongClick;
                InfoView.SetBackgroundResource(Resource.Drawable.selector);
                InfoView.SetColumnStretchable(2, true);
                int iPad1 = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 10, mActivity.Resources.DisplayMetrics);
                int iPad2 = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 5, mActivity.Resources.DisplayMetrics);
                InfoView.SetPadding(iPad2, 0, 0, 0);

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
                tvRDToffset.SetPadding(iPad1, 0, 0, 0);
                tvMSToffset = new TextView(mActivity) { Text = "-??:??" };
                tvMSToffset.SetPadding(iPad1, 0, 0, 0);
                tvTZToffset = new TextView(mActivity) { Text = "~??:??" };
                tvTZToffset.SetPadding(iPad1, 0, 0, 0);

                var lpImp = new TableLayout.LayoutParams(55,55);// (int)TypedValue.ApplyDimension(ComplexUnitType.Sp, 26, mActivity.Resources.DisplayMetrics), (int)TypedValue.ApplyDimension(ComplexUnitType.Sp, 26, mActivity.Resources.DisplayMetrics));
                var img = new ImageView(mActivity);
                img.SetMaxWidth(23);
                img.SetMaxWidth(23);
                img.SetImageResource(Tools.GetTimeTypeIconID(TimeType.RealSunTime, lth));
                //img.SetScaleType(ImageView.ScaleType.FitXy);

                row = new TableRow(mActivity);
                row.AddView(img);
                row.AddView(tvRDT);
                row.AddView(tvRDToffset);
                InfoView.AddView(row);

                img = new ImageView(mActivity);
                img.SetImageResource(Tools.GetTimeTypeIconID(TimeType.MiddleSunTime, lth));
                //img.SetScaleType(ImageView.ScaleType.FitXy);

                row = new TableRow(mActivity);
                row.AddView(img, lpImp);
                row.AddView(tvMST);
                row.AddView(tvMSToffset);
                InfoView.AddView(row);

                imgTZ = new ImageView(mActivity);
                imgTZ.SetImageResource(Tools.GetTimeTypeIconID(TimeType.TimeZoneTime, lth));
                imgTZ.SetScaleType(ImageView.ScaleType.FitXy);

                row = new TableRow(mActivity);
                row.AddView(imgTZ, 0, lpImp);
                row.AddView(tvTZT, 1);
                row.AddView(tvTZToffset, 2);
                InfoView.AddView(row);

                llInfoLayout.AddView(InfoView, new LinearLayout.LayoutParams((int)TypedValue.ApplyDimension(ComplexUnitType.Sp, 165, mActivity.Resources.DisplayMetrics), LinearLayout.LayoutParams.MatchParent));
                */
            }

            private void InfoView_LongClick(object sender, View.LongClickEventArgs e)
            {
                PopupMenu popup = new PopupMenu(mActivity, sender as View);
                popup.Menu.Add(0, 1, 0, Resource.String.action_remove);
                popup.Menu.Add(0, 2, 0, Resource.String.action_remove_all);

                popup.MenuItemClick += (s, e) =>
                {
                    if (e.Item.ItemId == 1)
                    {
                        RemoveItem(this);
                    }
                    else if (e.Item.ItemId == 2)
                    {
                        RemoveAllItems();
                    }
                };

                popup.Show();
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
                        try
                        {
                            var flag = typeof(Resource.Drawable).GetField("flag_" + lth.AreaInfo.countryCode.ToLower());
                            imgFlag.SetImageResource((int)flag.GetValue(null));
                        }
                        catch
                        {
                            imgFlag.SetImageResource(0);
                        }
                        imgTZ.SetImageResource(Tools.GetTimeTypeIconID(TimeType.TimeZoneTime, lth));
                        string cText = string.IsNullOrEmpty(lth.AreaName) ? sys.DezimalGradToGrad(lth.Latitude, lth.Longitude) : lth.AreaName;
                        tvArea.Text = cText;
                        Marker.Title = cText;
                        Marker.ShowInfoWindow();
                    }
                    catch { }
                });
            }

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
                lth?.StopTimeChangedHandler(this);
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
                if (lth == null || InfoView == null)
                    return;
                DateTime tCurrent = lth.GetTime(mTimeType);
                DateTime tInfo = lth.GetTime(typeType);
                var tsOff = tInfo - tCurrent;
                tvTime.Text = tInfo.ToLongTimeString();
                if (sys.GetTimeWithoutMilliSeconds(tCurrent) != sys.GetTimeWithoutMilliSeconds(tInfo))
                {
                    tvOffset.Text = (tCurrent > tInfo ? "-" : "+") + tsOff.ToShortString();
                    double iMin = tsOff.TotalMinutes;
                    if (iMin < 0) iMin *= -1;
                    if (iMin < 30)
                        tvOffset.SetTextColor(xColor.MaterialLightGreen.ToAndroid());
                    else if (iMin < 45)
                        tvOffset.SetTextColor(xColor.MaterialAmber.ToAndroid());
                    else if (iMin < 60)
                        tvOffset.SetTextColor(xColor.MaterialOrange.ToAndroid());
                    else if (iMin < 90)
                        tvOffset.SetTextColor(xColor.MaterialDeepOrange.ToAndroid());
                    else
                        tvOffset.SetTextColor(xColor.MaterialRed.ToAndroid());
                }
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
                    DateTime tEnd = DateTime.Now.AddMilliseconds(250);
                    while (blinkingID == ID && tEnd > DateTime.Now)
                        Task.Delay(25).Wait();
                    if (lth == null || InfoView == null)
                        return;
                    mActivity.RunOnUiThread(() => InfoView.SetBackgroundResource(Resource.Drawable.selector));
                    isBlinking = false;
                });
            }

            public void Dispose()
            {
                blinkingID = null;
                Stop();
                lth.Dispose();
                lth = null;
                Marker = null;
                InfoView = null;
                Location = null;
            }
        }
    }
}