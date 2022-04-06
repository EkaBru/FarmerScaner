using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using FarmScaner.layoutClasses;
using FarmScaner.Models;
using FarmScaner.Source;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FarmScaner
{
    [Activity(
        ConfigurationChanges = ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Portrait,
        Theme = "@style/AppTheme")]

    class ScanActivity : AppCompatActivity
    {
        private static ScanerAdapter lvScanAdapter;
        private static bool IsFullSrceen = false;
        private static long StorageFromOID;
        private static long StorageToOID;
        private static string ClaimOIDSet;
        private static bool IsCanFindElse;
        private static bool IsRequireComment;
        MoveRowResponse MoveRowResponse;
        MoveRowRequest MoveRowRequest;
        BroadcastReceiver BroadcastReceiver;
        string DataFilePath => Path.Combine(GetExternalFilesDir(null).Path, "data.txt");
        #region Controls
        private TextView LblType => FindViewById<TextView>(Resource.Id.lbl_scan_Type);
        private TextView LblStorageFrom => FindViewById<TextView>(Resource.Id.lbl_scan_StorageFrom);
        private TextView LblStorageTo => FindViewById<TextView>(Resource.Id.lbl_scan_StorageTo);
        private ListView lvMain => FindViewById<ListView>(Resource.Id.lvMain);
        private Button BtnFinishScan => FindViewById<Button>(Resource.Id.btnFinishScan);
        private Button BtnSaveScan => FindViewById<Button>(Resource.Id.btnSaveScan);
        private ImageButton BtnSort => FindViewById<ImageButton>(Resource.Id.ScanLayout_Sort);
        private ImageButton btnFullScreen => FindViewById<ImageButton>(Resource.Id.ScanLayout_FullScreen);
        private LinearLayout lyHead => FindViewById<LinearLayout>(Resource.Id.lyHead);
        private Android.Support.V7.Widget.Toolbar toolbar => FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
        private ImageButton ScanLayout_Camera => FindViewById<ImageButton>(Resource.Id.ScanLayout_Camera);
        private ImageButton ScanLayout_Edit => FindViewById<ImageButton>(Resource.Id.ScanLayout_Edit);
        private LinearLayout lyBottom => FindViewById<LinearLayout>(Resource.Id.LyBottom);
        private TextView lbl_scan_StorageFrom => FindViewById<TextView>(Resource.Id.lbl_scan_StorageFrom);
        private TextView lbl_scan_StorageTo => FindViewById<TextView>(Resource.Id.lbl_scan_StorageTo);
        #endregion

        internal class ThisBroadcastReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                if (lvScanAdapter != null)
                {
                    string Barcode = intent.GetStringExtra(context.Resources.GetString(Resource.String.Intent_BaseExtraKey));
                    ((Activity)context).RunOnUiThread(async () =>
                       {
                           await lvScanAdapter.Find(
                               Barcode,
                               StorageFromOID,
                               StorageToOID,
                               ClaimOIDSet,
                               (IsFind, Position, ErrorMessage) =>
                               {
                                   if (IsFind)
                                   {
                                       ((Activity)context).FindViewById<ListView>(Resource.Id.lvMain)?.SetSelection(Position);
                                   }
                                   if (!string.IsNullOrEmpty(ErrorMessage))
                                       App.Msg.ShowToastShort(context, ErrorMessage);

                               },
                            IsCanFindElse);
                       });
                }
            }
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.ScanLayout);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                lvMain.NestedScrollingEnabled = true;
            }
            toolbar.NavigationClick += delegate { OnBackPressed(); };
            toolbar.SetNavigationIcon(Resource.Drawable.abc_vector_test);

            btnFullScreen.Click += delegate
            {
                IsFullSrceen = !IsFullSrceen;
                SetFullSreenMode();
            };


            SetSortModeImage();      

            BtnSort.Click += delegate { 
                AppSettings.SortMode = AppSettings.SortMode.Next(); 
                SetSortModeImage();
                MoveRowResponse?.RequestData[0]?.Sort();
                lvScanAdapter?.NotifyDataSetChanged();
            };

            Load();
            ScanLayout_Camera.Click += delegate { StartActivity(new Intent(this, typeof(BarcodeCameraActivity))); };
            ScanLayout_Edit.Click += delegate
            {
                if (MoveRowResponse?.RequestData?.Count > 0)
                {
                    EditTextDialog editTextDialog = new EditTextDialog(this, Resources.GetString(Resource.String.ManualInput), "Введите штрихкод...", Android.Text.InputTypes.ClassNumber);
                    editTextDialog
                        .SetPositiveButton(Resources.GetString(Resource.String.DoOk), (senderAlerts, args) =>
                                {
                                    Intent intent = new Intent(Resources.GetString(Resource.String.Intent_ACTION))
                                        .PutExtra(Resources.GetString(Resource.String.Intent_BaseExtraKey), editTextDialog.ResultString);
                                    SendBroadcast(intent);
                                })
                        .SetNegativeButton(Resources.GetString(Resource.String.DoCancel), (senderAlerts, args) => { })
                        .Show();
                }
            };

        }
        void SetSortModeImage()
        {
            switch (AppSettings.SortMode)
            {
                case SortOption.Default:
                    BtnSort.SetImageResource(Resource.Drawable.ic_sort_by_default);
                    break;
                case SortOption.Alphabet:
                    BtnSort.SetImageResource(Resource.Drawable.ic_sort_by_alphabet);
                    break;
                case SortOption.Weight:
                    BtnSort.SetImageResource(Resource.Drawable.ic_sort_by_weight);
                    break;
            }
        }
        void SetFullSreenMode()
        {
            if (IsFullSrceen)
            {
                SystemUiFlags uiOptions = SystemUiFlags.HideNavigation | SystemUiFlags.Fullscreen | SystemUiFlags.LayoutFullscreen | SystemUiFlags.ImmersiveSticky;
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;

                btnFullScreen.SetImageResource(Resource.Drawable.collapse);
                lyHead.Visibility = ViewStates.Gone;
                lyBottom.Visibility = ViewStates.Gone;
            }
            else
            {
                Window.DecorView.SystemUiVisibility = StatusBarVisibility.Visible;

                btnFullScreen.SetImageResource(Resource.Drawable.expand);
                lyHead.Visibility = ViewStates.Visible;
                lyBottom.Visibility = ViewStates.Visible;
            };
        }
        async void Load()
        {
            Intent i = Intent;
            bool IsReceive = i.GetBooleanExtra("IsReceive", false);
            bool IsInvertory = i.GetBooleanExtra("IsInvertory", false);
            bool IsCreate = i.GetBooleanExtra("IsCreate", false);
            int MoveID = i.GetIntExtra("MoveID", 0);
            ClaimOIDSet = i.GetStringExtra("ClaimOIDSet");
            StorageFromOID = i.GetLongExtra("StorageFromOID", 0);
            StorageToOID = i.GetLongExtra("StorageToOID", 0);
            DateTime Date = Convert.ToDateTime( i.GetStringExtra("Date"));
            long MoveTypeOID = i.GetLongExtra("MoveTypeOID", 0);
            long CreateReasonOID = i.GetLongExtra("CreateReasonOID", 0);

            lbl_scan_StorageFrom.Visibility = IsInvertory ? ViewStates.Gone : ViewStates.Visible;
            lbl_scan_StorageTo.Visibility = lbl_scan_StorageFrom.Visibility;
            MoveResponse MoveResponse = await App.RESTClient.PostResp<MoveResponse>(this, new MoveRequest(IsReceive, IsInvertory, IsCreate, MoveID, ClaimOIDSet, StorageFromOID, StorageToOID, MoveTypeOID));

            if ((MoveResponse != null) && (MoveResponse.RequestData != null))
            {
                IsCanFindElse = MoveResponse.RequestData[0][0].IsCanFindElse;
                LblType.Text = MoveResponse.RequestData[0][0].TypeOIDName;
                IsRequireComment = MoveResponse.RequestData[0][0].IsRequireComment;
                LblStorageFrom.Text = Resources.GetString(Resource.String.lbl_StorageFrom) + ' ' + MoveResponse.RequestData[0][0].StorageFromOIDName;
                LblStorageTo.Text = Resources.GetString(Resource.String.lbl_StorageTo) + ' ' + MoveResponse.RequestData[0][0].StorageToOIDName;

                MoveRowRequest = new MoveRowRequest(IsReceive, IsInvertory, MoveID, ClaimOIDSet, StorageFromOID);
                MoveRowResponse = await App.RESTClient.PostResp<MoveRowResponse>(this, MoveRowRequest);
                if (MoveRowResponse?.RequestData?.Count > 0)
                {
                    lvScanAdapter = new ScanerAdapter(
                        this, 
                        MoveRowResponse.RequestData[0], 
                        IsReceive, 
                        IsInvertory,
                        !string.IsNullOrEmpty(ClaimOIDSet), 
                        !string.IsNullOrEmpty(ClaimOIDSet)
                    );
                    lvMain.Adapter = lvScanAdapter;

                    BtnFinishScan.Enabled = true;
                    BtnSaveScan.Enabled = true;

                    BtnFinishScan.Click += delegate
                    {
                        App.Msg.ShowDialog(
                            this, 
                            string.Empty, 
                            (IsReceive ? Resources.GetString(Resource.String.Question_RecieveMove) : IsInvertory ? Resources.GetString(Resource.String.Question_FinishInvertory) : Resources.GetString(Resource.String.Question_SendMove)),
                            () => {
                                List<MoveSendRequest.Rows> Rows = MoveRowResponse.RequestData[0].Select(
                                      r => new MoveSendRequest.Rows()
                                      {
                                          OID = r.OID,
                                          ScanedValue = r.ScanedValue,
                                          StoreValue = r.ValueOnStorage,
                                          IsDeleted = r.IsDeleted
                                      }
                                    ).Where(r => r.ScanedValue > 0).ToList();

                                SendMove(new MoveSendRequest(
                                    MoveResponse.RequestData[0][0].OID,
                                    StorageFromOID,
                                    StorageToOID,
                                    ClaimOIDSet,
                                    IsReceive,
                                    IsInvertory,
                                    IsCreate,
                                    Date,
                                    MoveTypeOID,
                                    CreateReasonOID,
                                    Rows
                                    ));
                            });
                    };

                    BtnSaveScan.Click += delegate { 
                        SaveData(new MoveRowSavedData(
                            MoveRowRequest.IsReceive,
                            MoveRowRequest.IsInvertory,
                            MoveRowRequest.MoveID,
                            MoveRowRequest.ClaimOIDSet,
                            MoveRowRequest.StorageFromOID,
                            MoveRowResponse.RequestData[0])); 
                    };


                    List<MoveRowSavedData> SavedData = await LoadData(DataFilePath);
                    MoveRowSavedData moveRowSavedData = new MoveRowSavedData(
                        MoveRowRequest.IsReceive,
                        MoveRowRequest.IsInvertory,
                        MoveRowRequest.MoveID,
                        MoveRowRequest.ClaimOIDSet,
                        MoveRowRequest.StorageFromOID,
                        MoveRowResponse.RequestData[0]);
                    if ((SavedData != null) && (SavedData.Exists(x => x.MoveRowResponse != null && x.MoveRowResponse.Exists(y => y.ScanedValue > 0) ? x.Equals(moveRowSavedData) : false)))
                    {
                        App.Msg.ShowDialog(this, Resource.String.HaveSavedData, Resource.String.Question_DoLoad, () => {

                            MoveRowSavedData Row = SavedData.Last(x => x.MoveRowResponse != null ? x.Equals(moveRowSavedData) : false);
                            foreach (MoveRowResponse.Data SavedRow in Row.MoveRowResponse)
                            {
                                foreach (MoveRowResponse.Data RespRow in MoveRowResponse.RequestData[0])
                                {
                                    if (SavedRow.OID == RespRow.OID)
                                    {
                                        RespRow.ScanedValue = SavedRow.ScanedValue;
                                        RespRow.IsDeleted = SavedRow.IsDeleted;
                                    }
                                }
                            }
                            lvScanAdapter?.NotifyDataSetChanged();
                        });
                    }
                }

            }

            IntentFilter filter = new IntentFilter();
            filter.AddCategory(Intent.CategoryDefault);
            filter.AddAction(Resources.GetString(Resource.String.Intent_ACTION));
            BroadcastReceiver = new ThisBroadcastReceiver();
            RegisterReceiver(BroadcastReceiver, filter);
        }
        private void SendMove(MoveSendRequest e)
        {
            Action<String> actionSend = async (aNotes) => {
                if (IsRequireComment && string.IsNullOrEmpty(aNotes))
                {
                    App.Msg.ShowMessage(this, string.Empty, "Укажите примечание!");
                }
                else
                {
                    e.Notes = aNotes;
                    MoveSendResponse MoveSendResponse = await App.RESTClient.PostResp<MoveSendResponse>(this, e).ConfigureAwait(false);
                    if (MoveSendResponse?.RequestError == null || string.IsNullOrEmpty(MoveSendResponse.RequestError.Message))
                    {
                        SetResult(Result.Ok, new Intent().PutExtra("IsMoveSended", true));
                        Finish();
                    }
                }
            };

            if (IsRequireComment)
            {
                EditTextDialog editTextDialog = new EditTextDialog(this, "Укажите примечание для отправки на согласование", string.Empty, Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextFlagMultiLine);
                editTextDialog
                    .SetNegativeButton(Resources.GetString(Resource.String.DoCancel), (senderAlerts, args) => { })
                    .SetPositiveButton(Resources.GetString(Resource.String.DoOk), (senderAlerts, args) => { actionSend(editTextDialog.ResultString); })
                    .Show();
            }
            else {
                actionSend(string.Empty);
            }

        }
        private async void SaveData(MoveRowSavedData data)
        {
            string FilePath = DataFilePath;
            data.MoveRowResponse.RemoveAll(x => !x.IsDeleted && x.ScanedValue == 0);
            if (data.MoveRowResponse.Count > 0)
            {
                try
                {
                    List<MoveRowSavedData> ListData = await LoadData(FilePath);
                    if (ListData == null)
                        ListData = new List<MoveRowSavedData> { data };
                    else
                    {
                        ListData.RemoveAll(x => x.Equals(data)); // удаляем все предыдущие сохранения
                        ListData.RemoveAll(x => (DateTime.Now - x.WriteDate).TotalDays >= 20); // удаляем все что уже 20 или более дней хранится
                        ListData.Add(data);
                    }

                    if (File.Exists(FilePath))
                        File.Delete(FilePath);

                    await File.WriteAllTextAsync(FilePath, JsonConvert.SerializeObject(ListData));
                    App.Msg.ShowToastShort(this, Resource.String.Saved);
                }
                catch (Exception e)
                {
                    App.Msg.ShowMessage(this, Resource.String.Error, e.Message);
                }
            }
            else
            {
                App.Msg.ShowToastShort(this, Resource.String.NothingToSave);
            }
        }
        async Task<List<MoveRowSavedData>> LoadData(string FilePath)
        {
            try
            {   
                return File.Exists(FilePath)
                    ? JsonConvert.DeserializeObject<List<MoveRowSavedData>>(File.ReadAllText(FilePath),
                                                                            new JsonSerializerSettings
                                                                            {
                                                                                NullValueHandling = NullValueHandling.Ignore,
                                                                                MissingMemberHandling = MissingMemberHandling.Ignore
                                                                            }
                    )
                    : null;
            }
            catch (Exception e)
            {
                App.Msg.ShowToastShort(this, e.Message);
                return null;
            }
        }
        public override void OnBackPressed()
        {
            App.Msg.ShowDialog(this, string.Empty, Resources.GetString(Resource.String.Question_AreYouSureExit), () => { base.OnBackPressed(); }, null);
        }
        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
                SetFullSreenMode();
        }
        protected override void OnDestroy()
        {
            if (BroadcastReceiver != null)
                UnregisterReceiver(BroadcastReceiver);

            base.OnDestroy();
        }
    }

}