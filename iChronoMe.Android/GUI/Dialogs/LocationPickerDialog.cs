using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Content;
using Android.Gms.Maps;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.GUI.Dialogs
{
    class LocationPickerDialog : DialogFragment {

        public static LocationPickerDialog NewInstance(Bundle bundle)
        {
            LocationPickerDialog fragment = new LocationPickerDialog();
            fragment.Arguments = bundle;
            return fragment;
        }

        MapsView map;

        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            map = new MapsView(Context, null, null);

            AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
            alert.SetTitle("Choose Location");
            alert.SetView(map);
            alert.SetPositiveButton("Select", (senderAlert, args) => {
                Toast.MakeText(Activity, "Selected :-)", ToastLength.Short).Show();
            });

            alert.SetNegativeButton("Cancel", (senderAlert, args) => {
                Toast.MakeText(Activity, "Cancelled!", ToastLength.Short).Show();
            });

            return alert.Create();
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            map.Initialize();
        }
    }
}

/*
 *   map.setOnMapLoadedCallback {

                val lat = arguments?.getDouble(EXTRA_LAT)
                    val lng = arguments?.getDouble(EXTRA_LNG)



                    if (lat != null && lng != null)
                {
                    val latLng = LatLng(lat, lng)

                        map.addMarker(MarkerOptions()
                                .position(latLng)
                                )

                        map.moveCamera(CameraUpdateFactory.newLatLngZoom(latLng, DEFAULT_ZOOM))
                    }
 */
