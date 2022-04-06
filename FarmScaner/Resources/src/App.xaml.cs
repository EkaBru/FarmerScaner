using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using FarmScaner.layoutClasses;
using FarmScaner.Models;
using Java.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FarmScaner.Source
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class App : ContentPage
    {
        static Client RESTClient_;
        static Msg Msg_;
        public App()
        {
            InitializeComponent();
        }
        public static Client RESTClient
        {
            get
            {
                if (RESTClient_ == null)
                    RESTClient_ = new Client();
                return RESTClient_;
            }
        }
        public static Models.Msg Msg
        {
            get
            {
                if (Msg_ == null)
                    Msg_ = new Models.Msg();
                return Msg_;
            }
        }
        public static class UpdateManager
        {

            static long CurrentDownloadID = -1;
            static Java.IO.File file;
            static DownloadManager downloadManager;
            static LoadingFrame loading;
            internal class DownloadBroadcastReceiver : BroadcastReceiver
            {
                public override void OnReceive(Context context, Intent intent)
                {
                    long Id = intent.GetLongExtra(DownloadManager.ExtraDownloadId, -1);
                    if (Id == CurrentDownloadID)
                    {
                        loading.Dismiss();
                        if (file.Name == "datawedge.db") // импорт настроек для DataWedge
                        {
                            try
                            {
                                FileOutputStream fos = null;
                                string autoImportDir = "/enterprise/device/settings/datawedge/autoimport/";
                                string temporaryFileName = "datawedge.tmp";
                                string finalFileName = "datawedge.db";
                                Java.IO.File outputDirectory = new Java.IO.File(autoImportDir);
                                Java.IO.File outputFile = new Java.IO.File(outputDirectory, temporaryFileName);
                                Java.IO.File finalFile = new Java.IO.File(outputDirectory, finalFileName);
                                fos = new FileOutputStream(outputFile);
                                FileInputStream fis = new FileInputStream(file);
                                byte[] buffer = new byte[1024];
                                int length;
                                int tot = 0;
                                while ((length = fis.Read(buffer)) > 0)
                                {
                                    fos.Write(buffer, 0, length);
                                    tot += length;
                                }
                                fos.Flush();
                                try
                                {
                                    fos.Close();
                                }
                                finally
                                {
                                    outputFile.SetExecutable(true, false);
                                    outputFile.SetReadable(true, false);
                                    outputFile.SetWritable(true, false);
                                    outputFile.RenameTo(finalFile);
                                }
                                Msg.ShowToastShort(context, "Выполнен импорт настроек Zebra");
                                AppSettings.DataWedgeIsLoded = true;
                            }
                            catch (Exception e)
                            {
                                Msg.ShowToastShort(context, "Ошибка импорта настроек Zebra. " + e.Message);
                            }
                        }
                        else
                        {

                            Intent i = new Intent(Intent.ActionView);
                            i.SetFlags(ActivityFlags.ClearTop);
                            i.SetFlags(ActivityFlags.GrantReadUriPermission);
                            i.SetDataAndType(
                                FileProvider.GetUriForFile(context, context.Resources.GetString(Resource.String.Authority_Main), file),
                            downloadManager.GetMimeTypeForDownloadedFile(Id));
                            context.StartActivity(i);
                            context.UnregisterReceiver(this);
                        }
                    }
                }
            }

            public static async Task<Boolean> CheckAndDownloadUpdate(Context context, bool IsNeedLockAndQuestion = true)
            {
                string Ver = context.PackageManager.GetPackageInfo(context.PackageName, 0).VersionName;
                CheckUpatesRequest Data = new CheckUpatesRequest(Ver, Build.Manufacturer, Build.Model);
                CheckUpatesResponse Resp = await RESTClient.PostResp<CheckUpatesResponse>(context, Data);


                if (Resp?.RequestData != null)
                {
                    if (!AppSettings.DataWedgeIsLoded && !string.IsNullOrEmpty(Resp.RequestData[0][0].DatawedgeLnk))
                    {
                        DownloadUpdate(
                            context,
                            Resp.RequestData[0][0].DatawedgeFileName,
                            Resp.RequestData[0][0].DatawedgeLnk,
                            string.Empty,
                            DownloadVisibility.Hidden
                            );
                    }

                    if (Resp.RequestData[0][0].IsNeedUpdate)
                    {
                        Action Download = () =>
                        {
                            DownloadUpdate(
                                context,
                                Resp.RequestData[0][0].DownloadFileName,
                                Resp.RequestData[0][0].DownloadLnk,
                                context.Resources.GetString(Resource.String.MimeType_App),
                                DownloadVisibility.Visible | DownloadVisibility.VisibleNotifyCompleted);
                        };
                        if (IsNeedLockAndQuestion)
                        {
                            Msg.ShowDialog(context, context.Resources.GetString(Resource.String.HaveUpdate), context.Resources.GetString(Resource.String.Question_DownloadUpdate),
                                () => { Download(); },
                                null,
                                false);
                        }
                        else
                            Download();

                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                    return true;
            }

            public static void DownloadUpdate(Context context, string FileName, string DownloadLink, string MimeType, DownloadVisibility NotificationVisibility)
            {
                try
                {
                    file = new Java.IO.File(context.GetExternalFilesDir(null), FileName);
                    if (file.Exists())
                        file.Delete();

                    Android.Net.Uri Lnk = Android.Net.Uri.Parse(DownloadLink);

                    DownloadManager.Request request = new DownloadManager.Request(Lnk)
                        .SetTitle(FileName)
                        .SetDescription(context.Resources.GetString(Resource.String.DownloadStarted))
                        .SetMimeType(MimeType)
                        .SetDestinationUri(Android.Net.Uri.FromFile(file))
                        .SetNotificationVisibility(NotificationVisibility)
                        .SetAllowedNetworkTypes(DownloadNetwork.Mobile | DownloadNetwork.Wifi);

                    context.RegisterReceiver(new DownloadBroadcastReceiver(), new IntentFilter(DownloadManager.ActionDownloadComplete));
                    downloadManager = (DownloadManager)context.GetSystemService(Context.DownloadService);

                    CurrentDownloadID = downloadManager.Enqueue(request);
                    loading = new LoadingFrame(context, false);
                    loading.Show();
                }
                catch (PackageManager.NameNotFoundException e)
                {
                    e.PrintStackTrace();
                }
            }

        }
        public static string GetEAN13(string Value)
        {
            if (!Value.All(char.IsNumber))
                return string.Empty;
            else if (Value.Length == 12)
            {
                int Sum = 0;
                for (int Pos = 0; Pos < Value.Length; Pos++)
                {
                    Sum += Convert.ToInt32(Value[Pos].ToString()) * ((Pos % 2 == 0) ? 1 : 3);
                }

                return Value + (10 - Sum % 10).ToString();
            }
            else
            if (Value.Length == 13)
                return Value;
            else
                return string.Empty;

        }
    }

}