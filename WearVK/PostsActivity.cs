using System;

using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.Wearable.Activity;
using AndroidX.RecyclerView.Widget;
using Android.Views;
using System.Collections.Generic;
using AndroidX.Wear.Widget;
using Android.Support.Wearable.Views;
using Android.Graphics;
using VkNet;
using Microsoft.Extensions.DependencyInjection;
using VkNet.AudioBypassService.Extensions;
using VkNet.Model;
using System.Threading.Tasks;
using VkNet.Model.Attachments;
using Org.Apache.Http.Client.Params;
using System.Net.Http;
using WearableRecyclerView = AndroidX.Wear.Widget.WearableRecyclerView;
using System.Linq;
using Android.Content;
using System.Runtime.Remoting.Contexts;
using AndroidX.Core.View;
using Button = Android.Widget.Button;
using Context = Android.Content.Context;
using VkNet.Enums.Filters;
using WearVK.RecyclerAdapters;

namespace WearVK
{
    [Activity(Label = "Posts", MainLauncher = false)]
    public class PostsActivity : WearableActivity
    {
        public static readonly long GroupId = 0;
        ulong loadedPages = 0;
        WearableRecyclerView recycler;
        PostsAdapter adapter;
        internal static Bitmap groupPic;
        internal static string groupName;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activity_posts);

            adapter = new PostsAdapter(this, MainActivity.VK, GroupId);

            recycler = FindViewById<WearableRecyclerView>(Resource.Id.recycler_posts_view);
            recycler.GenericMotion += Recycler_GenericMotion;
            recycler.ScrollChange += Recycler_ScrollChange;
            recycler.SetAdapter(adapter);
            recycler.EdgeItemsCenteringEnabled = true;
            recycler.BezelFraction = 1.0f;
            recycler.SetLayoutManager(new LinearLayoutManager(this));
            recycler.RequestFocus();
            _ = LoadMore();
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

        private void Recycler_ScrollChange(object sender, View.ScrollChangeEventArgs e)
        {
            if (!e.V.CanScrollVertically(1))
                _ = LoadMore();
        }

        private async Task LoadMore()
        {
            var group = (await MainActivity.VK.Groups.GetByIdAsync(new string[] { GroupId.ToString() }, GroupId.ToString(), GroupsFields.All)).First();
            using var client = new HttpClient();
            var bytes = await client.GetByteArrayAsync(group.Photo50);
            groupPic = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
            groupName = group.Name;
            var posts = await MainActivity.VK.Wall.GetAsync(new VkNet.Model.RequestParams.WallGetParams()
            {
                Count = 100,
                Offset = loadedPages * 100,
                OwnerId = -GroupId
            });
            foreach (var wall in posts.WallPosts)
            {
                if (wall.Attachment != null)
                {
                    foreach (var attachment in wall.Attachments)
                    {
                        if (attachment.Instance is Photo photo)
                        {
                            adapter.Items.Add((photo.Id.Value, photo.Sizes.Last().Url.ToString(), wall));
                        }
                    }
                }
            }
            loadedPages++;
            adapter.NotifyDataSetChanged();
        }
    }
}


