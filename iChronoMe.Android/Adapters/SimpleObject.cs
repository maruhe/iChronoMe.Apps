using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.Adapters
{
    public class SimpleObject
    {
        public object Tag { get; set; }

        public int IconRes { get; set; }

        public string Title1 { get; set; }
        public string Title2 { get; set; }
        public string Title3 { get; set; }

        public string Description1 { get; set; }
        public string Description2 { get; set; }
        public string Description3 { get; set; }

        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public string Text3 { get; set; }
    }
}