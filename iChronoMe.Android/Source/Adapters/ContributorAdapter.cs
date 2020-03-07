using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;

namespace iChronoMe.Droid.Source.Adapters
{
    public class ContributorAdapter : BaseAdapter<ContributorInfo>
    {
        Activity mContext;
        List<ContributorInfo> itemS;

        public ContributorAdapter(Activity context) : base()
        {
            mContext = context;
            itemS = new List<ContributorInfo>(Contributors.AllCredits);
        }

        public override ContributorInfo this[int position] => itemS[position];

        public override int Count => itemS.Count;

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = itemS[position];


            var view = mContext.LayoutInflater.Inflate(Resource.Layout.listitem_contributor, null);
            view.Tag = position;

            view.FindViewById<TextView>(Resource.Id.title).Text = item.Name;
            view.FindViewById<TextView>(Resource.Id.description).Text = item.Description;
            var tv = view.FindViewById<TextView>(Resource.Id.hyperlink);
            tv.PaintFlags = tv.PaintFlags | PaintFlags.UnderlineText;
            tv.Text = item.WebLink;

            view.Click += View_Click;
            view.FindViewById<TableRow>(Resource.Id.row_hyperlink).Click += HyperLink_Click;
            view.FindViewById<TableRow>(Resource.Id.row_hyperlink).Tag = item.WebLink;

            return view;
        }

        private void View_Click(object sender, EventArgs e)
        {
            try
            {
                var item = itemS[(int)(sender as TableLayout).Tag];
                var dlg = new AlertDialog.Builder(mContext)
                        .SetTitle(item.Name)
                        .SetNegativeButton(Resource.String.action_close, (s, e) => { });

                if (string.IsNullOrEmpty(item.LongInfoText))
                    dlg.SetMessage(item.Description);
                else
                    dlg.SetMessage(item.LongInfoText);

                if (!string.IsNullOrEmpty(item.WebLink))
                    dlg.SetPositiveButton(Resource.String.action_webpage, (s, e) =>
                    {
                        var intent = new Intent(Intent.ActionView);
                        intent.SetData(Android.Net.Uri.Parse(item.WebLink));
                        mContext.StartActivity(intent);
                    });
                dlg.Show();
            }
            catch { }
        }

        private void HyperLink_Click(object sender, EventArgs e)
        {
            try
            {
                var intent = new Intent(Intent.ActionView);
                intent.SetData(Android.Net.Uri.Parse((string)(sender as TableRow).Tag));
                mContext.StartActivity(intent);
            }
            catch { }
        }
    }
}