using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using VkNet.Enums.Filters;
using VkNet.Model;
using Button = Android.Widget.Button;

namespace WearVK.RecyclerAdapters
{
    public class NewsAdapter : RecyclerView.Adapter
    {
        private readonly Context _context;
        public List<(string, NewsItem)> Items { get; } = new List<(string, NewsItem)>();

        public NewsAdapter(Context context)
        {
            _context = context;
        }
        
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            _ = DownloadPost(Items[position], (NewsViewHolder)holder);
        }

        private async Task DownloadPost((string, NewsItem) post, NewsViewHolder holder)
        {
            using var client = new HttpClient();

            if (post.Item1 != null)
            {
                var bytes = await client.GetByteArrayAsync(post.Item1);
                var bitmap = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                holder.imageView.SetImageBitmap(bitmap);
                holder.imageView.Visibility = ViewStates.Visible;
            }
            else
                holder.imageView.Visibility = ViewStates.Gone;
            
            var n = post.Item2;
            if (n.SourceId < 0)
            {
                var group = (await MainActivity.VK.Groups.GetByIdAsync(new string[] { (-n.SourceId).ToString() },
                    (-n.SourceId).ToString(), GroupsFields.All)).First();
                var bytes = await client.GetByteArrayAsync(group.Photo50);
                var groupPic = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                holder.groupImage.SetImageBitmap(groupPic);
                holder.groupName.Text = group.Name;
            }
            else
            {
                var user = (await MainActivity.VK.Users.GetAsync(new long[] { n.SourceId }, ProfileFields.All)).First();
                var bytes = await client.GetByteArrayAsync(user.Photo50);
                var groupPic = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                holder.groupImage.SetImageBitmap(groupPic);
                holder.groupName.Text = $"{user.FirstName} {user.LastName}";
            }
            holder.textView.Text = n.Text;
            holder.likeButton.Text = $"Like ({n.Likes.Count})";
            holder.likeButton.ContentDescription = $"{n.SourceId}_{n.PostId}";
            holder.comButton.Text = $"Comment ({n.Comments.Count})";
            holder.comButton.ContentDescription = $"{n.SourceId}_{n.PostId}";
            holder.dateText.Text = n.Date.ToString();
            holder.progressBar.Visibility = ViewStates.Gone;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View view = LayoutInflater.FromContext(parent.Context)
                .Inflate(Resource.Layout.post_row_item, parent, false);

            return new NewsViewHolder(view, _context);
        }

        public override int ItemCount => Items.Count;

        class NewsViewHolder : RecyclerView.ViewHolder
        {
            private readonly Context context;

            public long Id { get; set; }
            public ImageView imageView { get; private set; }
            public ProgressBar progressBar { get; private set; }
            public TextView textView { get; private set; }
            public Button likeButton { get; private set; }
            public Button comButton { get; private set; }
            public ImageView groupImage { get; private set; }
            public TextView groupName { get; private set; }
            public TextView dateText { get; private set; }

            public NewsViewHolder(View itemView, Context context) : base(itemView)
            {
                imageView = itemView.FindViewById<ImageView>(Resource.Id.postImage);
                progressBar = itemView.FindViewById<ProgressBar>(Resource.Id.postProgress);
                textView = itemView.FindViewById<TextView>(Resource.Id.postText);
                likeButton = itemView.FindViewById<Button>(Resource.Id.postLikeBtn);
                likeButton.Click += Button_Click1;
                comButton = itemView.FindViewById<Button>(Resource.Id.postCommentBtn);
                comButton.Click += ComButton_Click1;
                this.context = context;
                groupImage = itemView.FindViewById<ImageView>(Resource.Id.postGroupImage);
                groupName = itemView.FindViewById<TextView>(Resource.Id.postGroupNameTxt);
                dateText = itemView.FindViewById<TextView>(Resource.Id.postDateTxt);
            }

            private void ComButton_Click1(object sender, EventArgs e)
            {
                Intent i = new Intent(context, typeof(CommentsActivity));
                i.PutExtra("PostId", Convert.ToInt64(((View)sender).ContentDescription.Split('_')[1]));
                i.PutExtra("OwnerId", Convert.ToInt64(((View)sender).ContentDescription.Split('_')[0]));
                context.StartActivity(i);
            }

            private void Button_Click1(object sender, EventArgs e)
            {
                _ = Like((Button)sender);
            }

            private async Task Like(Button sender)
            {
                Toast.MakeText(context, "Wait...", ToastLength.Short).Show();
                long id = Convert.ToInt64(sender.ContentDescription.Split('_')[1]);
                long ownerId = Convert.ToInt64(sender.ContentDescription.Split('_')[0]);
                await MainActivity.VK.Likes.AddAsync(new VkNet.Model.RequestParams.LikesAddParams()
                {
                    ItemId = id,
                    OwnerId = ownerId,
                    Type = VkNet.Enums.SafetyEnums.LikeObjectType.Post
                });
                Toast.MakeText(context, "Liked!", ToastLength.Short).Show();
            }
        }
    }
}