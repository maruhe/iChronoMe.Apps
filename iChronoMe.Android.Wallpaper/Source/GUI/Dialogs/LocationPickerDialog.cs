using System;
using System.Threading.Tasks;

using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;

using iChronoMe.Core.Classes;

using iChronoMe.Droid.Wallpaper;

using Xamarin.Essentials;
using Resource = iChronoMe.Droid.Wallpaper.Resource;

namespace iChronoMe.Droid.GUI.Dialogs
{
    class LocationPickerDialog : DialogFragment, IOnMapReadyCallback
    {
        private static Task<bool> UserInputTaskTask { get { return tcsUI == null ? Task.FromResult(false) : tcsUI.Task; } }
        private static TaskCompletionSource<bool> tcsUI = null;
        private static SelectPositionResult dlgResult;

        public static async Task<SelectPositionResult> SelectLocation(AppCompatActivity activity, Location center = null, Location marker = null)
        {
            dlgResult = null;
            await Task.Factory.StartNew(() =>
            {
                tcsUI = new TaskCompletionSource<bool>();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var dlg = new LocationPickerDialog(center, marker);
                    dlg.Show(activity.SupportFragmentManager, "LocationPickerDialog");
                });

                UserInputTaskTask.Wait();
            });
            if (UserInputTaskTask.Result)
                return dlgResult;
            return null;
        }

        public LocationPickerDialog(Location center = null, Location marker = null)
        {
            initCenter = center;
            initMarker = marker;
        }

        Location initCenter = null;
        Location initMarker = null;
        Marker CurrentMarker = null;

        GoogleMap mGoogleMap = null;
        MapView mMapView = null;
        double nLatitude, nLongitude;

        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            try
            {
                AlertDialog dialog = new AlertDialog.Builder(Context)
                .SetTitle(Resource.String.title_location_picker)
                .SetView(Resource.Layout.dialog_select_maplocation)
                .SetPositiveButton(Resource.String.action_select, (senderAlert, args) =>
                {
                    dlgResult = new SelectPositionResult { Title = "", Latitude = nLatitude, Longitude = nLongitude };
                    tcsUI?.TrySetResult(true);
                })
                .SetNegativeButton(Resource.String.action_cancel, (senderAlert, args) =>
                {
                    tcsUI?.TrySetResult(false);
                })
                .Create();
                dialog.Show();
                mGoogleMap = null;

                mMapView = (MapView)dialog.FindViewById(Resource.Id.mapView);
                MapsInitializer.Initialize(Context);

                mMapView = (MapView)dialog.FindViewById(Resource.Id.mapView);
                mMapView.OnCreate(dialog.OnSaveInstanceState());
                mMapView.OnResume();// needed to get the map to display immediately
                mMapView.GetMapAsync(this);

                return dialog;
            }
            catch (Exception ex)
            {
                xLog.Error(ex);
                return null;
            }
        }

        public override void OnCancel(IDialogInterface dialog)
        {
            tcsUI?.TrySetResult(false);
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            mGoogleMap = googleMap;

            LatLng center = new LatLng(sys.lastUserLocation.Latitude, sys.lastUserLocation.Longitude);
            if (initCenter != null && initCenter.Latitude != 0)
                center = new LatLng(initCenter.Latitude, initCenter.Longitude);
            if (initMarker != null && initMarker.Latitude != 0)
            {
                LatLng marker = new LatLng(initMarker.Latitude, initMarker.Longitude);
                if (initCenter == null)
                    center = marker;
                var options = new MarkerOptions();
                options.SetPosition(marker);
                options.Draggable(true);
                CurrentMarker = mGoogleMap.AddMarker(options);
            }
            mGoogleMap.MoveCamera(CameraUpdateFactory.NewLatLng(center));

            googleMap.UiSettings.ZoomControlsEnabled = true;
            googleMap.UiSettings.MapToolbarEnabled = false;
            googleMap.UiSettings.CompassEnabled = false;
            googleMap.UiSettings.MyLocationButtonEnabled = true;
            googleMap.UiSettings.RotateGesturesEnabled = false;

            mGoogleMap.MapClick += MGoogleMap_MapClick;
            mGoogleMap.MarkerDragEnd += MGoogleMap_MarkerDragEnd;
        }

        private void MGoogleMap_MapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            mGoogleMap.Clear();

            nLatitude = e.Point.Latitude;
            nLongitude = e.Point.Longitude;

            var options = new MarkerOptions();
            options.SetPosition(e.Point);
            options.Draggable(true);
            CurrentMarker = mGoogleMap.AddMarker(options);

            var cameraUpdate = CameraUpdateFactory.NewLatLngZoom(e.Point, 15);
            //mGoogleMap.MoveCamera(cameraUpdate);
        }
        private void MGoogleMap_MarkerDragEnd(object sender, GoogleMap.MarkerDragEndEventArgs e)
        {
            nLatitude = e.Marker.Position.Latitude;
            nLongitude = e.Marker.Position.Longitude;
        }
    }
}