using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.Wearable.Activity;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using AndroidX.RecyclerView.Widget;
using AndroidX.Wear.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Threading.Tasks;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.Attachments;
using WearVK.RecyclerAdapters;

namespace WearVK
{
    [Activity(Label = "Comments", MainLauncher =false)]
    public class CommentsActivity : WearableActivity
    {
        CommentsAdapter adapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_comments);

            var recycler = FindViewById<WearableRecyclerView>(Resource.Id.recycler_comments_view);

            adapter = new CommentsAdapter();

            recycler.GenericMotion += Recycler_GenericMotion;
            recycler.SetAdapter(adapter);
            recycler.EdgeItemsCenteringEnabled = true;
            recycler.BezelFraction = 1.0f;
            recycler.SetLayoutManager(new WearableLinearLayoutManager(this, new CustomScrollingLayoutCallback()));
            recycler.RequestFocus();

            _ = GetComments();
        }

        private void Recycler_GenericMotion(object sender, View.GenericMotionEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Scroll && e.Event.Source == InputSourceType.RotaryEncoder)
            {
                float delta = -e.Event.GetAxisValue(Axis.Scroll) *
                    ViewConfigurationCompat.GetScaledVerticalScrollFactor(ViewConfiguration.Get(ApplicationContext), ApplicationContext);

                if (Math.Abs(delta) > 100)
                    ((WearableRecyclerView)sender).SmoothScrollBy(0, (int)Math.Round(delta));
                else
                    ((WearableRecyclerView)sender).ScrollBy(0, (int)Math.Round(delta));
            }
        }

        private async Task GetComments()
        {
            var comments = await MainActivity.VK.Wall.GetCommentsAsync(new VkNet.Model.RequestParams.WallGetCommentsParams()
            {
                PostId = Intent.GetLongExtra("PostId", 0),
                OwnerId = Intent.GetLongExtra("OwnerId", 0),
                Count = 100
            });
            adapter.Items.AddRange(comments.Items);
            adapter.NotifyDataSetChanged();
        }
    }
}