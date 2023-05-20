using Android.Graphics;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using VkNet.Model.Attachments;
using VkNet;
using AndroidX.RecyclerView.Widget;
using Android.Content;

namespace WearVK.RecyclerAdapters
{
    public class PostsAdapter : RecyclerView.Adapter
    {
        private readonly Context context;
        private readonly VkApi vk;
        private readonly long _groupId;

        public List<(long, string, Post)> Items { get; } = new List<(long, string, Post)>();
        public override int ItemCount => Items.Count;

        public PostsAdapter(Context context, VkApi vk, long groupId)
        {
            this.context = context;
            this.vk = vk;
            _groupId = groupId;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            _ = DownloadBitmap(Items[position].Item1, Items[position].Item2, Items[position].Item3, holder, position);
        }

        private async Task DownloadBitmap(long id, string url, Post post, RecyclerView.ViewHolder holder, int position)
        {
            using var client = new HttpClient();
            var bytes = await client.GetByteArrayAsync(url);
            var bitmap = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
            (holder as PostsViewHolder).progressBar.Visibility = ViewStates.Gone;
            (holder as PostsViewHolder).imageView.Visibility = ViewStates.Visible;
            (holder as PostsViewHolder).imageView.SetImageBitmap(bitmap);
            (holder as PostsViewHolder).textView.Text = post.Text;
            (holder as PostsViewHolder).button.Text = $"Like ({post.Likes.Count})";
            (holder as PostsViewHolder).button.ContentDescription = id.ToString();
            (holder as PostsViewHolder).comButton.Text = $"Comments ({post.Comments.Count})";
            (holder as PostsViewHolder).comButton.ContentDescription = post.Id.ToString();
            (holder as PostsViewHolder).groupImage.SetImageBitmap(PostsActivity.groupPic);
            (holder as PostsViewHolder).groupName.Text = PostsActivity.groupName;
            (holder as PostsViewHolder).dateText.Text = post.Date.ToString();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View view = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.post_row_item, parent, false);

            return new PostsViewHolder(view, context, _groupId);
        }

        public class PostsViewHolder : RecyclerView.ViewHolder
        {
            private readonly Context context;
            private readonly long _groupId;

            public long Id { get; set; }
            public ImageView imageView { get; private set; }
            public ProgressBar progressBar { get; private set; }
            public TextView textView { get; private set; }
            public Button button { get; private set; }
            public Button comButton { get; private set; }
            public ImageView groupImage { get; private set; }
            public TextView groupName { get; private set; }
            public TextView dateText { get; private set; }

            public PostsViewHolder(View itemView, Context context, long groupId) : base(itemView)
            {
                imageView = itemView.FindViewById<ImageView>(Resource.Id.postImage);
                progressBar = itemView.FindViewById<ProgressBar>(Resource.Id.postProgress);
                textView = itemView.FindViewById<TextView>(Resource.Id.postText);
                button = itemView.FindViewById<Button>(Resource.Id.postLikeBtn);
                button.Click += Button_Click1;
                comButton = itemView.FindViewById<Button>(Resource.Id.postCommentBtn);
                comButton.Click += ComButton_Click1;
                this.context = context;
                _groupId = groupId;
                groupImage = itemView.FindViewById<ImageView>(Resource.Id.postGroupImage);
                groupName = itemView.FindViewById<TextView>(Resource.Id.postGroupNameTxt);
                dateText = itemView.FindViewById<TextView>(Resource.Id.postDateTxt);
            }

            private void ComButton_Click1(object sender, EventArgs e)
            {
                Intent i = new Intent(context, typeof(CommentsActivity));
                i.PutExtra("PostId", Convert.ToInt64(((View)sender).ContentDescription));
                i.PutExtra("OwnerId", -_groupId);
                context.StartActivity(i);
            }

            private void Button_Click1(object sender, EventArgs e)
            {
                _ = Like((Button)sender);
            }

            private async Task Like(Button sender)
            {
                Toast.MakeText(context, "Wait...", ToastLength.Short).Show();
                long id = Convert.ToInt64(sender.ContentDescription);
                await MainActivity.VK.Likes.AddAsync(new VkNet.Model.RequestParams.LikesAddParams()
                {
                    ItemId = id,
                    OwnerId = -PostsActivity.GroupId,
                    Type = VkNet.Enums.SafetyEnums.LikeObjectType.Photo
                });
                Toast.MakeText(context, "Liked!", ToastLength.Short).Show();
            }
        }
    }
}