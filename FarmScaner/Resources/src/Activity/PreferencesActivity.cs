using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using FarmScaner.Models;
using FarmScaner.Source;

namespace FarmScaner.Helpers
{
    [Activity(
        ConfigurationChanges = ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Portrait,
        Theme = "@style/AppTheme")]
    public class PreferencesActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Settingslayout);

            CheckBox IsUseTestServer = FindViewById<CheckBox>(Resource.Id.Settings_IsUseTestServer);
            IsUseTestServer.Checked = AppSettings.IsUseTestServer;

            IsUseTestServer.CheckedChange += delegate { AppSettings.IsUseTestServer = IsUseTestServer.Checked; };
            FindViewById<Toolbar>(Resource.Id.toolbar).NavigationOnClick += delegate { OnBackPressed(); };
            FindViewById<Button>(Resource.Id.Settings_DownloadUpdate).Click += delegate { App.UpdateManager.CheckAndDownloadUpdate(this, false).ConfigureAwait(false); };
            FindViewById<Button>(Resource.Id.Settings_ClearSettings).Click += delegate { AppSettings.ClearUser(); };
            FindViewById<TextView>(Resource.Id.Settings_AppVersion).Text = Resources.GetString(Resource.String.AppVersion) + " " + PackageManager.GetPackageInfo(PackageName, 0).VersionName;
        }

    }
}
