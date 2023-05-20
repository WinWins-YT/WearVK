using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Wearable.Activity;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using AndroidX.RecyclerView.Widget;
using AndroidX.Wear.Widget;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Threading.Tasks;
using VkNet;
using VkNet.AudioBypassService.Extensions;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.RequestParams;
using WearVK.RecyclerAdapters;
using Button = Android.Widget.Button;

namespace WearVK
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : WearableActivity
    {
        public static VkApi VK { get; private set; }
        public static Bitmap UserImage { get; private set; }
        public static string UserName { get; private set; }
        MainMenuAdapter adapter = new MainMenuAdapter();
        ProgressBar progressBar;
        WearableRecyclerView recyclerView;
        LinearLayout loginLayout;
        TextView loginText;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activity_main);

            var pref = GetSharedPreferences("General", FileCreationMode.Private);
            if (!pref.GetBoolean("loggedIn", false))
            {
                Intent intent = new Intent(this, typeof(LoginActivity));
                StartActivity(intent);
                Finish();
                return;
            }

            Toast.MakeText(ApplicationContext, "Logging in...", ToastLength.Short).Show();

            progressBar = FindViewById<ProgressBar>(Resource.Id.mainProgress);
            recyclerView = FindViewById<WearableRecyclerView>(Resource.Id.recycler_launcher_view);
            loginLayout = FindViewById<LinearLayout>(Resource.Id.mainMenuLoginLayout);
            loginText = FindViewById<TextView>(Resource.Id.mainMenuLoginText);
            Button loginConfirm = FindViewById<Button>(Resource.Id.mainMenuLoginBtnYes);
            Button loginCancel = FindViewById<Button>(Resource.Id.mainMenuLoginBtnNo);

            loginCancel.Click += (s, e) =>
            {
                if (!VK.UserId.HasValue || VK.UserId == 0)
                {
                    progressBar.Visibility = ViewStates.Visible;
                    loginLayout.Visibility = ViewStates.Gone;
                    _ = LoadInfo();
                }
                else
                {
                    loginLayout.Visibility = ViewStates.Gone;
                    recyclerView.Visibility = ViewStates.Visible;
                    recyclerView.RequestFocus();
                }
            };

            loginConfirm.Click += (s, e) =>
            {
                var editor = pref.Edit();
                editor.PutBoolean("loggedIn", false);
                editor.Apply();
                Intent intent = new Intent(this, typeof(LoginActivity));
                StartActivity(intent);
                Finish();
            };
            
            adapter.OnItemClicked += Recycler_OnItemClicked;
            recyclerView.GenericMotion += Recycler_GenericMotion;
            recyclerView.EdgeItemsCenteringEnabled = true;
            recyclerView.SetLayoutManager(new WearableLinearLayoutManager(this, new CustomScrollingLayoutCallback()));
            recyclerView.SetAdapter(adapter);
            recyclerView.RequestFocus();
            _ = LoadInfo();
        }

        private void Recycler_OnItemClicked(object sender, EventArgs e)
        {
            var layout = (LinearLayout)((LinearLayout)sender).GetChildAt(0);
            var desc = layout.GetChildAt(0).ContentDescription;
            if (desc == "Profile")
            {
                loginText.Text = "Log out?";
                loginLayout.Visibility = ViewStates.Visible;
                recyclerView.Visibility = ViewStates.Invisible;
            }
            else if (desc == "News")
            {
                StartActivity(new Intent(this, typeof(NewsActivity)));
            }
            else if (desc == "Messages")
                StartActivity(new Intent(this, typeof(MessagesActivity)));
        }

        private async Task LoadInfo()
        {
            var pref = GetSharedPreferences("General", FileCreationMode.Private);
            try
            {
                var services = new ServiceCollection();
                services.AddAudioBypass();
                VK = new VkApi(services);
                await VK.AuthorizeAsync(new ApiAuthParams()
                {
                    ApplicationId = 7960313,
                    //AccessToken = pref.GetString("token", "")
                    Login = pref.GetString("login", ""),
                    Password = pref.GetString("password", ""),
                    Settings = Settings.All
                });
                Toast.MakeText(this, "Logged in!", ToastLength.Short).Show();
                using var client = new HttpClient();
                var user = (await VK.Users.GetAsync(new long[] { VK.UserId.Value }, ProfileFields.FirstName | ProfileFields.LastName | ProfileFields.Photo50)).First();
                var bytes = await client.GetByteArrayAsync(user.Photo50);
                UserImage = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                UserName = $"{user.FirstName} {user.LastName}";
                adapter.Header = (UserImage, UserName);
                adapter.Items.Add((GetDrawable(Resource.Drawable.ic_density_small), "News"));
                adapter.Items.Add((GetDrawable(Resource.Drawable.ic_mail), "Messages"));
                adapter.NotifyDataSetChanged();
                progressBar.Visibility = ViewStates.Gone;
                recyclerView.Visibility = ViewStates.Visible;
                recyclerView.RequestFocus();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, $"Exception occured: {ex.Message}", ToastLength.Long).Show();
                progressBar.Visibility = ViewStates.Gone;
                loginLayout.Visibility = ViewStates.Visible;
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

    public class CustomScrollingLayoutCallback : WearableLinearLayoutManager.LayoutCallback
    {
        /** How much icons should scale, at most. */
        private static float MAX_ICON_PROGRESS = 0.65f;

        private float progressToCenter;

        public override void OnLayoutFinished(View child, RecyclerView parent)
        {
            // Figure out % progress from top to bottom.
            float centerOffset = (child.Height / 2.0f) / parent.Height;
            float yRelativeToCenterOffset = ((float)child.GetY() / parent.Height) + centerOffset;

            // Normalize for center.
            progressToCenter = Math.Abs(0.5f - yRelativeToCenterOffset);
            // Adjust to the maximum scale.
            progressToCenter = Math.Min(progressToCenter, MAX_ICON_PROGRESS);

            child.ScaleX = 1 - progressToCenter;
            child.ScaleY = 1 - progressToCenter;
        }
    }
}