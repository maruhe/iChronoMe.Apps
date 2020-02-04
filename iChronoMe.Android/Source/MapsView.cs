
using Android.Content;
using Android.Gms.Maps;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Util;
using Android.Widget;

namespace iChronoMe.Droid
{
    public class MapsView : FrameLayout, IOnMapReadyCallback
    {
        Context mContext;
        FragmentManager fManager;

        public MapsView(Context context, FragmentManager manager, IAttributeSet attrs) :
            base(context, attrs)
        {
            mContext = context;
            fManager = manager;
        }

        public MapsView(AppCompatActivity context, FragmentManager manager, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            mContext = context;
            fManager = manager;
        }

        SupportMapFragment mapFragment;
        GoogleMap googleMap;

        public void Initialize()
        {
            this.Id = 123125;
            mapFragment = SupportMapFragment.NewInstance();
            fManager.BeginTransaction()
                            .Add(this.Id, mapFragment, "map_fragment")
                            .Commit();
            //mapFragment.GetMapAsync(this);
        }

        public void OnMapReady(GoogleMap map)
        {
            googleMap = map;

            googleMap.UiSettings.ZoomControlsEnabled = true;
            googleMap.UiSettings.CompassEnabled = true;
            googleMap.UiSettings.MyLocationButtonEnabled = false;
        }

    }
}