﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace PixivToZip
{
    public class OAuth
    {
        private pixivUser user;
        public pixivUser User { get { return user; } }
        #region base api

        #region login

        public OAuth()
        {            
            Debug.WriteLine("Don't forget to do authAsync first!");
        }
        public OAuth(string username, string password)//it's not good enough
        {
            var task = authAsync(username, password);
            bool result = task.Result;
        }

        /// <summary>
        /// Caution: authAsync will new a user
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public async Task<bool> authAsync(string username, string password, CancellationTokenSource tokensource = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "client_id", ClientId.client_id },
                {"client_secret", ClientId.client_secret },

                {"grant_type" , "password" },
                {"username", username },
                {"password",password }
            };

            return await authAsync(parameters, tokensource);
        }

        private async Task<bool> authAsync(Dictionary<string, object> parameters, CancellationTokenSource tokensource = null)
        {
            var header = new Dictionary<string, object>
            {
                {"Referer","http://www.pixiv.net/" }//header
            };
            var api = "https://oauth.secure.pixiv.net/auth/token";//oauth_url

            Task<HttpResponseMessage> taskpost;


            taskpost = HttpPostAsync(api, header, parameters, tokensource);

            HttpResponseMessage result = null;

            try
            {
                result = await taskpost;
            }
            catch(Exception ex)
            {
                throw ex;
            }

            if (!result.IsSuccessStatusCode)
            {
                Debug.WriteLine(result);
                return false;
            }


            var json = JObject.Parse(result.Content.ReadAsStringAsync().Result);

            user = new pixivUser();
            user.avatar = new string[3];

            var response = json.Value<JObject>("response");

            user.access_token = response["access_token"].ToString();
            user.expires_time = (int)response["expires_in"];
            user.refresh_token = response["refresh_token"].ToString();
            user.id = response["user"]["id"].ToString();
            user.name = response["user"]["name"].ToString();
            user.avatar[0] = response["user"]["profile_image_urls"]["px_16x16"].ToString();//0 small
            user.avatar[1] = response["user"]["profile_image_urls"]["px_50x50"].ToString();//1 middle
            user.avatar[2] = response["user"]["profile_image_urls"]["px_170x170"].ToString();//2 big
            return true;
        }

        /// <summary>
        /// Caution:reAuthAsync will new a user
        /// </summary>
        public async Task<bool> reAuthAsync(CancellationTokenSource tokensource = null) {
            if (user == null) return false;
            var parameters = new Dictionary<string, object>
            {
                { "client_id", ClientId.client_id },
                {"client_secret", ClientId.client_secret },

                {"grant_type" , "refresh_token" },
                {"refresh_token",user.refresh_token }
            };
            return await authAsync(parameters, tokensource);
        }

        #endregion

        public async Task<HttpResponseMessage> HttpPostAsync(string api, Dictionary<string, object> parameters, CancellationTokenSource tokensource = null)
        {
            return await HttpPostAsync(api, null, parameters, tokensource);
        }

        public async Task<HttpResponseMessage> HttpPostAsync(string api, Dictionary<string, object> header, Dictionary<string, object> parameters, CancellationTokenSource tokensource = null)
        {
            Dictionary<string, object> req_header = new Dictionary<string, object>
            {
                {"Referer","http://spapi.pixiv.net/" },//header
                {"User-Agent","PixivIOSApp/5.8.7" }
            };
            
            if (user != null) req_header.Add("Authorization", ("Bearer " + user.access_token));

            if (parameters == null) parameters = new Dictionary<string, object>();
            if (header == null) header = new Dictionary<string, object>();
            //Add header
            foreach (KeyValuePair<string, object> x in header)
            {
                if (req_header.ContainsKey(x.Key)) req_header[x.Key] = x.Value;
                else req_header.Add(x.Key, x.Value);
            }

            HttpClient http= new HttpClient(new HttpClientHandler() { CookieContainer = new CookieContainer(), UseCookies = true });
            http.DefaultRequestHeaders.Clear();
            foreach (KeyValuePair<string, object> x in req_header)
            {
                http.DefaultRequestHeaders.Add(x.Key, (string)x.Value);
            }

            var dict = new Dictionary<string, object>(parameters.ToDictionary(k => k.Key, v => v.Value));

            HttpContent httpContent = null;
            //hash type
            if (dict.Count(p => p.Value.GetType() == typeof(byte[]) || p.Value.GetType() == typeof(System.IO.FileInfo)) > 0)
            {
                var content = new MultipartFormDataContent();

                foreach (var param in dict)
                {
                    var dataType = param.Value.GetType();
                    if (dataType == typeof(byte[]))
                    {
                        content.Add(new ByteArrayContent((byte[])param.Value), param.Key, GetNonceString());
                    }
                    else if (dataType == typeof(System.IO.FileInfo))
                    {
                        var file = (System.IO.FileInfo)param.Value;
                        content.Add(new ByteArrayContent(System.IO.File.ReadAllBytes(file.FullName)), param.Key, file.Name);
                    }
                    else
                    {
                        content.Add(new StringContent(string.Format("{0}", param.Value)), param.Key);
                    }
                }
                httpContent = content;
            }
            else
            {
                var content = new FormUrlEncodedContent(dict.ToDictionary(k => k.Key, v => string.Format("{0}", v.Value)));
                httpContent = content;
            }
            if (tokensource == null)
                return await http.PostAsync(api, httpContent);
            else return await http.PostAsync(api, httpContent, tokensource.Token);
        }
        public async Task<HttpResponseMessage> HttpGetAsync(string api, Dictionary<string, object> parameters, CancellationTokenSource tokensource = null)
        {
            return await HttpGetAsync(api, null, parameters, tokensource);
        }
        public async Task<HttpResponseMessage> HttpGetAsync(string api, Dictionary<string, object> header, Dictionary<string, object> parameters, CancellationTokenSource tokensource = null)
        {
            if (user == null)//exclude null exception
            {
                Debug.WriteLine("Please login first!");
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            }

            Dictionary<string, object> req_header = new Dictionary<string, object>
            {
                {"Referer","http://spapi.pixiv.net/" },//header
                {"UserAgent","PixivIOSApp/5.8.3" },
                {"Authorization",("Bearer "+user.access_token) }
            };
            
            if (header == null) header = new Dictionary<string, object>();
            //Add header
            foreach (KeyValuePair<string, object> x in header)
            {
                if (req_header.ContainsKey(x.Key)) req_header[x.Key] = x.Value;
                else req_header.Add(x.Key, x.Value);
            }

            HttpClient http = new HttpClient(new HttpClientHandler() { CookieContainer = new CookieContainer(), UseCookies = true });
            http.DefaultRequestHeaders.Clear();
            foreach (KeyValuePair<string, object> x in req_header)
            {
                http.DefaultRequestHeaders.Add(x.Key, (string)x.Value);
            }

            if (parameters == null) parameters = new Dictionary<string, object>();

            var queryString = string.Join("&", parameters.Where(p => p.Value == null || p.Value.GetType().IsValueType || p.Value.GetType() == typeof(string)).Select(p => string.Format("{0}={1}", Uri.EscapeDataString(p.Key), Uri.EscapeDataString(string.Format("{0}", p.Value)))));

            if (api.IndexOf("?") < 0)
            {
                api = string.Format("{0}?{1}", api, queryString);
            }
            else
            {
                api = string.Format("{0}&{1}", api, queryString);
            }

            api = api.Trim('&', '?');
            if (tokensource == null) return await http.GetAsync(api);
            return await http.GetAsync(api, tokensource.Token);
        }
        public async Task<HttpResponseMessage> HttpDeleteAsync(string api, Dictionary<string, object> header, Dictionary<string, object> parameters, CancellationTokenSource tokensource = null)
        {
            if (user == null)//exclude null exception
            {
                Debug.WriteLine("Please login first!");
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);                
            }

            Dictionary<string, object> req_header = new Dictionary<string, object>
            {
                {"Referer","http://spapi.pixiv.net/" },//header
                {"UserAgent","PixivIOSApp/5.8.3" },
                {"Authorization",("Bearer "+user.access_token) }
            };

            if (header == null) header = new Dictionary<string, object>();
            //Add header
            foreach (KeyValuePair<string, object> x in header)
            {
                if (req_header.ContainsKey(x.Key)) req_header[x.Key] = x.Value;
                else req_header.Add(x.Key, x.Value);
            }

            HttpClient http = new HttpClient(new HttpClientHandler() { CookieContainer = new CookieContainer(), UseCookies = true });
            http.DefaultRequestHeaders.Clear();
            foreach (KeyValuePair<string, object> x in req_header)
            {
                http.DefaultRequestHeaders.Add(x.Key, (string)x.Value);
            }


            if (parameters == null) parameters = new Dictionary<string, object>();

            var queryString = string.Join("&", parameters.Where(p => p.Value == null || p.Value.GetType().IsValueType || p.Value.GetType() == typeof(string)).Select(p => string.Format("{0}={1}", Uri.EscapeDataString(p.Key), Uri.EscapeDataString(string.Format("{0}", p.Value)))));

            if (api.IndexOf("?") < 0)
            {
                api = string.Format("{0}?{1}", api, queryString);
            }
            else
            {
                api = string.Format("{0}&{1}", api, queryString);
            }

            api = api.Trim('&', '?');

            if (tokensource == null)
                return await http.DeleteAsync(api);
            return await http.DeleteAsync(api, tokensource.Token);
        }
        private string GetNonceString(int length = 8)
        {
            var sb = new StringBuilder();

            var rnd = new Random();
            for (var i = 0; i < length; i++)
            {

                sb.Append((char)rnd.Next(97, 123));

            }
            return sb.ToString();
        }
        /// <summary>
        /// it's a new base api which can use it to download picture:)
        /// </summary>
        /// <param name="header">reqest header (can be null)</param>
        /// <param name="strUrl"></param>
        /// <returns></returns>
        public async Task<Stream> HttpGetStreamAsync(Dictionary<string, object> header, string strUrl, CancellationTokenSource tokensource = null)
        {
            if (user == null)//exclude null exception
            {
                Debug.WriteLine("Please login first!");
                return null;
            }

            Dictionary<string, object> req_header = new Dictionary<string, object>
            {
                {"Referer","http://spapi.pixiv.net/" },//header
                {"UserAgent","PixivIOSApp/5.8.3" },
                {"Authorization",("Bearer "+user.access_token) }
            };

            if (header == null) header = new Dictionary<string, object>();
            //Add header
            foreach (KeyValuePair<string, object> x in header)
            {
                if (req_header.ContainsKey(x.Key)) req_header[x.Key] = x.Value;
                else req_header.Add(x.Key, x.Value);
            }

            HttpClient http = new HttpClient(new HttpClientHandler() { CookieContainer = new CookieContainer(), UseCookies = true });
            http.DefaultRequestHeaders.Clear();
            foreach (KeyValuePair<string, object> x in req_header)
                http.DefaultRequestHeaders.Add(x.Key, (string)x.Value);


            if (tokensource != null)
                return await Task.Run(() => { return http.GetStreamAsync(strUrl); }, tokensource.Token);
            return await http.GetStreamAsync(strUrl);


        }
        #endregion
    }

    public class pixivUser
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        /// <summary>
        /// 0 small 1 middle 2 big
        /// </summary>
        public string[] avatar { get; set; }
        public int expires_time { get; set; }
    }
}
