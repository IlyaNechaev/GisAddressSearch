using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace GisAddressSearch
{
    class Program
    {
        string _authToken = "Bearer 4319be2a-2253-47b3-8944-0b69c7134d36";

        static void Main(string[] args)
        {
            


        }

        private string PerformGisRequest(string url, string data, string method)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            var httpRequest = (HttpWebRequest)WebRequest.Create(url);

            httpRequest.Method = method;
            httpRequest.ContentType = "application/json";
            httpRequest.Headers["Authorization"] = _authToken;

            using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
            {
                streamWriter.Write(data);
            }

            HttpWebResponse httpResponse;

            try
            {
                httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            }
            catch (WebException)
            {
                throw;
            }

            var result = string.Empty;
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }

            return result;
        }
    }
}
