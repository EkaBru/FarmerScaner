using System;
using System.Globalization;
using Android.App;
using Android.Content;
using Android.Views.InputMethods;
using Android.Widget;

namespace FarmScaner
{
    class CountDialog : AlertDialog.Builder
    {
        const decimal MinValue = 0;
        decimal MaxValue;
        public decimal Count;
        bool IsDecimal;
        EditText EdtCount;
        bool EdtCountLocked = false;
        string ArticleName;
        public CountDialog(Context context, decimal aCount, bool aIsDecimal, decimal aMaxValue, string aArticleName) : base(context)
        {
            Count = aCount;
            IsDecimal = aIsDecimal;
            MaxValue = aMaxValue;
            ArticleName = aArticleName;
        }

        public override AlertDialog Show()
        {
            SetView(Resource.Layout.CountDialogLayout);
            AlertDialog dialog = base.Show();
            dialog.SetCancelable(false); // Что бы не закрывалось случайно по тапу мимо окна
            EdtCount = dialog.FindViewById<EditText>(Resource.Id.CountDialog_EdtCount);
            dialog.FindViewById<TextView>(Resource.Id.CountDialog_lblArticleName).Text = ArticleName;

            if (!IsDecimal)
                EdtCount.InputType = Android.Text.InputTypes.ClassNumber;

            EdtCount.Text = Count.ToString(CultureInfo.GetCultureInfo("en-US"));
            // Установка курсора в поле ввода, выбор текста и открытие клавиатуры
            EdtCount.RequestFocus();
            EdtCount.SelectAll();
            ((InputMethodManager)Context.GetSystemService(Context.InputMethodService)).ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.None);
             
            EdtCount.AfterTextChanged += delegate
            {
                if (!EdtCountLocked)
                {
                    SetValue(EdtCount.Text);
                }
            };
            
            dialog.FindViewById<ImageButton>(Resource.Id.CountDialog_CountDec).Click += delegate
            {
                SetValue((--Count).ToString(CultureInfo.GetCultureInfo("en-US")), true);
            };

            dialog.FindViewById<ImageButton>(Resource.Id.CountDialog_CountInc).Click += delegate
            {
                SetValue((++Count).ToString(CultureInfo.GetCultureInfo("en-US")), true);
            };

            dialog.DismissEvent += Dialog_DismissEvent;

            return dialog;
        }

        private void Dialog_DismissEvent(object sender, EventArgs e)
        {
            ((InputMethodManager)Context.GetSystemService(Context.InputMethodService)).ToggleSoftInput(ShowFlags.Implicit, HideSoftInputFlags.None);
        }

        void SetValue(string Value, bool IsNeedUpdateText = false)
        {
            decimal decValue;

            if (!decimal.TryParse(Value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out decValue))
            {
                if (Value == string.Empty)
                    decValue = 0;
                else
                    decValue = Count;
            }

            Count = (MaxValue > 0) & (decValue > MaxValue) ? MaxValue : decValue < MinValue ? MinValue : decValue;
            if (EdtCount != null && (Count != decValue | IsNeedUpdateText)) 
            {
                EdtCountLocked = true;
                try
                {
                    int pos = EdtCount.SelectionStart;
                    EdtCount.Text = Count.ToString(CultureInfo.GetCultureInfo("en-US"));
                    EdtCount.SetSelection(Math.Min(pos, EdtCount.Text.Length));
                }
                finally
                {
                    EdtCountLocked = false;
                }

            }
        }

    }
}