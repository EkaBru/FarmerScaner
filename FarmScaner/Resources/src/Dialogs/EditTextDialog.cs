using Android.App;
using Android.Content;
using Android.Widget;


namespace FarmScaner.layoutClasses
{
    class EditTextDialog : AlertDialog.Builder
    {
        public string ResultString;
        private string Title;
        private string Hint;
        private Android.Text.InputTypes InputType;
        public EditTextDialog(Context aContext, string aTitle, string aHint, Android.Text.InputTypes aInputType) : base(aContext) 
        {
            Title = aTitle;
            Hint = aHint;
            InputType = aInputType;
        }

        public override AlertDialog Show()
        {
            SetView(Resource.Layout.EditTextDialogLayout);
            SetTitle(Title);
            AlertDialog dialog = base.Show();
            EditText Edit = dialog.FindViewById<EditText>(Resource.Id.Edit);

            Edit.InputType = InputType;
            Edit.Hint = Hint;
            Edit.AfterTextChanged += delegate
            {
                ResultString = Edit.Text;
            };
            return dialog;
        }
    }
}