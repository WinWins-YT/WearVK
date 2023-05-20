using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Wearable.Activity;
using Android.Widget;
using Microsoft.Extensions.DependencyInjection;
using System;
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
        Button loginBtn;
        EditText loginTxt;
        EditText passTxt;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activity_login);

            loginBtn = FindViewById<Button>(Resource.Id.loginLoginBtn);
            loginTxt = FindViewById<EditText>(Resource.Id.loginLoginTxt);
            passTxt = FindViewById<EditText>(Resource.Id.loginPassTxt);

            loginBtn.Click += LoginBtn_Click;
        }

        private void LoginBtn_Click(object sender, System.EventArgs e)
        {
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
                    ApplicationId = 7960313,
                    Login = loginTxt.Text,
                    Password = passTxt.Text,
                });

                prefEditor.PutBoolean("loggedIn", true);
                prefEditor.PutString("token", vk.Token);
                prefEditor.PutString("login", loginTxt.Text);
                prefEditor.PutString("password", passTxt.Text);
                prefEditor.Apply();           
                
                Finish();
                StartActivity(new Intent(this, typeof(MainActivity)));
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, $"Exception occured: {ex.Message}", ToastLength.Long).Show();
            }
        }
    }
}