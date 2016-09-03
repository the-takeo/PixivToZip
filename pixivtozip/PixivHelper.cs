using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace PixivToZip
{
    public class PixivHelper
    {
        OAuth oauth;

        public async Task<bool> LogIn(string username, string password)
        {
            oauth = new OAuth();
            var task = oauth.authAsync(username, password);

            bool result = false;
            try
            { result = await task; }
            catch
            { return false; }

            return result;
        }

        public async Task<string> DownloadPicturesAsync(string id, string dir)
        {
            string folderName = "[" + await getPicturesUser(id) + "]" + await getPicturesTitle(id);

            string path = dir + @"\" + folderName;

            foreach (var url in await getPictures(id))
            {
                await downloadFileAsync(path, url);
            }

            return path;
        }

        public async Task<string> getPicturesTitle(string id)
        {
            Task<string> task = Task.Run(() =>
            {
                var json = illust_work(id);

                if (json == null) return null;

                List<string> result = new List<string>();

                return json.Value<JArray>("response")[0]["title"].ToString();
            });

            return await task;
        }

        private async Task<string> getPicturesUser(string id)
        {
            Task<string> task = Task.Run(() =>
            {
                var json = illust_work(id);

                if (json == null) return null;

                List<string> result = new List<string>();

                return json.Value<JArray>("response")[0]["user"]["name"].ToString();
            });

            return await task;
        }

        private async Task<List<string>> getPictures(string id)
        {
            Task<List<string>> task = Task.Run(() => 
            {
                var json = illust_work(id);

                if (json == null) return null;

                List<string> result = new List<string>();

                foreach (JObject response in json.Value<JArray>("response"))
                {
                    if (!response["metadata"].HasValues)//illust
                    { result.Add(response["image_urls"]["large"].ToString()); }
                    else
                    {
                        if (!(bool)response["is_manga"])//ugoira
                        {
                            result.Add(response["image_urls"]["large"].ToString());
                            result.Add(response["metadata"]["zip_urls"]["ugoira600x600"].ToString());
                        }
                        else// manga
                        {
                            foreach (JObject image in response["metadata"]["pages"].Value<JArray>())
                            {
                                result.Add(image["image_urls"]["large"].ToString());
                            }
                        }
                    }
                }
                return result;
            });

            return await task;
        }

        private JObject illust_work(string illust_id)
        {
            return illust_workAsync(illust_id).Result;
        }

        private async Task<JObject> illust_workAsync(string illust_id)
        {
            string url = ("https://public-api.secure.pixiv.net/v1/works/" + illust_id + ".json");
            var parameters = new Dictionary<string, object>(){
                   {"image_sizes", "px_128x128,small,medium,large,px_480mw" },
                   { "include_stats","true" }
            };

            var task = oauth.HttpGetAsync(url, parameters);
            System.Net.Http.HttpResponseMessage message = null;

            message = await task;

            if (!message.IsSuccessStatusCode)
            { return null; }
            return JObject.Parse(message.Content.ReadAsStringAsync().Result);
        }

        private async Task<string> downloadFileAsync(string strPathName, string strUrl)
        {
            var task = oauth.HttpGetStreamAsync(null, strUrl, null);
            int CompletedLength = 0;

            string[] split = strUrl.Split('/');
            string filename = split[split.Length - 1];

            string fileRoute = strPathName + '/' + filename;
            if (!Directory.Exists(strPathName))
            { Directory.CreateDirectory(strPathName); }
            if (File.Exists(fileRoute))
            { File.Delete(fileRoute); }

            using (FileStream FStream = new FileStream(fileRoute, FileMode.Create))
            using (Stream myStream = await task)
            {
                byte[] btContent = new byte[1024];

                await Task.Run(() =>
                {
                    while ((CompletedLength = myStream.Read(btContent, 0, 1024)) > 0)
                    {
                        FStream.Write(btContent, 0, CompletedLength);
                    }
                });

                return fileRoute;
            }
        }
    }
}
