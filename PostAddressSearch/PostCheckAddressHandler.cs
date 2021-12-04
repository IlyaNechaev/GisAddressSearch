using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Reflection;

namespace GisAddressSearch
{
    class PostCheckAddressHandler
    {
        public void ProcessRequest()
        {
            SendUsingRequest();
        }

        public void SendUsingClient()
        {
            string fileName = string.Empty;
            string url1 = "http://jsonplaceholder.typicode.com/posts";
            string url2 = "https://address.pochta.ru/suggest/api/v4_5";
            string data = "{"
    + "\"appName\": \"ПК МС\","
    + "\"reqId\": \"${#Project#UID}\","
    + "\"rmFedCities\": false,"
    + "\"addr\": \"Москва, Красностуденческий пр-д\","
    + "\"addressType\": 1"
+ "\"}\"";
            string queryString = string.Empty;

            WebClient client = new WebClient();
            client.Headers.Add("content-type", "application/json;charset=UTF-8");
            var result = Encoding.UTF8.GetString(client.DownloadData(url1));
            var result2 = client.DownloadString(url1);

            /*
            client.Headers.Add("Accept-Encoding", "gzip,deflate");
            client.Headers.Add("Authorization", "Bearer 4319be2a-2253-47b3-8944-0b69c7134d36");
            //client.Headers.Add("Content-Length", "206");
            //client.Headers.Add("Host", "address.pochta.ru");
            //client.Headers.Add("Connection", "Keep-Alive");
            client.Headers.Add("User-Agent", "Apache-HttpClient/4.5.5 (Java/12.0.1)");*/

            var result3 = client.UploadString(url2, data);
        }

        public void SendUsingRequest()
        {
            var gisSearchService = new GisSearchService();
            FiasAddressPart[] addresses = null;
            try
            {
                addresses = gisSearchService.GetAddressParts("3, к. 3", GisAddressLevel.House, "a4e0b9b0-eb84-42ec-8bb8-758facbbf4dd");
            }
            catch (GisSearchException ex)
            {
                Console.WriteLine(ex);
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"{addresses[0].GetFullName()} ({addresses[0].fiasId})");
            Console.ReadKey();
        }
    }
}
