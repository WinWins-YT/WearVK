using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Support.Wearable.Activity;
using Android.Widget;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.AudioBypassService.Extensions;
using VkNet.Model;
using Button = Android.Widget.Button;

namespace WearVK
{
    [Activity(Label = "Login", MainLauncher = false)]
    public class LoginActivity : WearableActivity
    {
        Button loginBtn, twoFactorBtn;
        EditText loginTxt, passTxt, twoFactorTxt;
        LinearLayout mainLayout, twoFactorLayout;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activity_login);

            loginBtn = FindViewById<Button>(Resource.Id.loginLoginBtn);
            twoFactorBtn = FindViewById<Button>(Resource.Id.loginTwoFactorBtn);
            loginTxt = FindViewById<EditText>(Resource.Id.loginLoginTxt);
            passTxt = FindViewById<EditText>(Resource.Id.loginPassTxt);
            twoFactorTxt = FindViewById<EditText>(Resource.Id.loginTwoFactorCodeTxt);
            mainLayout = FindViewById<LinearLayout>(Resource.Id.loginLoginLayout);
            twoFactorLayout = FindViewById<LinearLayout>(Resource.Id.loginTwoFactorLayout);

            loginBtn.Click += LoginBtn_Click;
        }

        private void LoginBtn_Click(object sender, System.EventArgs e)
        {
            loginBtn.Enabled = false;
            _ = Login();
        }

        private async Task Login()
        {
            var prefEditor = GetSharedPreferences("General", FileCreationMode.Private).Edit();

            try
            {
                var services = new ServiceCollection();
                services.AddAudioBypass();
                var vk = new VkApi(services);
                await vk.AuthorizeAsync(new ApiAuthParams()
                {
                    ApplicationId = 2274003,
                    Login = loginTxt.Text,
                    Password = passTxt.Text,
                    IsTokenUpdateAutomatically = true,
                    Settings = VkNet.Enums.Filters.Settings.All,
                    TwoFactorAuthorization = GetTwoFactor
                });

                prefEditor.PutBoolean("loggedIn", true);
                prefEditor.PutString("token", vk.Token);
                prefEditor.Apply();
                
                Finish();
                StartActivity(new Intent(this, typeof(MainActivity)));
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, $"Exception occured: {ex.Message}", ToastLength.Long).Show();
                RunOnUiThread(() => loginBtn.Enabled = true);
            }
        }

        private string GetTwoFactor()
        {
            mainLayout.Visibility = Android.Views.ViewStates.Gone;
            twoFactorLayout.Visibility = Android.Views.ViewStates.Visible;
            bool ok = false;

            twoFactorBtn.Click += (s, e) =>
            {
                twoFactorBtn.Enabled = false;
                ok = true;
            };

            while (!ok) { }

            string code = twoFactorTxt.Text;
            twoFactorTxt.Text = "";
            twoFactorBtn.Enabled = true;
            twoFactorLayout.Visibility = Android.Views.ViewStates.Gone;
            mainLayout.Visibility = Android.Views.ViewStates.Visible;

            return code;
        }
    }
}