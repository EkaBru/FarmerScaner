using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using FarmScaner.Models;
using FarmScaner.Source;
using FarmScaner.Helpers;
using System;
using Android.Content.PM;
using Android.Content;
using Android.Support.V7.App;
using Android.Net;

namespace FarmScaner
{
    [Activity(
        ConfigurationChanges = ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Portrait, 
        Label = "@string/app_caption", 
        Theme = "@style/AppTheme", 
        MainLauncher = true)]
    public class StartActivity : AppCompatActivity
    {
        bool doubleBackToExitPressedOnce = false;
        static TextView ConnectionWarning;
        EditText editLogin;
        EditText editPassword;
        Button btnConnent;

        internal class ThisBroadcastReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                ConnectivityManager manager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
                if (manager.ActiveNetwork == null)
                {
                    if (ConnectionWarning != null)
                    {
                        ConnectionWarning.Visibility = ViewStates.Visible;
                        context.Theme.ApplyStyle(Resource.Style.AppThemeWrong, true);
                        ConnectionWarning.RequestLayout();
                    }
                }
                else
                {
                    if (ConnectionWarning != null)
                    {
                        ConnectionWarning.Visibility = ViewStates.Gone;
                        context.Theme.ApplyStyle(Resource.Style.AppTheme, true);
                        ConnectionWarning.RequestLayout();
                    }
                }
            }
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Forms.Forms.Init(this, savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.MainLayout);

            ConnectionWarning = FindViewById<TextView>(Resource.Id.MainLayout_ConnectionWarning);
            RegisterReceiver(new ThisBroadcastReceiver(), new IntentFilter(ConnectivityManager.ConnectivityAction));

            editLogin = FindViewById<EditText>(Resource.Id.editLogin);
            editPassword = FindViewById<EditText>(Resource.Id.editPassword);
            btnConnent = FindViewById<Button>(Resource.Id.btnConnent);

            editLogin.Text = AppSettings.Lgn;
            editPassword.Text = AppSettings.Pwd;
            editLogin.AfterTextChanged += UserTextChanged;
            editPassword.AfterTextChanged += UserTextChanged;
            btnConnent.Click += async delegate 
            {
                string lgn = editLogin.Text;
                string pwd = editPassword.Text;

                btnConnent.Text = Resources.GetString(Resource.String.do_wait_connect);
                btnConnent.Enabled = false;
                try
                {
                    if (await App.RESTClient.Login(this, new TokenReqest(lgn, pwd)))
                    {
                        GoToActivity(typeof(WizardActivity));
                    }
                }
                finally
                {
                    btnConnent.Text = Resources.GetString(Resource.String.do_connect);
                    btnConnent.Enabled = true;
                }
            };

            FindViewById<Toolbar>(Resource.Id.MainLayout_Toolbar).NavigationOnClick += delegate { GoToActivity(typeof(PreferencesActivity)); };
        }
        private void UserTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            AppSettings.ClearUser();
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        void GoToActivity(Type type)
        {
            StartActivity(
                new Intent(this, type)
                    .SetFlags(ActivityFlags.ReorderToFront));
        }
        public override void OnBackPressed()
        {
            if (doubleBackToExitPressedOnce)
            {
                base.OnBackPressed();
                return;
            }

            this.doubleBackToExitPressedOnce = true;
            Toast.MakeText(this, Resources.GetString(Resource.String.on_try_to_exit), ToastLength.Short).Show();

            Action myAction = () =>
            {
                doubleBackToExitPressedOnce = false;
            };

            new Handler().PostDelayed(myAction, 2000);
        }
    }

}