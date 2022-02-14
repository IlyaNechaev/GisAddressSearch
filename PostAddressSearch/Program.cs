using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace GisAddressSearch
{
    class Program
    {
        static string authToken = "Bearer 4319be2a-2253-47b3-8944-0b69c7134d36";
        static string mainUrl = "https://address.pochta.ru/suggest/api/v4_5";
        static string childrenUrl = "https://address.pochta.ru/suggest/api/v4_5/children";

        static void Main(string[] args)
        {
            var gisService = new GisSearchService(authToken, mainUrl, childrenUrl);

            var kladr = gisService.GetKladrAddressByElements("0bc1452a-6566-4f5d-b25f-4d4d51beb78a", GetRegionCode);

            Console.WriteLine("643,424032,12,Йошкар-Ола г,,Милосердие снт,2-я линия,27,,6");
            Console.WriteLine(kladr.AddressString);

            Console.ReadKey();
        }

        public static string GetRegionCode(string fiasId)
        {
            var code = string.Empty;
            var connectionString = "Persist Security Info=False;Server=localhost\\SQLEXPRESS;Database=GisPaTest;Trusted_Connection=True;";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"select top 1 [Code] from RegionCode where FiasId = \'{fiasId}\'";
                    var reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        code = reader.GetByte(0).ToString();
                    }
                }
                connection.Close();
            }

            return code;
        }
    }
}
