using B2Library.Entities;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace B2Library
{
    public class Utilities
    {
        public static AuthorizeResponse AuthorizeUser(string accountId, string applicationKey)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://api.backblaze.com/b2api/v1/b2_authorize_account");
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", accountId, applicationKey)));
            webRequest.Headers.Add("Authorization", "Basic " + credentials);
            webRequest.ContentType = "application/json; charset=utf-8";
            WebResponse response = (HttpWebResponse)webRequest.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            response.Close();
            return JsonConvert.DeserializeObject<AuthorizeResponse>(responseString);
        }

        public static async Task<ListBucketsResponse> ListBuckets(ListBucketsRequest request, string authToken, string apiUrl)
        {
            var headers = GetAuthHeaders(authToken);

            string responseString = await MakeRequest2(apiUrl + "/b2api/v1/b2_list_buckets", headers, JsonConvert.SerializeObject(request));

            return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<ListBucketsResponse>(responseString));
        }

        public static List<Tuple<string, string>> GetAuthHeaders(string authToken)
        {
            List<Tuple<string, string>> headers = new List<Tuple<string, string>>();
            headers.Add(new Tuple<string, string>("Authorization", authToken));
            return headers;
        }

        public static async Task<GetUploadURLResponse> GetUploadURL(GetUploadURLRequest request, string apiUrl, string authToken)
        {
            var headers = GetAuthHeaders(authToken);
            string responseString = await MakeRequest2(apiUrl + "/b2api/v1/b2_get_upload_url", headers, JsonConvert.SerializeObject(request));

            return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<GetUploadURLResponse>(responseString));
        }

        public static string getValidFilename(string input)
        {
            string fileName = input.Replace('\\', '/');
            fileName = fileName.Replace(' ', '_');
            if (fileName.StartsWith("/"))
            {
                fileName = fileName.Substring(1);
            }
            return fileName;
        }

        public static UploadFileResponse UploadFile(string authToken, string contentType, string filePath, string uploadUrl)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Debug("Starting Uploading {0}", filePath);

            String sha1 = GetSha1(filePath);

            var headers = GetAuthHeaders(authToken);

            string fileName = getValidFilename(filePath);

            headers.Add(new Tuple<string, string>("X-Bz-File-Name", fileName));
            headers.Add(new Tuple<string, string>("X-Bz-Content-Sha1", sha1));


            string responseString = MakeRequest2(uploadUrl, headers, filePath, true, contentType).Result;

            var resp = JsonConvert.DeserializeObject<UploadFileResponse>(responseString);

            if (resp.contentSha1 == sha1)
            {
                Console.WriteLine(responseString);
                return resp;
            }
            else
            {
                //something went wrong!
                return null;
            }

        }

        public static ListFileNamesResponse ListFileNames(ListFileNamesRequest request, string apiUrl, string authToken)
        {
            var headers = GetAuthHeaders(authToken);
            string responseString = MakeRequest2(string.Format("{0}/b2api/v1/b2_list_file_names", apiUrl), headers, JsonConvert.SerializeObject(request)).Result;

            return JsonConvert.DeserializeObject<ListFileNamesResponse>(responseString);
        }


        public static async Task<string> MakeRequest2(string url, List<Tuple<string, string>> headers, string data, bool isFile = false, string contentType = "application/json; charset=utf-8")
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            var client = new HttpClient();
            if (isFile)
            {
                client.Timeout = TimeSpan.FromMinutes(60);
            }

            foreach (var head in headers)
            {
                client.DefaultRequestHeaders.Add(head.Item1, head.Item2);
            }

            HttpContent content = null;
            if (isFile)
            {
                content = new StreamContent(System.IO.File.OpenRead(data));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            }
            else
            {
                content = new StringContent(data);
            }
            var resp = await client.PostAsync(url, content);
            try
            {
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                logger.Error("Error getting respoonse: {0} {1}", ex.Message, ex.StackTrace);
                throw;
            }
        }


        public static string GetSha1(string fileName)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                using (FileStream fs = System.IO.File.OpenRead(fileName))
                {
                    var hash = sha1.ComputeHash(fs);
                    var sb = new StringBuilder(hash.Length * 2);

                    foreach (byte b in hash)
                    {
                        // can be "x2" if you want lowercase
                        sb.Append(b.ToString("X2"));
                    }
                    return sb.ToString();
                }
            }
        }
    }
}
