using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.Attachments;
using WearVK;

namespace WearVK.RecyclerAdapters
{
    public class CommentsAdapter : RecyclerView.Adapter
    {
        public List<Comment> Items { get; } = new List<Comment>();
        public override int ItemCount => Items.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            _ = DownloadComment(Items[position], holder);
        }

        private async Task DownloadComment(Comment comment, RecyclerView.ViewHolder holder)
        {
            using var client = new HttpClient();
            var user = (await MainActivity.VK.Users.GetAsync(new long[] { comment.FromId.Value }, ProfileFields.Photo50 | ProfileFields.FirstName | ProfileFields.LastName)).First();
            var bytes = await client.GetByteArrayAsync(user.Photo50.ToString());
            var bitmap = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
            (holder as CommentsViewHolder).UserImage.SetImageBitmap(bitmap);
            (holder as CommentsViewHolder).UserName.Text = $"{user.FirstName} {user.LastName}";
            (holder as CommentsViewHolder).CommentText.Text = comment.Text;
            if (comment.Attachments.Any())
            {
                var photo = comment.Attachments.First(x => x.Instance is Photo).Instance as Photo;
                bytes = await client.GetByteArrayAsync(photo.Sizes.OrderBy(x => x.Height).Last().Src);
                bitmap = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                (holder as CommentsViewHolder).Image.SetImageBitmap(bitmap);
                (holder as CommentsViewHolder).Image.Visibility = ViewStates.Visible;
            }
            else
                (holder as CommentsViewHolder).Image.Visibility = ViewStates.Gone;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View view = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.comment_row_section, parent, false);

            return new CommentsViewHolder(view);
        }

        class CommentsViewHolder : RecyclerView.ViewHolder
        {
            public ImageView UserImage { get; private set; }
            public TextView UserName { get; private set; }
            public TextView CommentText { get; private set; }
            public ImageView Image { get; private set; }
            public CommentsViewHolder(View itemView) : base(itemView)
            {
                UserImage = itemView.FindViewById<ImageView>(Resource.Id.comUserImage);
                UserName = itemView.FindViewById<TextView>(Resource.Id.comUserName);
                CommentText = itemView.FindViewById<TextView>(Resource.Id.comText);
                Image = itemView.FindViewById<ImageView>(Resource.Id.comImage);
            }
        }
    }
}