using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.Wearable.Activity;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using AndroidX.RecyclerView.Widget;
using AndroidX.Wear.Widget;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Model.RequestParams;
using WearVK.RecyclerAdapters;

namespace WearVK
{
    [Activity(Label = "Chat")]
    public class ChatActivity : WearableActivity
    {
        private WearableRecyclerView _recyclerView;
        private EditText _messageTxt;
        private long _peerId;
        private readonly Random _rnd = new Random();
        private ChatAdapter _adapter;
        private int _loadedPages = 0;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_chat);

            var intent = Intent;
            _peerId = intent.GetLongExtra("peerId", 0);
            _recyclerView = FindViewById<WearableRecyclerView>(Resource.Id.recycler_chat_view);
            _messageTxt = FindViewById<EditText>(Resource.Id.chatMessageTxt);
            var sendBtn = FindViewById<ImageButton>(Resource.Id.chatSendBtn);

            sendBtn.Click += (sender, args) =>
            {
                _ = SendMessage();
            };

            _adapter = new ChatAdapter();
            _recyclerView.GenericMotion += Recycler_GenericMotion;
            _recyclerView.ScrollChange += RecyclerViewOnScrollChange; 
            _recyclerView.EdgeItemsCenteringEnabled = true;
            /*_recyclerView.SetLayoutManager(new WearableLinearLayoutManager(this, new CustomScrollingLayoutCallback())
            {
                ReverseLayout = true
            });*/
            _recyclerView.SetLayoutManager(new LinearLayoutManager(this)
            {
                ReverseLayout = true
            });
            _recyclerView.SetAdapter(_adapter);
            _recyclerView.RequestFocus();

            _ = LoadMore();
        }

        private void RecyclerViewOnScrollChange(object sender, View.ScrollChangeEventArgs e)
        {
            if (!e.V.CanScrollVertically(-1))
                _ = LoadMore();
        }

        private async Task LoadMore()
        {
            try
            {
                var messages = await MainActivity.VK.Messages.GetHistoryAsync(new MessagesGetHistoryParams
                {
                    PeerId = _peerId,
                    Extended = true,
                    Reversed = false,
                    Offset = _loadedPages++ * 20,
                    Count = 20,
                });
                Bitmap opImage;
                string opName;
                using var client = new HttpClient();
                if (_peerId < 2e9)
                {
                    if (_peerId < 0)
                    {
                        var group = (await MainActivity.VK.Groups.GetByIdAsync(new[] { (-_peerId).ToString() },
                            (-_peerId).ToString(), GroupsFields.All)).First();
                        var bytes = await client.GetByteArrayAsync(group.Photo50);
                        opImage = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                        opName = group.Name;
                    }
                    else
                    {
                        var chatUser = (await MainActivity.VK.Users.GetAsync(new[] { _peerId }, ProfileFields.All))
                            .First();
                        var bytes = await client.GetByteArrayAsync(chatUser.Photo50);
                        opImage = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                        opName = $"{chatUser.FirstName} {chatUser.LastName}";
                    }

                    foreach (var m in messages.Messages)
                    {
                        _adapter.Items.Add(m.FromId == MainActivity.VK.UserId
                            ? (MainActivity.UserImage, MainActivity.UserName, m)
                            : (opImage, opName, m));
                    }
                }
                else
                {
                    foreach (var m in messages.Messages)
                    {
                        if (m.FromId < 0)
                        {
                            var group = (await MainActivity.VK.Groups.GetByIdAsync(new[] { (-m.FromId).ToString() },
                                (-m.FromId).ToString(), GroupsFields.All)).First();
                            var bytes = await client.GetByteArrayAsync(group.Photo50);
                            opImage = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                            opName = group.Name;
                        }
                        else if (m.FromId == MainActivity.VK.UserId)
                        {
                            opImage = MainActivity.UserImage;
                            opName = MainActivity.UserName;
                        }
                        else
                        {
                            var chatUser =
                                (await MainActivity.VK.Users.GetAsync(new[] { m.FromId.Value }, ProfileFields.All))
                                .First();
                            var bytes = await client.GetByteArrayAsync(chatUser.Photo50);
                            opImage = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                            opName = $"{chatUser.FirstName} {chatUser.LastName}";
                        }

                        _adapter.Items.Add((opImage, opName, m));
                    }
                }

                _adapter.NotifyDataSetChanged();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, $"Exception occured: {ex.Message}", ToastLength.Long).Show();
            }
        }

        private async Task SendMessage()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_messageTxt.Text))
                    return;
                await MainActivity.VK.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = _peerId,
                    RandomId = _rnd.Next(),
                    Message = _messageTxt.Text
                });
                _messageTxt.Text = "";
                _ = LoadMore();
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