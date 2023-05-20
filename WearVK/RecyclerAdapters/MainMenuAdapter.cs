using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;

namespace WearVK.RecyclerAdapters
{
    public class MainMenuAdapter : RecyclerView.Adapter
    {
        private const int TYPE_HEADER = 0;
        private const int TYPE_ITEM = 1;

        public EventHandler OnItemClicked { get; set; }
        public (Bitmap, string) Header { get; set; }
        public List<(Drawable, string)> Items { get; } = new List<(Drawable, string)>();

        public override int ItemCount => Items.Count + 1;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = (MainMenuViewHolder)holder;
            if (position == 0)
            {
                viewHolder.ImageView.SetImageBitmap(Header.Item1);
                viewHolder.TextView.Text = Header.Item2;
                viewHolder.ImageView.ContentDescription = "Profile";
            }
            else
            {
                viewHolder.ImageView.SetImageDrawable(Items[position - 1].Item1);
                viewHolder.TextView.Text = Items[position - 1].Item2;
                viewHolder.ImageView.ContentDescription = Items[position - 1].Item2;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View view = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.main_menu_row_item, parent, false);

            if (OnItemClicked != null)
                view.Click += OnItemClicked;

            return new MainMenuViewHolder(view);
        }

        public override int GetItemViewType(int position)
        {
            if (position == 0)
                return TYPE_HEADER;
            else
                return TYPE_ITEM;
        }

        public class MainMenuViewHolder : RecyclerView.ViewHolder
        {
            public ImageView ImageView { get; set; }
            public TextView TextView { get; set; }

            public MainMenuViewHolder(View itemView) : base(itemView)
            {
                ImageView = itemView.FindViewById<ImageView>(Resource.Id.mainMenuDrawable);
                TextView = itemView.FindViewById<TextView>(Resource.Id.mainMenuText);
            }
        }
    }
}