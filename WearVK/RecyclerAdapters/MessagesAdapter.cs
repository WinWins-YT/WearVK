using System;
using System.Collections.Generic;
using Android.Graphics;
using Android.Locations;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace WearVK.RecyclerAdapters
{
    public class MessagesAdapter : RecyclerView.Adapter
    {
        public List<(Bitmap, string, long)> Items { get; } = new List<(Bitmap, string, long)>();
        public event EventHandler OnItemClicked;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = Items[position];
            var viewHolder = holder as MessagesViewHolder;
            
            viewHolder.ImageView.SetImageBitmap(item.Item1);
            viewHolder.TextView.Text = item.Item2;
            viewHolder.ImageView.ContentDescription = item.Item3.ToString();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.FromContext(parent.Context)
                .Inflate(Resource.Layout.main_menu_row_item, parent, false);

            if (OnItemClicked != null)
                view.Click += OnItemClicked;

            return new MessagesViewHolder(view);
        }

        public override int ItemCount => Items.Count;

        class MessagesViewHolder : RecyclerView.ViewHolder
        {
            public ImageView ImageView { get; set; }
            public TextView TextView { get; set; }
            public MessagesViewHolder(View itemView) : base(itemView)
            {
                ImageView = itemView.FindViewById<ImageView>(Resource.Id.mainMenuDrawable);
                TextView = itemView.FindViewById<TextView>(Resource.Id.mainMenuText);
            }
        }
    }
}