using System.Collections.Generic;
using System.IO;

using Android.App;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;

namespace iChronoMe.Droid.Adapters
{
    class ErrorLogAdapter : BaseAdapter<SimpleObject>
    {
        public List<SimpleObject> Items { get; }
        Activity mContext;

        public ErrorLogAdapter(Activity context) : base()
        {
            mContext = context;
            Items = new List<SimpleObject>();

            var logS = new List<string>(Directory.GetFiles(sys.ErrorLogPath));
            logS.Sort();

            foreach (string log in logS)
            {
                string cTitle = Path.GetFileNameWithoutExtension(log);
                int iIcon = -1;
                if (log.EndsWith(".png"))
                {
                    iIcon = Resource.Drawable.icons8_screenshot;
                    cTitle = mContext.Resources.GetString(Resource.String.label_sceenshot) + " " + File.GetCreationTime(log).ToShortDateString() + " " + File.GetCreationTime(log).ToShortTimeString();
                }
                else
                {
                    iIcon = Resource.Drawable.icons8_error_clrd;
                    cTitle = mContext.Resources.GetString(Resource.String.label_errorlog) + " " + File.GetCreationTime(log).ToShortDateString() + " " + File.GetCreationTime(log).ToShortTimeString();
                }

                if (!log.EndsWith(".png") || !AppConfigHolder.MainConfig.DenyErrorScreens)
                    this.Items.Add(new SimpleObject() { Tag = log, IconRes = iIcon, Title1 = cTitle });
            }
        }

        public override SimpleObject this[int position]
        {
            get
            {
                return Items[position];
            }
        }

        public override int Count
        {
            get
            {
                return Items.Count;
            }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = Items[position];

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