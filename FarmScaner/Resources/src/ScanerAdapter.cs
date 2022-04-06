
using Android.Content;
using Android.Views;
using Android.Widget;
using FarmScaner.Source;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Android.Views.View;

namespace FarmScaner.Models
{
    class ScanerAdapter : BaseAdapter
    {
        const int WEIGHT_BARCODE_LEN = 5; // длина части ШК для весового товара, которая определяет вес
        const int WEIGHT_BARCODE_FRACTIONAL_LEN = 3; // кол-во знаков после запятой для весового товара

        static Context Context;
        LayoutInflater LayoutInflater;
        static List<MoveRowResponse.Data> Rows;
        bool IsReceive = false;
        static bool IsInvertory = false;
        bool IsCanDelete = true;
        bool IsShowValueOnStorage = true;

        internal class ItemClickListener : Java.Lang.Object, IOnClickListener
        {
            static ScanerAdapter _adapter;
            public ItemClickListener(ScanerAdapter adapter) => _adapter = adapter;
            public void OnClick(View v)
            {
                int Pos = (int)v.Tag;
                if (Rows?.Count > 0)
                {
                    CountDialog dialog = new CountDialog(
                        Context, 
                        Rows[Pos].ScanedValue, 
                        Rows[Pos].IsByWeight, 
                        (IsInvertory ? -1 : Math.Max(Rows[Pos].ValueOnStorage, Rows[Pos].Value)), 
                        Rows[Pos].GoodsName 
                    );
                    dialog.SetPositiveButton(Resource.String.DoOk, (senderAlerts, args) => { Rows[Pos].ScanedValue = dialog.Count; Rows.Sort(); _adapter.NotifyDataSetChanged(); });
                    dialog.SetNeutralButton(Resource.String.CountDialog_delete, (senderAlerts, args) => { Rows[Pos].ScanedValue = 0; Rows.Sort(); _adapter.NotifyDataSetChanged(); });
                    dialog.SetNegativeButton(Resource.String.DoCancel, (senderAlerts, args) => { });
                    dialog.Show();
                }
            }
        }
        internal class ButtonClickListener : Java.Lang.Object, IOnClickListener
        {
            static ScanerAdapter _adapter;
            public ButtonClickListener(ScanerAdapter adapter) => _adapter = adapter;
            public void OnClick(View v)
            {
                int Pos = (int) v.Tag;
                if (!Rows[Pos].IsDeleted)
                {
                    new Android.App.AlertDialog.Builder(Context)
                        .SetTitle("Удалить позицию из заявки №" + Rows[Pos].ClaimIDSet + "?")
                        .SetPositiveButton(Resource.String.DoOk, (senderAlerts, args) => {
                           Rows[Pos].IsDeleted = true;
                           Rows[Pos].ScanedValue = 0;
                           Rows.Sort();
                           _adapter.NotifyDataSetChanged();
                        })
                        .SetNegativeButton(Resource.String.DoCancel, delegate { })
                        .Show();
                } else
                {
                    Rows[Pos].IsDeleted = false;
                    Rows.Sort();
                    _adapter.NotifyDataSetChanged();
                }
            }
        }

        bool CompareBarCode(string LBarCode, string RBarCode, bool IsByWeight)
        {
            return
                LBarCode != null
                ? IsByWeight && (LBarCode.IndexOf("X") > 0) && (RBarCode.Length >= LBarCode.IndexOf("X"))
                    ? RBarCode.Substring(0, LBarCode.IndexOf("X")) == LBarCode.Substring(0, LBarCode.IndexOf("X"))
                    : LBarCode == RBarCode || App.GetEAN13(LBarCode) == RBarCode
                : false;
        }

        public bool InternalFind(string Barcode, out int Position, out bool IsCantAddMore)
        {
            Position = -1;
            IsCantAddMore = false;
            string FindedBarcode = string.Empty;

            for (int i = 0; i < Rows.Count; i++)
            {
                if (Rows[i]?.BarCodeSet?.Count > 0)
                {
                    foreach (string LBarCode in Rows[i].BarCodeSet)
                    {
                        if (CompareBarCode(LBarCode, Barcode, Rows[i].IsByWeight))
                        {
                            Position = i;
                            FindedBarcode = LBarCode;
                            break;
                        }
                    }
                }

                if (Position >= 0)
                    break;
            }

            if (Position >= 0)
            {
                if (Rows[Position].IsByWeight && FindedBarcode.IndexOf("X") > 0)
                    Rows[Position].ScanedValue += Convert.ToDecimal(Barcode.Substring(FindedBarcode.IndexOf("X"), WEIGHT_BARCODE_LEN)) / (int)System.Math.Pow(10, WEIGHT_BARCODE_FRACTIONAL_LEN);
                else
                {
                    if (Rows[Position].IsByWeight | Rows[Position].ScanedValue + 1 <= Rows[Position].ValueOnStorage)
                        Rows[Position].ScanedValue++;
                    else
                        IsCantAddMore = true;
                }


                long RowOID = Rows[Position].OID;
                Rows.Sort();
                Position = Rows.FindIndex(x => x.OID == RowOID);

                NotifyDataSetChanged();
                return true;
            }
            else
            {
                return false;
            }
        }

        //<result, position, message>
        public async Task Find(string Barcode, long StorageFromOID, long StorageToOID, string ClaimOIDSet, Action<bool, int, string> OnEndAction, bool IsCanFindElse = false)
        {
            bool IsFind;
            bool IsCantAddMore;
            int Position;
            string ErrorMessage = string.Empty;
            IsFind = InternalFind(Barcode, out Position, out IsCantAddMore);

            Action SetMessage = () => { ErrorMessage = !IsFind ? "Не удалось найти" : (IsCantAddMore ? "Недостаточно кол-ва на складе" : string.Empty); };

            if (!IsFind && IsCanFindElse)
            {
                FindArticleRequest FindArticleRequest = new FindArticleRequest(Barcode, StorageFromOID, StorageToOID, ClaimOIDSet);
                FindArticleResponse FindArticleResponce = await App.RESTClient.PostResp<FindArticleResponse>(Context, FindArticleRequest);
                var MoveRowResponseData = FindArticleResponce.ToListMoveRowResponseData();
                if (MoveRowResponseData != null)
                {
                    if (!string.IsNullOrEmpty(FindArticleResponce.RequestData[0][0].Question))
                    {
                        IsFind = true;
                        App.Msg.ShowDialog(Context, string.Empty, FindArticleResponce.RequestData[0][0].Question, () =>
                        {
                            Rows.Add(MoveRowResponseData);
                            InternalFind(Barcode, out Position, out IsCantAddMore);
                            // удаялем если была ошибка
                            if (IsCantAddMore)
                            {
                                Rows.Remove(MoveRowResponseData);
                                Position = -1;
                            }
                            SetMessage();
                            OnEndAction(IsFind, Position, ErrorMessage);
                        });
                    }
                    else
                    {
                        Rows.Add(FindArticleResponce.ToListMoveRowResponseData());
                        IsFind = InternalFind(Barcode, out Position, out IsCantAddMore);
                    }
                }
            }

            SetMessage();
            OnEndAction(IsFind, Position, ErrorMessage);
        }

        public TextView GetlvScanlblHeader(int id) => GetView(id, null, null).FindViewById<TextView>(Resource.Id.lvScanlblHeader);
        public ScanerAdapter(Context aContext, List<MoveRowResponse.Data> aRows, bool aIsRecieve, bool aIsInvertory, bool aIsCanDelete, bool aIsShowValueOnStorage)
        {
            Context = aContext;
            IsReceive = aIsRecieve;
            IsInvertory = aIsInvertory;
            Rows = aRows;
            IsCanDelete = aIsCanDelete;
            IsShowValueOnStorage = aIsShowValueOnStorage;
            Rows.Sort();
            LayoutInflater = (LayoutInflater)Context
                .GetSystemService(Context.LayoutInflaterService);
        }
        public override int Count => Rows.Count;
        public override Java.Lang.Object GetItem(int position) => null;
        public override long GetItemId(int position) => position;

        public MoveRowResponse.Data GetRow(int position) => Rows[position];
        public List<MoveRowResponse.Data> GetRows() => Rows;
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view;

            if (convertView == null)
                view = LayoutInflater.Inflate(Resource.Layout.lvScanItemLayout, parent, false);
            else
                view = convertView;

            MoveRowResponse.Data r = GetRow(position);
            CustomizeRow(r, view, position);
            view.SetOnClickListener(new ItemClickListener(this));
            
            return view;
        }

        public void CustomizeRow(MoveRowResponse.Data Row, View view, int Pos)
        {
            TextView Header = view.FindViewById<TextView>(Resource.Id.lvScanlblHeader);
            TextView BodyName = view.FindViewById<TextView>(Resource.Id.lvScanlblBody);
            TextView BodyValue = view.FindViewById<TextView>(Resource.Id.lvScanlblBodyValue);
            TextView ValueOnStorage = view.FindViewById<TextView>(Resource.Id.lvScanlblValueOnStorage);
            Button DeleteButton = view.FindViewById<Button>(Resource.Id.lvScanbtnDelete);
            LinearLayout Item = view.FindViewById<LinearLayout>(Resource.Id.lvScanItem);

            DeleteButton.Visibility = IsCanDelete ? ViewStates.Visible : ViewStates.Gone;
            ValueOnStorage.Visibility = IsShowValueOnStorage | IsInvertory ? ViewStates.Visible : ViewStates.Gone;
            BodyName.Visibility = IsInvertory ? ViewStates.Gone : ViewStates.Visible;

            Header.Tag = Pos;
            BodyValue.Tag = Pos;
            BodyName.Tag = Pos;
            DeleteButton.Tag = Pos;
            view.Tag = Pos;

            Header.Text = Row.GoodsName;
            BodyName.Text = Row.ValueMeasureCode;
            BodyValue.Text = Row.ScanedValue == 0 ? string.Empty : Row.ScanedValue.ToString();
            ValueOnStorage.Text = Row.ValueOnStorage == 0 ? "Нет на складе" : "На складе: " + Row.ValueOnStorageCode;

            if (IsReceive)
                DeleteButton.Visibility = ViewStates.Invisible;
            else
            {
                DeleteButton.SetOnClickListener(new ButtonClickListener(this));
                DeleteButton.SetText(Row.IsDeleted ? Resource.String.lvScan_CancelDelete : Resource.String.lvScan_Delete);
            };

            if (Row.IsDeleted)
                Item.Background.SetTint(view.Context.Resources.GetColor(Resource.Color.colorListViewItemDeleted));
            else if (Row.IsByWeight && (Row.ScanedValue > 0))
                Item.Background.SetTint(view.Context.Resources.GetColor(Resource.Color.colorListViewItemWeight));
            else if (Row.ScanedValue == Row.Value && Row.Value != 0)
                Item.Background.SetTint(view.Context.Resources.GetColor(Resource.Color.colorListViewItemOk));
            else if (Row.ScanedValue > 0)
                Item.Background.SetTint(view.Context.Resources.GetColor(Resource.Color.colorListViewItemFault));
            else
                Item.Background.SetTint(view.Context.Resources.GetColor(Resource.Color.colorListViewItemDefault));
        }
    }
}