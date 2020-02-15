using System;
using System.Threading.Tasks;

using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;

using iChronoMe.Core.Classes;

using Xamarin.Essentials;

namespace iChronoMe.Droid.GUI.Dialogs
{
    class LocationPickerDialog : DialogFragment, IOnMapReadyCallback
    {
        private static Task<bool> UserInputTaskTask { get { return tcsUI == null ? Task.FromResult(false) : tcsUI.Task; } }
        private static TaskCompletionSource<bool> tcsUI = null;
        private static SelectPositionResult dlgResult;

        public static async Task<SelectPositionResult> SelectLocation(AppCompatActivity activity)
        {
            dlgResult = null;
            await Task.Factory.StartNew(() =>
            {
                tcsUI = new TaskCompletionSource<bool>();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var dlg = new LocationPickerDialog();
                    dlg.Show(activity.SupportFragmentManager, "LocationPickerDialog");
                });

                UserInputTaskTask.Wait();
            });
            if (UserInputTaskTask.Result)
                return dlgResult;
            return null;
        }

        public static LocationPickerDialog NewInstance(Bundle bundle)
        {
            LocationPickerDialog fragment = new LocationPickerDialog();
            fragment.Arguments = bundle;
            return fragment;
        }

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

            LatLng posisiabsen = new LatLng(47.2813, 13.7255); ////your lat lng
            //mGoogleMap.AddMarker(new MarkerOptions() { Position = posisiabsen, Title = "Yout title");
            mGoogleMap.MoveCamera(CameraUpdateFactory.NewLatLng(posisiabsen));

            googleMap.UiSettings.ZoomControlsEnabled = true;
            googleMap.UiSettings.MapToolbarEnabled = false;
            googleMap.UiSettings.CompassEnabled = true;
            googleMap.UiSettings.MyLocationButtonEnabled = true;

            mGoogleMap.MapClick += MGoogleMap_MapClick;
        }

        private void MGoogleMap_MapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            mGoogleMap.Clear();

            nLatitude = e.Point.Latitude;
            nLongitude = e.Point.Longitude;

            var passchendaeleMarker = new MarkerOptions();
            passchendaeleMarker.SetPosition(e.Point);
            mGoogleMap.AddMarker(passchendaeleMarker);

            var cameraUpdate = CameraUpdateFactory.NewLatLngZoom(e.Point, 15);
            //mGoogleMap.MoveCamera(cameraUpdate);
        }
    }
}