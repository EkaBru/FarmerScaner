using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using FarmScaner.layoutClasses;
using FarmScaner.Models;
using FarmScaner.Source;
using System;
using System.Collections.Generic;
using System.Globalization;
using static Android.App.DatePickerDialog;
using static Android.App.TimePickerDialog;

namespace FarmScaner
{
    [Activity(
        ConfigurationChanges = ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Portrait,
        Theme = "@style/AppTheme")]
    class WizardActivity : AppCompatActivity
    {
        const int RQ_SCANACTIVITY = 999;
        static bool IsCheckLocked = false;

        #region Контролы
        private Spinner edtStorageFrom => FindViewById<Spinner>(Resource.Id.edtStorageFrom);
        private Spinner edtStorageTo => FindViewById<Spinner>(Resource.Id.edtStorageTo);
        private RadioButton rbIsCreate => FindViewById<RadioButton>(Resource.Id.rbIsCreate);
        private RadioButton rbIsInv => FindViewById<RadioButton>(Resource.Id.rbIsInv);
        private RadioButton rbIsRc => FindViewById<RadioButton>(Resource.Id.rbIsRc);
        private RadioButton rbIsSnd => FindViewById<RadioButton>(Resource.Id.rbIsSnd);
        private Button btnOk => FindViewById<Button>(Resource.Id.btnOk);
        private TextView edtDate => FindViewById<TextView>(Resource.Id.edtDate);
        private ProgressBar Wizard_ProgressBar => FindViewById<ProgressBar>(Resource.Id.Wizard_ProgressBar);
        private TextView lblStorageTo => FindViewById<TextView>(Resource.Id.lblStorageTo);
        private TextView lblStorageFrom => FindViewById<TextView>(Resource.Id.lblStorageFrom);
        private Spinner edtMoveType => FindViewById<Spinner>(Resource.Id.edtMoveType);
        private TextView lblMoveType => FindViewById<TextView>(Resource.Id.lblMoveType);
        #endregion
        #region Параметры
        private DateTime edtDateValue
        {
            get => DateTime.TryParse(edtDate.Text, out DateTime Date) ? Date : DateTime.Now;
            set => edtDate.Text = rbIsInv.Checked ? value.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("en-US")) : value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("en-US"));            
        }
        private StorageFromResponse.Data PStorageFrom
        {
            get => edtStorageFrom.SelectedItemPosition >= 0 ? ((ArrayAdapter<StorageFromResponse.Data>)edtStorageFrom.Adapter).GetItem(edtStorageFrom.SelectedItemPosition) : new StorageFromResponse.Data();
        }
        private StorageToResponse.Data PStorageTo
        {
            get => edtStorageTo.SelectedItemPosition >= 0 ? ((ArrayAdapter<StorageToResponse.Data>)edtStorageTo.Adapter).GetItem(edtStorageTo.SelectedItemPosition) : new StorageToResponse.Data();
        }
        private MoveTypeResponse.Data PMoveType
        {
            get => edtMoveType.SelectedItemPosition >= 0 ? ((ArrayAdapter<MoveTypeResponse.Data>)edtMoveType.Adapter).GetItem(edtMoveType.SelectedItemPosition) : new MoveTypeResponse.Data();
        }
        #endregion

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.WizardLayout);

            FindViewById<Toolbar>(Resource.Id.toolbar).NavigationOnClick += delegate { OnBackPressed(); };

            CheckedChange(rbIsCreate);
            LoadData();
        }
        private void LoadData()
        {
            edtDate.Click += delegate
            {
                new DatePickerDialog(
                    this,
                    delegate (object sender, DateSetEventArgs e)
                    {
                        DateTime dt = e.Date;
                        Action SetAndChange = () =>
                        {
                            edtDateValue = dt;
                            EdtDateChange();
                        };

                        if (rbIsInv.Checked)
                        {
                            new TimePickerDialog(
                                this,
                                delegate (object sender, TimeSetEventArgs e)
                                {
                                    dt = dt.AddHours(e.HourOfDay).AddMinutes(e.Minute);
                                    SetAndChange();
                                },
                                (edtDateValue.Hour + edtDateValue.Minute == 0) ? DateTime.Now.Hour : edtDateValue.Hour,
                                (edtDateValue.Hour + edtDateValue.Minute == 0) ? DateTime.Now.Minute : edtDateValue.Minute,
                                true
                            ).Show();
                        }
                        else
                            SetAndChange();

                    },
                    edtDateValue.Year,
                    edtDateValue.Month - 1,
                    edtDateValue.Day
                    ).Show();
            };
            edtStorageFrom.ItemSelected += delegate { EdtStorageFromChange(); };
            edtStorageTo.ItemSelected += delegate { SetBtnOkReadOnly(); };
            edtMoveType.ItemSelected += delegate { EdtDateChange(); };
            rbIsCreate.CheckedChange += WizardActivity_CheckedChange;
            rbIsInv.CheckedChange += WizardActivity_CheckedChange;
            rbIsRc.CheckedChange += WizardActivity_CheckedChange;
            rbIsSnd.CheckedChange += WizardActivity_CheckedChange;

            btnOk.Click += async delegate
            {
                long CreateReasonOID = 0;
                Action StartActivity = () =>
                {
                    StartActivityForResult(
                        new Intent(this, typeof(ScanActivity))
                            .SetFlags(ActivityFlags.ReorderToFront)
                            .PutExtra("IsReceive", rbIsRc.Checked)
                            .PutExtra("IsInvertory", rbIsInv.Checked)
                            .PutExtra("IsCreate", rbIsCreate.Checked)
                            .PutExtra("MoveTypeOID", PMoveType.OID)
                            .PutExtra("MoveID", PStorageTo.MoveID)
                            .PutExtra("ClaimOIDSet", PStorageTo?.ClaimOIDSet)
                            .PutExtra("StorageFromOID", PStorageFrom.OID)
                            .PutExtra("StorageToOID", PStorageTo.OID)
                            .PutExtra("CreateReasonOID", CreateReasonOID)
                            .PutExtra("Date", edtDateValue.ToString()),
                        RQ_SCANACTIVITY);
                };

                if (PMoveType.IsNeedCreateReason)
                {
                    CreateReasonResponse createReasonResponse = await App.RESTClient.PostResp<CreateReasonResponse>(this, new CreateReasonRequest(PMoveType.OID));
                    if (createReasonResponse.RequestData?.Count > 0)
                    {
                        
                        ListViewDialog listViewDialog = new ListViewDialog(
                            this,
                            "Укажите причину создания",
                            new ArrayAdapter<CreateReasonResponse.Data>(this, Resource.Layout.support_simple_spinner_dropdown_item, createReasonResponse.RequestData[0]),
                            (Adapter, Position) => {
                                if (Position >= 0)
                                {
                                    CreateReasonOID = ((ArrayAdapter<CreateReasonResponse.Data>)Adapter).GetItem(Position).OID;
                                }
                                StartActivity();
                            }
                            );
                        listViewDialog
                            .SetNegativeButton(Resources.GetString(Resource.String.DoCancel), (senderAlerts, args) => { })
                            .Show();
                    }
                }
                else
                {
                    StartActivity();
                }
            };

            EdtDateChange();
        }
        private void WizardActivity_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            CheckedChange(sender);
        }
        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if ((data != null) && (requestCode == RQ_SCANACTIVITY) && (resultCode == Result.Ok))
            {
                if (data.GetBooleanExtra("IsMoveSended", false))
                    EdtDateChange();
            }
            base.OnActivityResult(requestCode, resultCode, data);
        }
        private async void EdtDateChange()
        {
            edtStorageFrom.Adapter = null;
            edtStorageTo.Adapter = null;

            SetBtnOkReadOnly();
            try
            {
                StorageFromResponse storageFromResponse = null;

                Wizard_ProgressBar.Visibility = ViewStates.Visible;
                try
                {
                    storageFromResponse = await App.RESTClient.PostResp<StorageFromResponse>(this, new StorageFromRequest(edtDateValue, rbIsRc.Checked, rbIsCreate.Checked, rbIsInv.Checked, PMoveType.OID)).ConfigureAwait(true);
                }
                finally
                {
                    Wizard_ProgressBar.Visibility = ViewStates.Gone;
                }
                if (storageFromResponse?.RequestData?.Count > 0)
                {
                    edtStorageFrom.Adapter = new ArrayAdapter<StorageFromResponse.Data>(this, Resource.Layout.support_simple_spinner_dropdown_item, storageFromResponse.RequestData[0]);
                }
            }
            finally
            {
                SetBtnOkReadOnly();
            }
        }
        private async void EdtStorageFromChange()
        {
            edtStorageTo.Adapter = null;
            SetBtnOkReadOnly();
            if (!rbIsInv.Checked)
            {
                try
                {
                    StorageToResponse storageToResponse = null;
                    Wizard_ProgressBar.Visibility = ViewStates.Visible;
                    try
                    {
                        storageToResponse = await App.RESTClient.PostResp<StorageToResponse>(this, new StorageToRequest(edtDateValue, PStorageFrom.OID, rbIsRc.Checked, rbIsCreate.Checked, PMoveType.OID)).ConfigureAwait(true);
                    }
                    finally
                    {
                        Wizard_ProgressBar.Visibility = ViewStates.Gone;
                    }
                    if (storageToResponse?.RequestData?.Count > 0)
                    {
                        edtStorageTo.Adapter = new ArrayAdapter<StorageToResponse.Data>(this, Resource.Layout.support_simple_spinner_dropdown_item, storageToResponse.RequestData[0]);
                    }
                }
                finally
                {
                    SetBtnOkReadOnly();
                }
            }
        }
        private void SetBtnOkReadOnly()
        {
            edtStorageTo.Enabled = edtStorageTo.Adapter != null;
            edtStorageFrom.Enabled = edtStorageFrom.Adapter != null;
            edtMoveType.Enabled = edtMoveType.Adapter != null;
            edtStorageFrom.Visibility = edtMoveType.Visibility == ViewStates.Gone | PMoveType.IsNeedStorageFrom ? ViewStates.Visible : ViewStates.Gone;
            edtStorageTo.Visibility = (edtMoveType.Visibility == ViewStates.Gone | PMoveType.IsNeedStorageTo) && !rbIsInv.Checked ? ViewStates.Visible : ViewStates.Gone;
            lblStorageTo.Visibility = edtStorageTo.Visibility;
            lblStorageFrom.Visibility = edtStorageFrom.Visibility;

            btnOk.Enabled = (edtStorageFrom.Visibility == ViewStates.Gone | (edtStorageFrom.SelectedItemPosition >= 0)
                         && (edtStorageTo.Visibility == ViewStates.Gone | edtStorageTo.SelectedItemPosition >= 0)
                            );
        }
        private async void CheckedChange(object sender)
        {
            if (!IsCheckLocked)
            {
                edtMoveType.Adapter = null;
                IsCheckLocked = true;

                try
                {
                    List<RadioButton> ls = new List<RadioButton> { { rbIsSnd }, { rbIsRc }, { rbIsInv }, { rbIsCreate } };
                    foreach (RadioButton ch in ls)
                    {
                        ch.Checked = ch == sender;
                    }

                    edtDateValue = rbIsCreate.Checked | rbIsInv.Checked ? DateTime.Now : DateTime.Now.Date.AddDays(1);


                    if (rbIsCreate.Checked)
                    {
                        MoveTypeResponse moveTypeResponse = null;
                        Wizard_ProgressBar.Visibility = ViewStates.Visible;
                        try
                        {
                            moveTypeResponse = await App.RESTClient.PostResp<MoveTypeResponse>(this, new MoveTypeRequest());
                        }
                        finally
                        {
                            Wizard_ProgressBar.Visibility = ViewStates.Gone;
                        }
                        if (moveTypeResponse?.RequestData?.Count > 0)
                        {
                            edtMoveType.Adapter = new ArrayAdapter<MoveTypeResponse.Data>(this, Resource.Layout.support_simple_spinner_dropdown_item, moveTypeResponse.RequestData[0]);
                        }
                    }
                }
                finally
                {
                    IsCheckLocked = false;
                    edtMoveType.Visibility = rbIsCreate.Checked ? ViewStates.Visible : ViewStates.Gone;
                    lblMoveType.Visibility = edtMoveType.Visibility;
                    EdtDateChange();
                }
            }
        }
    }
}