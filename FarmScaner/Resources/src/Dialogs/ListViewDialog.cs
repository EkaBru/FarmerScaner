using Android.App;
using Android.Content;
using Android.Widget;
using FarmScaner.Models;
using System;

namespace FarmScaner.layoutClasses
{
    class ListViewDialog : AlertDialog.Builder
    {
        private string Title;
        private Action<ArrayAdapter, int> SelectAction;
        private ArrayAdapter Adapter;
        public ListViewDialog(Context Context, string Title, ArrayAdapter Adapter, Action<ArrayAdapter, int> SelectAction) : base(Context) 
        {
            this.Adapter = Adapter;
            this.Title = Title;
            this.SelectAction = SelectAction;
        }

        public override AlertDialog Show()
        {
            SetView(Resource.Layout.ListViewDialogLayout);
            SetTitle(Title);
            AlertDialog dialog = base.Show();
            ListView Edit = dialog.FindViewById<ListView>(Resource.Id.Edit);

            Edit.Adapter = Adapter;
            Edit.ItemClick += (object sender, AdapterView.ItemClickEventArgs e) =>
            {
                SelectAction((ArrayAdapter) Edit.Adapter, e.Position);
                dialog.Dismiss();
            };
            return dialog;
        }
    }
}