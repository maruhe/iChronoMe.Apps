﻿using System;

using Android.OS;
using Android.Views;

namespace iChronoMe.Droid.GUI.Calendar
{
    public class EventEditFragment : ActivityFragment, IMenuItemOnMenuItemClickListener
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            RootView = (ViewGroup)inflater.Inflate(Resource.Layout.fragment_event_edit, container, false);


            return RootView;
        }

        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            base.OnPrepareOptionsMenu(menu);
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            throw new NotImplementedException();
        }
    }
}