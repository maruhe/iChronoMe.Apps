using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.Adapters;

namespace iChronoMe.Droid.Source.Adapters
{
    class ErrorLogAdapter : BaseAdapter<SimpleObject>
    {
        List<SimpleObject> items;
        Activity mContext;

        public ErrorLogAdapter(Activity context) : base()
        {
            var logS = Directory.GetFiles(sys.ErrorLogPath);

            items = new List<SimpleObject>();
            foreach (string log in logS)
            {
                string cTitle = Path.GetFileNameWithoutExtension(log);
                int iIcon = -1;
                if (log.EndsWith(".log"))
                {
                    iIcon = Resource.Drawable.icons8_delete;
                    cTitle = "Error " + File.GetCreationTime(log).ToShortDateString() + " " + File.GetCreationTime(log).ToShortTimeString();
                }
                else if (log.EndsWith(".png"))
                {
                    iIcon = Resource.Drawable.icons8_edit;
                    cTitle = "Screenshot " + File.GetCreationTime(log).ToShortDateString() + " " + File.GetCreationTime(log).ToShortTimeString();
                }

                this.items.Add(new SimpleObject() { Tag = log, IconRes = iIcon, Title1 = cTitle });
            }

            mContext = context;
        }

        public override SimpleObject this[int position]
        {
            get
            {
                return items[position];
            }
        }

        public override int Count
        {
            get
            {
                return items.Count;
            }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = items[position];

            if (convertView == null)
            {
                convertView = mContext.LayoutInflater.Inflate(Resource.Layout.listitem_title, null);
            }

            if (item.IconRes > 0)
                convertView.FindViewById<ImageView>(Resource.Id.icon).SetImageResource(item.IconRes);
            convertView.FindViewById<TextView>(Resource.Id.title).Text = item.Title1;

            return convertView;
        }
    }
}