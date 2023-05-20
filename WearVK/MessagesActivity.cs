using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.Wearable.Activity;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidX.Core.View;
using AndroidX.Wear.Widget;
using VkNet.Enums.Filters;
using VkNet.Model.RequestParams;
using WearVK.RecyclerAdapters;

namespace WearVK
{
    [Activity(Label = "Messages")]
    public class MessagesActivity : WearableActivity
    {
        ProgressBar progressBar;
        WearableRecyclerView recyclerView;
        private MessagesAdapter _adapter;
        private ulong _offsetPage = 0;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            
            progressBar = FindViewById<ProgressBar>(Resource.Id.mainProgress);
            recyclerView = FindViewById<WearableRecyclerView>(Resource.Id.recycler_launcher_view);

            _adapter = new MessagesAdapter();
            _adapter.OnItemClicked += (sender, args) =>
            {
                var layout = (LinearLayout)((LinearLayout)sender).GetChildAt(0);
                var desc = layout.GetChildAt(0).ContentDescription;
                var intent = new Intent(this, typeof(ChatActivity));
                intent.PutExtra("peerId", Convert.ToInt64(desc));
                StartActivity(intent);
            };
            recyclerView.GenericMotion += Recycler_GenericMotion;
            recyclerView.ScrollChange += Recycler_ScrollChange;
            recyclerView.EdgeItemsCenteringEnabled = true;
            recyclerView.SetLayoutManager(new WearableLinearLayoutManager(this, new CustomScrollingLayoutCallback()));
            recyclerView.SetAdapter(_adapter);
            recyclerView.RequestFocus();
            
            _ = LoadHistory();
        }
        
        private void Recycler_ScrollChange(object sender, View.ScrollChangeEventArgs e)
        {
            if (!e.V.CanScrollVertically(1))
                _ = LoadHistory();
        }

        private async Task LoadHistory()
        {
            try
            {
                var history = await MainActivity.VK.Messages.GetConversationsAsync(new GetConversationsParams
                {
                    Offset = _offsetPage++ * 20,
                    Count = 20
                });

                using var client = new HttpClient();
                foreach (var h in history.Items)
                {
                    Bitmap bitmap = null;
                    string name = "";
                    if (h.Conversation.Peer.Id < 0)
                    {
                        var group = (await MainActivity.VK.Groups.GetByIdAsync(
                            new[] { (-h.Conversation.Peer.Id).ToString() },
                            (-h.Conversation.Peer.Id).ToString(), GroupsFields.All)).First();
                        var bytes = await client.GetByteArrayAsync(group.Photo50);
                        bitmap = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                        name = group.Name;
                    }
                    else if (h.Conversation.Peer.Id < 2e9)
                    {
                        var user = (await MainActivity.VK.Users.GetAsync(new[] { h.Conversation.Peer.Id },
                            ProfileFields.All)).First();
                        var bytes = await client.GetByteArrayAsync(user.Photo50);
                        bitmap = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                        name = $"{user.FirstName} {user.LastName}";
                    }
                    else
                    {
                        var con = (await MainActivity.VK.Messages.GetConversationsByIdAsync(new[] { h.Conversation.Peer.Id })).Items.First();
                        if (con.ChatSettings.Photo is null)
                            bitmap = await BitmapFactory.DecodeStreamAsync(Assets.Open("ZuevFace.png"));
                        else
                        {
                            var bytes = await client.GetByteArrayAsync(con.ChatSettings.Photo.Photo50);
                            bitmap = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                        }

                        name = con.ChatSettings.Title;
                    }

                    _adapter.Items.Add((bitmap, name, h.Conversation.Peer.Id));
                }

                _adapter.NotifyDataSetChanged();

                progressBar.Visibility = ViewStates.Gone;
                recyclerView.Visibility = ViewStates.Visible;
                recyclerView.RequestFocus();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, $"Exception occured: {ex.Message}", ToastLength.Long).Show();
            }
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
    }
}