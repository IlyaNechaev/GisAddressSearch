using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace GisAddressSearch
{
    class PostSearchAddressHandler
    {
        
        public void ProcessRequest(HttpContext context)
        {
            var authToken = "Bearer 4319be2a-2253-47b3-8944-0b69c7134d36";

            var addressService = new GisSearchService(authToken);
            string url = "fias/address/0/search.aspx";
            var path = url.Split('/');

            string parentId = path[2];
            FiasAddressLevel levels = FiasAddressLevel.Region;
            string name = "Моск";

            var gisAddressLevel = levels.ConvertToGis();
            var addressParts = addressService.GetAddressParts(name, gisAddressLevel, parentId);

            Console.WriteLine($"{string.Join("\n", addressParts.Select(addr => addr.name).ToArray())}");
            Console.ReadKey();
        }
    }
}
