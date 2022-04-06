using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using FarmScaner.Models;
using Newtonsoft.Json;
using FarmScaner.layoutClasses;

namespace FarmScaner.Source
{
    public class RESTClient
    {
        readonly HttpClient Client;

        public RESTClient()
        {
            Client = new HttpClient();
            Client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<Boolean> Login(Context Context, TokenReqest Data)
        {
            if ((AppSettings.Lgn != Data.Name) | (AppSettings.Pwd != Data.Password))
                AppSettings.ClearUser();

            if ((!string.IsNullOrEmpty(AppSettings.Token)) && (AppSettings.TokenEndTime > DateTime.Now))
            {
                return await App.UpdateManager.CheckAndDownloadUpdate(Context);
            }
            else
            {
                var resp = await PostResp<TokenResponse>(Context, Data);

                if ((resp != null) && (resp.RequestData != null) && (!string.IsNullOrEmpty(resp.RequestData.access_token)))
                {
                    AppSettings.Lgn = Data.Name;
                    AppSettings.Pwd = Data.Password;
                    AppSettings.Token = resp.RequestData.access_token;
                    AppSettings.TokenEndTime = DateTime.Now.AddMinutes(resp.RequestData.life_time_minutes);
                    return await App.UpdateManager.CheckAndDownloadUpdate(Context);
                }
                else
                    return false;
            }
        }

        public async Task<T> PostResp<T>(Context Context, BaseRequest Data) where T : BaseResponse
        {
            T ResponseObj = null;
            bool IsNotRaise = false;
            if (typeof(T) != typeof(TokenResponse))
            {
                //TODO: Что то не работает. Приложениее виснит, видимо в цикл уходит почему то, а в отладке вылетает
                //if (!CheckAndTryRestoreConnection(Context))
                //    App.Msg.ShowMessage(Context, "Ошибка!", "Не удалось авторизироваться");

                Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppSettings.Token);
            }

            try
            {
                LoadingFrame loading = new LoadingFrame(Context, Data.IsCanCancelLoading);
                if (Data.IsShowLoading)
                {
                    loading.Show();
                    loading.OnCancel += delegate { Client.CancelPendingRequests(); IsNotRaise = true; };
                }
                try
                {
                    var Response = await Client.PostAsync(Data.Url, new StringContent(Data.JSONData, Encoding.UTF8, "application/json"));
                    var JSONText = Response.Content.ReadAsStringAsync().Result;
                    ResponseObj = JsonConvert.DeserializeObject<T>(
                        JSONText,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            MissingMemberHandling = MissingMemberHandling.Ignore
                        });

                    if (Response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        AppSettings.Token = string.Empty;
                        AppSettings.TokenEndTime = DateTime.MinValue;
                    }

                    if (Response.StatusCode != HttpStatusCode.OK)
                        App.Msg.ShowMessage(Context, "Ошибка!", "Запрос вернул статус " + Response.StatusCode);
                    else
                    {
                        if ((ResponseObj.RequestError != null) && (!string.IsNullOrEmpty(ResponseObj.RequestError.Message)))
                            App.Msg.ShowMessage(Context, "Ошибка!", ResponseObj.RequestError.Message);
                    }
                }
                finally
                {
                    loading.Dismiss();
                }
            }
            catch (Exception e)
            {
                if (!IsNotRaise)
                    App.Msg.ShowMessage(Context, "Критическая ошибка!", e.Message);
                IsNotRaise = true;
            }
            return ResponseObj;
        }
        
        private bool CheckAndTryRestoreConnection(Context context)
        {
            if (string.IsNullOrEmpty(AppSettings.Token))
            {
                return !string.IsNullOrEmpty(AppSettings.Lgn) 
                    && !string.IsNullOrEmpty(AppSettings.Pwd)
                    && Login(context, new TokenReqest(AppSettings.Lgn, AppSettings.Pwd)).Result;
            } else
            {
                return true;
            }
        }
    
    }

}