using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;

using Android.Content;
using Android.Database;
using Android.Gms.Common.Data;
using Android.Gms.Location.Places;
using Android.Gms.Maps.Model;
using Android.Provider;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;

namespace iChronoMe.Droid.Adapters
{
    public class CalendarEventLocationAdapter : CursorAdapter
    {
        Context mContext;
        ContentResolver mContent;

        public CalendarEventLocationAdapter(Context context, ICursor c) : base(context, c, true)
        {
            mContext = context;
            mContent = context.ContentResolver;
        }

        public override Java.Lang.ICharSequence ConvertToStringFormatted(ICursor cursor)
        {
            return new Java.Lang.String(cursor.GetString(1));
        }

        public override void BindView(View view, Context context, ICursor cursor)
        {
            ((TextView)view).Text = cursor.GetString(1);
        }

        public override View NewView(Context context, ICursor cursor, ViewGroup parent)
        {
            var inflater = LayoutInflater.From(context);
            var view = (TextView)inflater.Inflate(Android.Resource.Layout.SimpleDropDownItem1Line, parent, false);
            view.Text = cursor.GetString(1);
            return view;
        }

        public override ICursor RunQueryOnBackgroundThread(Java.Lang.ICharSequence constraint)
        {
            return doRunQueryOnBackgroundThread(constraint == null ? null : constraint.ToString());
        }

        public ICursor doRunQueryOnBackgroundThread(string constraint)
        {
            if (FilterQueryProvider != null)
            {
                this.ToString();
                ;//return getFilterQueryProvider().runQuery(constraint);
            }

            StringBuilder buffer = null;
            String[] args = null;

            if (string.IsNullOrEmpty(constraint))
                return null;

            if (constraint != null)
            {
                try
                {
                    constraint = constraint.ToString().Trim();
                    buffer = new StringBuilder();
                    buffer.Append("UPPER(").Append(CalendarContract.Events.InterfaceConsts.EventLocation).Append(") GLOB ? ");
                    buffer.Append(")) GROUP BY ((").Append(CalendarContract.Events.InterfaceConsts.EventLocation);

                    String cSearch = constraint.ToUpper();
                    cSearch = cSearch.Replace("  ", " ");
                    cSearch = cSearch.Replace(" ", "* *");
                    cSearch = "*" + cSearch + "*";

                    args = new String[] { cSearch };

                    var cur = mContent.Query(CalendarContract.Events.ContentUri, new string[] { CalendarContract.Events.InterfaceConsts.Id, CalendarContract.Events.InterfaceConsts.EventLocation }, buffer == null ? null : buffer.ToString(), args, CalendarContract.Events.InterfaceConsts.EventLocation);

                    ClearOnlineResult();
                    if (cur.Count < 5)
                    {
                        StartOnlineSearch(constraint);
                    }

                    return cur;
                }
                catch (Exception ex)
                {
                    sys.LogException(ex);
                }
            }

            return null;
        }

        #region GoogleOnlineSearch
        private GeoDataClient mGeoDataClient;
        private LatLngBounds mBounds;
        private AutocompleteFilter mPlaceFilter;

        private void StartOnlineSearch(string constraint)
        {
            return;
            Task.Factory.StartNew(() =>
            {
                var data = getGoogleAutocomplete(constraint);
                data.ToString();
            });
        }


        private void ClearOnlineResult()
        {

        }

        private IList getGoogleAutocomplete(string constraint)
        {
            xLog.Info("Starting google autocomplete query for: " + constraint);

            if (mGeoDataClient == null)
                mGeoDataClient = PlacesClass.GetGeoDataClient(mContext);

            try
            {

                // Submit the query to the autocomplete API and retrieve a PendingResult that will
                // contain the results when the query completes.
                var task = mGeoDataClient.GetAutocompletePredictions(constraint, mBounds, mPlaceFilter);

                while (!task.IsCanceled && !task.IsComplete && !task.IsSuccessful)
                    Task.Delay(100).Wait();

                /*lock (this)
                {
                    Looper.Prepare();
                    lock (task)
                    {
                        task.Wait();
                    }
                }*/

                var result = task.Result;

                /*// This method should have been called off the main UI thread. Block and wait for at most
                // 60s for a result from the API.
                lock (task)
                {
                    try
                    {
                        task.Wait(60 * 1000);
                    }
                    catch (Exception e)
                    {
                        xLog.Error(e);
                    }
                }
                */

                //AutocompletePredictionBufferResponse
                IDataBuffer autocompletePredictions = result as IDataBuffer;

                xLog.Info("Query completed. Received " + "?" + " predictions.");

                // Freeze the results immutable representation that can be stored safely.
                return DataBufferUtils.FreezeAndClose(autocompletePredictions);
            }
            catch (Exception e)
            {
                // If the query did not complete successfully return null
                Tools.ShowToast(mContext, "Error contacting API: " + e.Message);
                xLog.Error(e, "Error getting autocomplete prediction API call");
                return null;
            }
        }


        #endregion
    }
}