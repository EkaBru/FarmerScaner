using System;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace FarmScaner.layoutClasses
{
    public class LoadingFrame : AlertDialog.Builder
    {
        public Action OnCancel { get; set; }
        private AlertDialog Dialog { get; set; }
        private bool IsCanCancel = true;

        public LoadingFrame(Context aContext, bool aIsCanCancel) : base(aContext)
        {
            SetView(Resource.Layout.LoadingLayout);
            SetCancelable(false);
            IsCanCancel = aIsCanCancel;
        }
        public override AlertDialog Show()
        {
            Dialog = base.Show();
            OnCancel = delegate { Dialog.Dismiss(); };
            Dialog.Window.SetBackgroundDrawableResource(Resource.Color.colorTransparent);
            Button btnCancel;

            btnCancel = Dialog.FindViewById<Button>(Resource.Id.LoadingLayout_Cancel);
            btnCancel.Visibility = IsCanCancel ? ViewStates.Visible : ViewStates.Gone;
            btnCancel.Click += delegate { if (IsCanCancel) OnCancel(); };

            return Dialog;
        }

        public void Dismiss()
        {
            Dialog?.Dismiss();
        }

    }
}