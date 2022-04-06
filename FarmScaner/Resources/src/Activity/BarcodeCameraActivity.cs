using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Vision;
using Android.Gms.Vision.Barcodes;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using FarmScaner.Source;
using System;
using System.Threading.Tasks;
using static Android.Gms.Vision.Detector;

namespace FarmScaner
{
    [Activity(
    ConfigurationChanges = ConfigChanges.Orientation,
    ScreenOrientation = ScreenOrientation.Portrait,
    Theme = "@style/AppTheme")]
    class BarcodeCameraActivity : AppCompatActivity, ISurfaceHolderCallback, IProcessor
    {
        private CameraSource cameraSource;
        private BarcodeDetector barcodeDetector;
        private SurfaceView cameraPreview;
        private const int REQUEST_CAMERAID = 1001;
        private bool IsCanDetect = true;

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            switch (requestCode)
            {
                case REQUEST_CAMERAID:
                    {
                        CreateSurface(cameraPreview.Holder);
                    }
                break;

            }
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            //throw new System.NotImplementedException();
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            CreateSurface(holder);
        }

        private void CreateSurface(ISurfaceHolder holder)
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.Camera }, REQUEST_CAMERAID);
                return;
            }
            else
            {
                if (cameraSource != null)
                    try
                    {
                        cameraSource.Start(holder);
                    }
                    catch (Exception e)
                    {
                        App.Msg.ShowMessage(this, "Error on create surface", e.Message);
                    }
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            if (cameraSource != null)
                cameraSource.Stop();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.BarCodeCameraLayout);

            barcodeDetector = new BarcodeDetector.Builder(this)
                .SetBarcodeFormats(BarcodeFormat.Code128 | BarcodeFormat.QrCode | BarcodeFormat.Ean13)
                .Build();
            cameraSource = new CameraSource
                .Builder(this, barcodeDetector)
                .SetRequestedPreviewSize(1920, 1080)
                .SetAutoFocusEnabled(true)
                .Build();
            cameraPreview = FindViewById<SurfaceView>(Resource.Id.BarCodeCameraLayout_Preview);

            cameraPreview.Holder.AddCallback(this);
            barcodeDetector.SetProcessor(this);
        }

        public void ReceiveDetections(Detections detections)
        {
            if (IsCanDetect)
            {
                SparseArray Codes = detections.DetectedItems;
                if (Codes.Size() > 0)
                {
                    IsCanDetect = false;
                    Vibrator v = (Vibrator)GetSystemService(Context.VibratorService);
                    v.Vibrate(300);

                    Intent intent = new Intent(Resources.GetString(Resource.String.Intent_ACTION))
                        .PutExtra(Resources.GetString(Resource.String.Intent_BaseExtraKey), ((Barcode)Codes.ValueAt(0)).RawValue);
                    SendBroadcast(intent);
                    Finish();
                }
            }
        }

        public void Release()
        {
            //throw new NotImplementedException();
        }

        public override void OnBackPressed()
        {
            if (cameraSource != null)
                cameraSource.Stop();
            base.OnBackPressed();
        }
    }
}