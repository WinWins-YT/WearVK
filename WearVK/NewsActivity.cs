using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.Wearable.Activity;
using Android.Views;
using AndroidX.Core.View;
using AndroidX.RecyclerView.Widget;
using AndroidX.Wear.Widget;
using VkNet.Enums.Filters;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using WearVK.RecyclerAdapters;

namespace WearVK
{
    [Activity(Label = "News")]
    public class NewsActivity : WearableActivity
    {
        private NewsAdapter adapter;
        private WearableRecyclerView recycler;
        private string nextFrom = "";
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_posts);

            adapter = new NewsAdapter(this);
            
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
                              ViewConfigurationCompat.GetScaledVerticalScrollFactor(ViewConfiguration.Get(this), this);

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
            var news = await MainActivity.VK.NewsFeed.GetAsync(new NewsFeedGetParams
            {
                StartFrom = nextFrom
            });
            nextFrom = news.NextFrom;
            foreach (var n in news.Items.Where(x => x.Type.Equals(NewsTypes.Post)))
            {
                var photos = n.Attachments.Where(x => x.Instance is Photo).ToArray();
                if (photos.Any())
                    adapter.Items.AddRange(photos.Select(x =>
                        (((Photo)x.Instance).Sizes.OrderBy(x => x.Height).Last().Url.ToString(), n)));
                else
                    adapter.Items.Add((null, n));
            }
            adapter.NotifyDataSetChanged();
        }
    }
}