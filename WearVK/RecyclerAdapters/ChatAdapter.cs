using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using JetBrains.Annotations;
using VkNet.Model;
using VkNet.Model.Attachments;

namespace WearVK.RecyclerAdapters
{
    public class ChatAdapter : RecyclerView.Adapter
    {
        public List<(Bitmap, string, Message)> Items { get; } = new List<(Bitmap, string, Message)>();
        
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = holder as ChatViewHolder;
            var item = Items[position];
            
            viewHolder.AuthorImage.SetImageBitmap(item.Item1);
            viewHolder.AuthorName.Text = item.Item2;
            viewHolder.MessageText.Text = item.Item3.Text;
            _ = GetImage(item.Item3, viewHolder);
        }

        private async Task GetImage(Message message, ChatViewHolder holder)
        {
            using var client = new HttpClient();
            if (message.Attachments.Any())
            {
                var photo = (Photo)message.Attachments.First(x => x.Instance is Photo).Instance;
                var bytes = await client.GetByteArrayAsync(photo.Sizes.OrderBy(x => x.Height).Last().Url);
                var bitmap = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                holder.Image.SetImageBitmap(bitmap);
                holder.Image.Visibility = ViewStates.Visible;
            }
            else
            {
                holder.Image.Visibility = ViewStates.Gone;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.FromContext(parent.Context)
                .Inflate(Resource.Layout.comment_row_section, parent, false);

            return new ChatViewHolder(view);
        }

        public override int ItemCount => Items.Count;

        class ChatViewHolder : RecyclerView.ViewHolder
        {
            public ImageView AuthorImage { get; set; }
            public TextView AuthorName { get; set; }
            public TextView MessageText { get; set; }
            public ImageView Image { get; set; }
            
            public ChatViewHolder([NotNull] View itemView) : base(itemView)
            {
                AuthorImage = itemView.FindViewById<ImageView>(Resource.Id.comUserImage);
                AuthorName = itemView.FindViewById<TextView>(Resource.Id.comUserName);
                MessageText = itemView.FindViewById<TextView>(Resource.Id.comText);
                Image = itemView.FindViewById<ImageView>(Resource.Id.comImage);
            }
        }
    }
}