using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Widget;
using Newtonsoft.Json;
using Plugin.Settings;
using Plugin.Settings.Abstractions;

namespace FarmScaner.Models
{
    #region Base
    public abstract class BaseResponse
    {
        [JsonProperty("error")]
        public ErrorMessage RequestError { get; set; }
        public class ErrorMessage
        {
            [JsonProperty("message")]
            public string Message { get; set; }
        }
    }

    public abstract class BaseRequest
    {
        public virtual bool IsShowLoading => false;
        public virtual bool IsCanCancelLoading => true;
        protected abstract Dictionary<string, dynamic> Data { get; }
        protected abstract string UrlPath { get; }
        public string Url => (AppSettings.IsUseTestServer ? "https://XXX.ma.ru" : "https://YYY.ma.ru") + UrlPath;
        public string JSONData => JsonConvert.SerializeObject(Data);
    }
    #endregion

    #region autuh
    public class TokenResponse : BaseResponse
    {
        [JsonProperty("data")]
        public Data RequestData { get; set; }
        public class Data
        {
            [JsonProperty("access_token")]
            public string access_token { get; set; }
            [JsonProperty("life_time_minutes")]
            public int life_time_minutes { get; set; }
        }
    }

    public class TokenReqest : BaseRequest
    {
        public override bool IsShowLoading => true;
        protected override string UrlPath => "/Auth/GetToken";
        public string Name { get; set; }
        public string Password { get; set; }

        public TokenReqest(string aName, string aPassword)
        {
            Name = aName;
            Password = aPassword;
        }

        protected override Dictionary<string, dynamic> Data => new Dictionary<string, dynamic>
        {
            { "UserName", Name },
            { "Password", Password }
        };
    }
    #endregion

    #region moving types
    public class MoveTypeRequest : BaseRequest
    {
        protected override string UrlPath => "/MASP/api1/Farm/MoveType/ReadForUser";
        protected override Dictionary<string, dynamic> Data => new Dictionary<string, dynamic> { };
    }

    public class MoveTypeResponse : BaseResponse
    {
        [JsonProperty("data")]
        public List<List<Data>> RequestData { get; set; }
        public class Data
        {
            public override string ToString() => Name;
            [JsonProperty("OID")]
            public long OID { get; set; }
            [JsonProperty("Name")]
            public string Name { get; set; }
            [JsonProperty("IsNeedStorageFrom")]
            public bool IsNeedStorageFrom { get; set; }
            [JsonProperty("IsNeedStorageTo")]
            public bool IsNeedStorageTo { get; set; }
            [JsonProperty("IsNeedCreateReason")]
            public bool IsNeedCreateReason { get; set; }
        }
    }
    #endregion

    #region reasons
    public class CreateReasonRequest : BaseRequest
    {
        protected override string UrlPath => "/MASP/api1/Farm/CreateReason/ReadDict";
        private long MoveTypeOID;
        public CreateReasonRequest(long aMoveTypeOID)
        {
            MoveTypeOID = aMoveTypeOID;
        }
        protected override Dictionary<string, dynamic> Data => new Dictionary<string, dynamic>
        {
            {"MoveTypeOID", MoveTypeOID }
        };
    }

    public class CreateReasonResponse : BaseResponse
    {
        [JsonProperty("data")]
        public List<List<Data>> RequestData { get; set; }
        public class Data
        {
            public override string ToString() => Name;

            [JsonProperty("OID")]
            public long OID { get; set; }
            [JsonProperty("Name")]
            public string Name { get; set; }
        }
    }
    #endregion

    #region storage from
    public class StorageFromRequest : BaseRequest
    {
        protected override string UrlPath => "/MASP/api1/Storage/ReadDictProdByClaimsOnDate";

        private DateTime Date;
        private bool IsReceive;
        private bool IsCreate;
        private bool IsInvertory;
        private long MoveTypeOID;
        public StorageFromRequest(DateTime aDate, bool aIsReceive, bool aIsCreate, bool aIsInvertory, long aMoveTypeOID)
        {
            Date = aDate;
            IsReceive = aIsReceive;
            IsCreate = aIsCreate;
            IsInvertory = aIsInvertory;
            MoveTypeOID = aMoveTypeOID;
        }
        protected override Dictionary<string, dynamic> Data => new Dictionary<string, dynamic>
        {
            {"Date", Date },
            {"IsReceive", IsReceive },
            {"IsCreate", IsCreate },
            {"IsInvertory", IsInvertory },
            {"MoveTypeOID", MoveTypeOID }
        };
    }

    public class StorageFromResponse : BaseResponse
    {
        [JsonProperty("data")]
        public List<List<Data>> RequestData { get; set; }
        public class Data
        {
            public override string ToString() => Name;

            [JsonProperty("OID")]
            public long OID { get; set; }
            [JsonProperty("Name")]
            public string Name { get; set; }
        }
    }
    #endregion

    #region storage to
    public class StorageToRequest : BaseRequest
    {
        protected override string UrlPath => "/MASP/api1/Storage/ReadDictByClaimsOnDate";
        private DateTime Date;
        private bool IsReceive;
        private bool IsCreate;
        private long MoveTypeOID;
        private long StorageFromOID;
        public StorageToRequest(DateTime aDate, long aStorageFromOID, bool aIsReceive, bool aIsCreate, long aMoveTypeOID)
        {
            Date = aDate;
            StorageFromOID = aStorageFromOID;
            IsReceive = aIsReceive;
            IsCreate = aIsCreate;
            MoveTypeOID = aMoveTypeOID;
        }
        protected override Dictionary<string, dynamic> Data => new Dictionary<string, dynamic>
        {
            { "Date", Date },
            { "StorageFromOID", StorageFromOID },
            { "IsReceive", IsReceive },
            { "IsCreate", IsCreate },
            { "MoveTypeOID", MoveTypeOID }
        };
    }
    public class StorageToResponse : BaseResponse
    {
        [JsonProperty("data")]
        public List<List<Data>> RequestData { get; set; }
        public class Data
        {
            public override string ToString() => Name;

            [JsonProperty("OID")]
            public long OID { get; set; }
            [JsonProperty("Name")]
            public string Name { get; set; }
            [JsonProperty("ClaimOIDSet")]
            public string ClaimOIDSet { get; set; }
            [JsonProperty("IsDivideByClaim")]
            public bool IsDivideByClaim { get; set; }
            [JsonProperty("MoveID")]
            public int MoveID { get; set; }
        }
    }
    #endregion

    #region move
    public class MoveRequest : BaseRequest
    {
        protected override string UrlPath => "/MASP/api1/Farm/ReadMove";
        public override bool IsShowLoading => true;
        private bool IsReceive;
        private bool IsInvertory;
        private bool IsCreate;
        private long MoveTypeOID;
        private int MoveID;
        private string ClaimOIDSet;
        private long StorageFromOID;
        private long StorageToOID;
        public MoveRequest(bool aIsRecieve, bool aIsInvertory, bool aIsCreate, int aMoveID, string aClaimOIDSet, long aStorageFromOID, long aStorageToOID, long aMoveTypeOID)
        {
            IsReceive = aIsRecieve;
            IsInvertory = aIsInvertory;
            IsCreate = aIsCreate;
            MoveID = aMoveID;
            ClaimOIDSet = aClaimOIDSet;
            StorageFromOID = aStorageFromOID;
            StorageToOID = aStorageToOID;
            MoveTypeOID = aMoveTypeOID;
        }
        protected override Dictionary<string, dynamic> Data => new Dictionary<string, dynamic>
        {
            { "IsReceive", IsReceive },
            { "IsInvertory", IsInvertory },
            { "IsCreate", IsCreate },
            { "ID", MoveID },
            { "ClaimOIDSet", ClaimOIDSet },
            { "StorageFromOID", StorageFromOID },
            { "StorageToOID", StorageToOID },
            { "MoveTypeOID", MoveTypeOID }
        };
    }
    public class MoveResponse : BaseResponse
    {
        [JsonProperty("data")]
        public List<List<Data>> RequestData { get; set; }
        public class Data
        {
            [JsonProperty("OID")]
            public long OID { get; set; }

            [JsonProperty("TypeOID_Name")]
            public string TypeOIDName { get; set; }

            [JsonProperty("StorageFromOID_Name")]
            public string StorageFromOIDName { get; set; }

            [JsonProperty("StorageFromOID")]
            public string StorageFromOID { get; set; }

            [JsonProperty("StorageToOID_Name")]
            public string StorageToOIDName { get; set; }

            [JsonProperty("StorageToOID")]
            public string StorageToOID { get; set; }
            [JsonProperty("IsCanFindElse")]
            public bool IsCanFindElse { get; set; }
            [JsonProperty("IsRequireComment")]
            public bool IsRequireComment { get; set; }
        }
    }
    #endregion

    #region move rows
    public class MoveRowRequest : BaseRequest
    {
        public override bool IsShowLoading => true;
        public bool IsReceive;
        public bool IsInvertory;
        public int MoveID;
        public string ClaimOIDSet;
        public long StorageFromOID;
        protected override string UrlPath => "/MASP/api1/Farm/ReadGoodsByMove";
        protected override Dictionary<string, dynamic> Data => new Dictionary<string, dynamic>
        {
            { "IsReceive", IsReceive },
            { "ID", MoveID },
            { "ClaimOIDSet", ClaimOIDSet },
            { "StorageFromOID", StorageFromOID },
            { "IsInvertory", IsInvertory }
        };
        public MoveRowRequest(bool aIsRecieve, bool aIsInvertory, int aMoveID, string aClaimOIDSet, long aStorageFromOID)
        {
            IsReceive = aIsRecieve;
            MoveID = aMoveID;
            ClaimOIDSet = aClaimOIDSet;
            StorageFromOID = aStorageFromOID;
            IsInvertory = aIsInvertory;
        }
    }
    public class MoveRowResponse : BaseResponse
    {
        [JsonProperty("data")]
        public List<List<Data>> RequestData { get; set; }
        public class Data : IComparable<Data>, ICloneable
        {
            [JsonProperty("OID")]
            public long OID { get; set; }
            [JsonProperty("GoodsOID_Name")]
            public string GoodsName { get; set; }
            [JsonProperty("Value")]
            public decimal Value { get; set; }
            [JsonProperty("ValueOnStorage")]
            public decimal ValueOnStorage { get; set; }
            [JsonProperty("ValueOnStorageCode")]
            public string ValueOnStorageCode { get; set; }
            [JsonProperty("ScanedValue")]
            public decimal ScanedValue { get; set; }
            [JsonProperty("IsByWeight")]
            public bool IsByWeight { get; set; }
            [JsonProperty("ValueWMeasureCode")]
            public string ValueMeasureCode { get; set; }
            [JsonProperty("ClaimIDSet")]
            public string ClaimIDSet { get; set; }
            [JsonProperty("BarCodeSetList")]
            public string BarCodeSetList { get; set; }
            public List<string> BarCodeSet { get => string.IsNullOrEmpty(BarCodeSetList) ? null: BarCodeSetList.Split(',').ToList(); }

            private int SortOrder {
                get
                {
                    if (ScanedValue > 0 && IsByWeight) // Весовой просакнированный товар
                        return 0;
                    if (ScanedValue == Value && Value != 0) // Заказали = просканировали
                        return 1;
                    else if (ScanedValue != 0) // Что то просканировали
                        return 2;
                    else if (IsDeleted) // Удалено
                        return 4;
                    else
                        return 3;
                }
            }
            public bool IsDeleted { get; set; }
            public int CompareTo(Data other)
            {
                if (other == null)
                    throw new ArgumentNullException(nameof(other));

                switch (AppSettings.SortMode)
                {       
                    case SortOption.Alphabet:
                        return GoodsName.CompareTo(other.GoodsName);
                    case SortOption.Weight:
                        return other.Value.CompareTo(Value);
                    default: // SortOption.Default
                        return SortOrder.CompareTo(other.SortOrder);
                }
            }

            public object Clone()
            {
                Data d = new Data();
                d.ClaimIDSet = ClaimIDSet;
                d.GoodsName = GoodsName;
                d.IsByWeight = IsByWeight;
                d.IsDeleted = IsDeleted;
                d.OID = OID;
                d.ScanedValue = ScanedValue;
                d.Value = Value;
                d.ValueMeasureCode = ValueMeasureCode;
                return d;
            }
        }
    }
    public class MoveRowSavedData
    {
        [JsonProperty("WriteDate")]
        public DateTime WriteDate { get; set; }
        [JsonProperty("MoveRowResponse")]
        public List<MoveRowResponse.Data> MoveRowResponse { get; set; }
        [JsonProperty("IsReceive")]
        bool IsReceive { get; set; }
        [JsonProperty("IsInvertory")]
        bool IsInvertory { get; set; }
        [JsonProperty("MoveID")]
        int MoveID { get; set; }
        [JsonProperty("ClaimOIDSet")]
        string ClaimOIDSet { get; set; }
        [JsonProperty("StorageFromOID")]
        long StorageFromOID { get; set; }

        public MoveRowSavedData(bool IsReceive, bool IsInvertory, int MoveID, string ClaimOIDSet, long StorageFromOID, List<MoveRowResponse.Data> MoveRowResponse)
        {
            this.IsReceive = IsReceive;
            this.IsInvertory = IsInvertory;
            this.MoveID = MoveID;
            this.ClaimOIDSet = ClaimOIDSet;
            this.StorageFromOID = StorageFromOID;
            this.MoveRowResponse = MoveRowResponse.Select(x => (MoveRowResponse.Data) x.Clone()).ToList();
            WriteDate = DateTime.Now;
        }
        public override bool Equals(object obj)
        {
            return obj is MoveRowSavedData
                && IsReceive == ((MoveRowSavedData)obj).IsReceive
                && IsInvertory == ((MoveRowSavedData)obj).IsInvertory
                && MoveID == ((MoveRowSavedData)obj).MoveID
                && ClaimOIDSet == ((MoveRowSavedData)obj).ClaimOIDSet
                && StorageFromOID == ((MoveRowSavedData)obj).StorageFromOID;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
    #endregion

    #region send move

    public class MoveSendRequest : BaseRequest
    {
        protected override string UrlPath => "/MASP/api1/Move/CreateFromClaim";
        public override bool IsShowLoading => true;
        public override bool IsCanCancelLoading => false;
        private DateTime Date;
        private long StorageFromOID;
        private long StorageToOID;
        private string ClaimOIDSet;
        private bool IsReceive;
        private bool IsInvertory;
        private bool IsCreate;
        private long MoveOID;
        private long MoveTypeOID;
        private long CreateReasonOID;
        public string Notes;
        private List<Rows> MoveRows;
        public class Rows
        {
            public long OID { get; set; }
            public decimal ScanedValue { get; set; }
            public bool IsDeleted { get; set; }
            public decimal StoreValue { get; set; }
        }
        public MoveSendRequest(long aMoveOID, long aStorageFromOID, long aStorageToOID, string aClaimOIDSet, bool aIsReceive, bool aIsInvertory, bool aIsCreate, DateTime aDate, long aMoveTypeOID, long aCreateReasonOID, List<Rows> aMoveRows, string aNotes = "")
        {
            Date = aDate;
            StorageFromOID = aStorageFromOID;
            StorageToOID = aStorageToOID;
            ClaimOIDSet = aClaimOIDSet;
            IsReceive = aIsReceive;
            MoveOID = aMoveOID;
            MoveRows = aMoveRows;
            IsInvertory = aIsInvertory;
            IsCreate = aIsCreate;
            MoveTypeOID = aMoveTypeOID;
            CreateReasonOID = aCreateReasonOID;
            Notes = aNotes;
        }
        protected override Dictionary<string, dynamic> Data => new Dictionary<string, dynamic>
        {
            { "OID", MoveOID },
            { "StorageFromOID", StorageFromOID },
            { "StorageToOID", StorageToOID },
            { "ClaimOIDSet", ClaimOIDSet },
            { "IsReceive", IsReceive },
            { "IsInvertory", IsInvertory},
            { "IsCreate", IsCreate },
            { "Date", Date },
            { "MoveTypeOID", MoveTypeOID },
            { "Article", MoveRows },
            { "Notes", Notes },
            { "CreateReasonOID", CreateReasonOID }
        };
    }
    public class MoveSendResponse : BaseResponse {}

    #endregion

    #region find article by barcode
    public class FindArticleRequest : BaseRequest
    {
        protected override string UrlPath => "/MASP/api1/Farm/Article/ReadByBarCode";
        public override bool IsShowLoading => true;
        public string BarCode;
        public long StorageFromOID;
        public long StorageToOID;
        public string ClaimOIDSet;
        protected override Dictionary<string, dynamic> Data => new Dictionary<string, dynamic>
        {
            { "StorageFromOID", StorageFromOID },
            { "StorageToOID", StorageToOID },
            { "BarCode", BarCode },
            { "ClaimOIDSet", ClaimOIDSet }
        }; 
        public FindArticleRequest(string aBarCode, long aStorageFromOID, long aStorageToOID, string aClaimOIDSet)
        {
            BarCode = aBarCode;
            StorageFromOID = aStorageFromOID;
            StorageToOID = aStorageToOID;
            ClaimOIDSet = aClaimOIDSet;
        }
    }

    public class FindArticleResponse : BaseResponse
    {
        [JsonProperty("data")]
        public List<List<Data>> RequestData { get; set; }
        public class Data
        {
            [JsonProperty("OID")]
            public long OID { get; set; }
            [JsonProperty("GoodsOID_Name")]
            public string GoodsName { get; set; }
            [JsonProperty("BarCode")]
            public string BarCode { get; set; }
            [JsonProperty("BarCodeFS")]
            public string BarCodeFS { get; set; }
            [JsonProperty("Value")]
            public decimal Value { get; set; }
            [JsonProperty("ValueOnStorage")]
            public decimal ValueOnStorage { get; set; }
            [JsonProperty("ValueOnStorageCode")]
            public string ValueOnStorageCode { get; set; }
            [JsonProperty("IsByWeight")]
            public bool IsByWeight { get; set; }
            [JsonProperty("ValueWMeasureCode")]
            public string ValueMeasureCode { get; set; }
            [JsonProperty("Question")]
            public string Question { get; set; }
            [JsonProperty("BarCodeSetList")]
            public string BarCodeSetList { get; set; }
        }
        public MoveRowResponse.Data ToListMoveRowResponseData ()
        {
            MoveRowResponse.Data RData = null;

            if (RequestData != null && RequestData[0] != null)
            {
                RData = new MoveRowResponse.Data
                {
                    BarCodeSetList = RequestData[0][0].BarCodeSetList,
                    GoodsName = RequestData[0][0].GoodsName,
                    IsByWeight = RequestData[0][0].IsByWeight,
                    OID = RequestData[0][0].OID,
                    Value = RequestData[0][0].Value,
                    ValueMeasureCode = RequestData[0][0].ValueMeasureCode,
                    ValueOnStorage = RequestData[0][0].ValueOnStorage,
                    ValueOnStorageCode = RequestData[0][0].ValueOnStorageCode,
                    IsDeleted = false,
                    ScanedValue = 0
                };
            }

            return RData;
        }
    }
    #endregion

    #region check updates
    public class CheckUpatesRequest : BaseRequest
    {
        protected override string UrlPath => "/MASP/api1/MultiDevice/CheckUpdates";
        public override bool IsShowLoading => true;
        private string CurrentVersion;
        private string Manufacturer;
        private string Model;

        public CheckUpatesRequest(string aCurrentVersion, string aManufacturer, string aModel)
        {
            Model = aModel;
            Manufacturer = aManufacturer;
            CurrentVersion = aCurrentVersion;
        }
        protected override Dictionary<string, dynamic> Data => new Dictionary<string, dynamic>
        {
            { "Version", CurrentVersion },
            { "Manufacturer", Manufacturer },
            { "Model", Model }
        };
    }

    public class CheckUpatesResponse : BaseResponse
    {
        [JsonProperty("data")]
        public List<List<Data>> RequestData { get; set; }
        public class Data
        {
            [JsonProperty("IsNeedUpdate")]
            public bool IsNeedUpdate { get; set; }
            [JsonProperty("DownloadLnk")]
            public string DownloadLnk { get; set; }
            [JsonProperty("DownloadFileName")]
            public string DownloadFileName { get; set; }
            [JsonProperty("DatawedgeLnk")]
            public string DatawedgeLnk { get; set; }
            [JsonProperty("DatawedgeFileName")]
            public string DatawedgeFileName { get; set; }
        }
    }
    #endregion

    public class Msg
    {
        public bool ShowMessage(Context Context, int Title, string Message) => ShowMessage(Context, Context.Resources.GetString(Title), Message);
        public bool ShowMessage(Context Context, int Title, int Message) => ShowMessage(Context, Context.Resources.GetString(Title), Context.Resources.GetString(Message));
        public bool ShowMessage(Context Context, string Title, string Message)
        {
            bool res = false;
            new AlertDialog.Builder(Context)
                .SetTitle(Title)
                .SetMessage(Message)
                .SetPositiveButton("OK", (senderAlerts, args) => { res = true; })
                .Show();
            return res;

        }
        public void ShowDialog(Context Context, int Title, int Message, Action Positive) => ShowDialog(Context, Context.GetString(Title), Context.GetString(Message), Positive);
        public void ShowDialog(Context Context, string Title, string Message, Action Positive) => ShowDialog(Context, Title, Message, Positive, null);
        public void ShowDialog(Context Context, string Title, string Message, Action PositiveAction, Action NegativeAction, bool IsCanCancel = true, string PositiveText = "Да", string NegativeText = "Отмена")
        {
            if (Context is null) throw new ArgumentNullException(nameof(Context));
            new AlertDialog.Builder(Context)
                .SetTitle(Title)
                .SetMessage(Message)
                .SetPositiveButton(PositiveText, (senderAlerts, args) => { PositiveAction?.Invoke(); })
                .SetNegativeButton(NegativeText, (senderAlerts, args) => { NegativeAction?.Invoke(); })
                .SetCancelable(IsCanCancel)
                .Show();
        }
        public void ShowToastShort(Context Context, int MessageSource)
        {
            if (Context is null) throw new ArgumentNullException(nameof(Context));
            ShowToastShort(Context, Context.Resources.GetString(MessageSource));
        }
        public void ShowToastShort(Context Context, string Message)
        {
            if (Context is null) throw new ArgumentNullException(nameof(Context));
            Toast.MakeText(Context, Message, ToastLength.Short).Show();
        }
    }
    public enum SortOption { Default, Alphabet, Weight };
    // SortOption navigation
    public static class Extensions
    {
        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf<T>(Arr, src) + 1;
            return (Arr.Length == j) ? Arr[0] : Arr[j];
        }
    } 
    static class AppSettings
    {
        static ISettings Settings
        {
            get
            {
                return CrossSettings.Current;
            }
        }

        public static void ClearUser()
        {
            CrossSettings.Current.Remove(nameof(Lgn));
            CrossSettings.Current.Remove(nameof(Pwd));
            CrossSettings.Current.Remove(nameof(Token));
            CrossSettings.Current.Remove(nameof(TokenEndTime));
        }
        public static string Lgn
        {
            get => Settings.GetValueOrDefault(nameof(Lgn), string.Empty);
            set => Settings.AddOrUpdateValue(nameof(Lgn), value);
        }
        public static string Pwd
        {
            get => Settings.GetValueOrDefault(nameof(Pwd), string.Empty);
            set => Settings.AddOrUpdateValue(nameof(Pwd), value);
        }
        public static string Token
        {
            get => Settings.GetValueOrDefault(nameof(Token), string.Empty);
            set => Settings.AddOrUpdateValue(nameof(Token), value);
        }
        public static DateTime TokenEndTime
        {
            get => Settings.GetValueOrDefault(nameof(TokenEndTime), DateTime.Now);
            set => Settings.AddOrUpdateValue(nameof(TokenEndTime), value);
        }
        public static bool IsUseTestServer
        {
            get => Settings.GetValueOrDefault(nameof(IsUseTestServer), false);
            set 
            {
                Settings.AddOrUpdateValue(nameof(IsUseTestServer), value);
                ClearUser();
            }
        }
        public static SortOption SortMode
        {
            get => (SortOption) Settings.GetValueOrDefault(nameof(SortMode), (decimal) SortOption.Default);
            set => Settings.AddOrUpdateValue(nameof(SortMode), (decimal) value);

        }
        public static bool DataWedgeIsLoded // настройка для сканера загружена?
        {
            get => Settings.GetValueOrDefault(nameof(DataWedgeIsLoded), false);
            set => Settings.AddOrUpdateValue(nameof(DataWedgeIsLoded), value);
        }
    }
}   