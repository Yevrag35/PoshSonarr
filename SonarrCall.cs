using MG.Api;
using Sonarr.Api.Enums;
using Sonarr.Api.Endpoints;
using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Sonarr.Api
{
    public class SonarrCall : GenericApi, IWebRequestCreate
    {
        #region Properties/Fields

        private protected const string _ct = "application/json";

        private HttpWebRequest _wr;
        private RequestParameters _p;
        private static readonly Array EnumArray = typeof(SonarrMethod).GetEnumValues();

        public SonarrMethod? Method
        {
            get
            {
                for (int i = 0; i < EnumArray.Length; i++)
                {
                    var m = (SonarrMethod)EnumArray.GetValue(i);
                    if (m.ToString() == _wr.Method)
                    {
                        return m;
                    }
                }
                return null;
            }
            set => _wr.Method = value.ToString();
        }

        #endregion

        #region Constructors

        // These 2 constructors assumes that the specified 'Url' is the whole thing
        // (e.g. - includes the endpoint, query, etc.).
        public SonarrCall(ApiUrl url) => _wr = WebRequest.CreateHttp((Uri)url);        
        public SonarrCall(ApiUrl url, SonarrMethod method, ApiKey apiKey, RequestParameters parameters = null)
        {
            _wr = WebRequest.CreateHttp((Uri)url);
            _wr.Method = method.ToString();
            _wr.Headers = apiKey.AsSonarrHeader();
            _wr.ContentType = _ct;
            _p = parameters;
        }

        // This constructor specifies each piece explicitly
        public SonarrCall(string baseUrl, ISonarrEndpoint endpoint, 
            SonarrMethod method, ApiKey apiKey, RequestParameters parameters = null)
        {
            _wr = WebRequest.CreateHttp(baseUrl + endpoint);
            _wr.Method = method.ToString();
            _wr.Headers = apiKey.AsSonarrHeader();
            _wr.ContentType = _ct;
            _p = parameters;
        }

        #endregion

        #region Cast Operators

        public static implicit operator SonarrCall(WebRequest req) => req;
        public static implicit operator WebRequest(SonarrCall nreq) => nreq;
        public static implicit operator SonarrCall(HttpWebRequest hwr) => hwr;
        public static implicit operator HttpWebRequest(SonarrCall nreq) => nreq;

        #endregion

        #region Methods

        #region Interface Methods
        WebRequest IWebRequestCreate.Create(Uri uri) => _wr = WebRequest.CreateHttp(uri);

        #endregion

        #region Public Methods
        public void AddApiKey(ApiKey key) => _wr.Headers = key.AsSonarrHeader();
        public void AddRequestParameters(RequestParameters parameters) => _p = parameters;

        #endregion

        #region 'THE' Method
        public SonarrResult Send()
        {
            if (_p != null)
            {
                byte[] data = GetBodyData(_p.JsonContent);
                _wr.GetRequestStream().Write(data, 0, data.Length);
            }
            HttpWebResponse response = null;
            try
            {
                response = _wr.GetResponse() as HttpWebResponse;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
            
            if (response != null)
            {
                dynamic result = ReadResponse(response);
                response.Close();
                response.Dispose();
                var sr = new SonarrResult(result);
                return sr;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Backend Methods
        //private byte[] GetBodyData(string body)
        //{
        //    var encoder = new UTF8Encoding();
        //    byte[] data = encoder.GetBytes(body);
        //    return data;
        //}

        //private dynamic ReadResponse(WebResponse resp)
        //{
        //    var stream = resp.GetResponseStream();
        //    var reader = new StreamReader(stream);
        //    string answer = reader.ReadToEnd();
        //    var setts = new JsonSerializerSettings()
        //    {
        //        Formatting = Formatting.Indented,
        //        ObjectCreationHandling = ObjectCreationHandling.Auto
        //    };
        //    dynamic result = JsonConvert.DeserializeObject(answer);
        //    return result;
        //}

        #endregion

        #endregion
    }
}
