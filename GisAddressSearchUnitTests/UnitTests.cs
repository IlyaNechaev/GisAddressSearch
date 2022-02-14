using System;
using Xunit;
using GisAddressSearch;
using FiasCode;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;

namespace GisAddressSearchUnitTests
{
    public class UnitTests
    {
        GisSearchService addressService;

        public UnitTests()
        {
            var authToken = "Bearer 4319be2a-2253-47b3-8944-0b69c7134d36";
            var mainUrl = "https://address.pochta.ru/suggest/api/v4_5";
            var childrenUrl = "https://address.pochta.ru/suggest/api/v4_5/children";

            addressService = new(authToken, mainUrl, childrenUrl);
        }

        [Fact]
        public void Get_Only_One_Address()
        {
            var levels = new GisAddressLevel[] { GisAddressLevel.Street };
            var addressParts = addressService.GetAddressParts("Красностуденческий п", levels, "0c5b2444-70a0-4932-980c-b4dc0d3f02b5");
            var fiasAddressParts = addressService.GetAddressParts("Красностуденческий проезд", levels, "0c5b2444-70a0-4932-980c-b4dc0d3f02b5");

            var address = Assert.Single(addressParts);
            var fiasAddress = Assert.Single(fiasAddressParts);

            Assert.Equal(address, fiasAddress);
        }


        [Fact]
        public void Get_Address_Houses()
        {
            var fiasAddressStreet = new GisAddressPart()
            {
                fiasId = new Guid("73b67e9d-4c88-44b2-9758-d7a367c4bc27"),
                id = new Guid("73b67e9d-4c88-44b2-9758-d7a367c4bc27"),
                name = "Красностуденческий проезд",
                level = FiasAddressLevel.Street,
                parent = new()
                {
                    fiasId = new Guid("0c5b2444-70a0-4932-980c-b4dc0d3f02b5"),
                    id = new Guid("0c5b2444-70a0-4932-980c-b4dc0d3f02b5"),
                    level = FiasAddressLevel.Region,
                    name = "Москва г"
                }
            };
            var fiasAddresses = new List<GisAddressPart>()
            {
                new()
                {
                    fiasId = new Guid("77b7217e-8c1d-484d-9ec9-7931b12ed01c"),
                    id = new Guid("77b7217e-8c1d-484d-9ec9-7931b12ed01c"),
                    level = FiasAddressLevel.House,
                    name = "4",
                    index = "127434",
                    parent = fiasAddressStreet
                },
                new()
                {
                    fiasId = new Guid("a6a6bc37-08ed-4ae6-8d08-12d11067a69f"),
                    id = new Guid("a6a6bc37-08ed-4ae6-8d08-12d11067a69f"),
                    level = FiasAddressLevel.House,
                    name = "4",
                    housings = "2",
                    index = "127434",
                    parent = fiasAddressStreet
                },
                new()
                {
                    fiasId = new Guid("87ddf1c8-46c5-4aa0-80c8-460479426ab4"),
                    id = new Guid("87ddf1c8-46c5-4aa0-80c8-460479426ab4"),
                    level = FiasAddressLevel.House,
                    name = "4",
                    structure = "2",
                    index = "127434",
                    parent = fiasAddressStreet
                },
                new()
                {
                    fiasId = new Guid("6236b8ec-808d-4117-9ff5-0e93268a1063"),
                    id = new Guid("6236b8ec-808d-4117-9ff5-0e93268a1063"),
                    level = FiasAddressLevel.House,
                    name = "4",
                    housings = "2",
                    structure = "1",
                    index = "127434",
                    parent = fiasAddressStreet
                }
            };

            var levels = new GisAddressLevel[] { GisAddressLevel.House };
            var addressParts = addressService.GetAddressParts("4", levels, "73b67e9d-4c88-44b2-9758-d7a367c4bc27").ToList();

            foreach (var addr in addressParts)
            {
                if (fiasAddresses.Contains(addr))
                    fiasAddresses.Remove(addr);
            }

            Assert.Empty(fiasAddresses);
        }

        [Theory]
        [MemberData(nameof(HousesTestData))]
        public void Get_Address_Houses_With_Only_Number(string houseNumber, string streetId, string[] houses)
        {
            var levels = new GisAddressLevel[] { GisAddressLevel.House };
            var addressParts = addressService.GetAddressParts(houseNumber, levels, streetId).Select(addr => addr.GetFullName()).ToList();
            houses = houses.Select(h => h.Trim()).ToArray();

            foreach (var house in houses)
            {
                if (addressParts.Contains(house))
                    addressParts.Remove(house);
            }
            Assert.Empty(addressParts);
        }

        [Theory]
        [MemberData(nameof(CheckTestData))]
        public void Check_Address(string parentId, int levels, string name, bool isChecked)
        {
            var gisAddressLevel = ((FiasAddressLevel)levels).ConvertToGis();
            var addressParts = addressService.GetAddressParts(name, gisAddressLevel, parentId);

            addressParts = addressParts.Where(addr => addr.GetFullName().Equals(name)).ToArray();
            if (isChecked)
            {
                var address = Assert.Single(addressParts);
                Assert.True(address.GetFullName().Equals(name));
            }
            else
            {
                Assert.Empty(addressParts);
            }
        }

        [Theory]
        [MemberData(nameof(SearchTestData))]
        public void Search_Address(string request, int levels, string parentId, string[] results)
        {
            var gisAddressLevel = ((FiasAddressLevel)levels).ConvertToGis();
            var addressParts = addressService.GetAddressParts(request, gisAddressLevel, parentId);

            Assert.Equal(results.Length, addressParts.Length);
            foreach (var address in addressParts)
            {
                Assert.Contains(address.GetFullName(), results);
            }
        }

        [Theory]
        [MemberData(nameof(ConvertTestData))]
        public void Check_Convertion(HashSet<GisAddressLevel> gisLevels, HashSet<FiasAddressLevel> fiasLevels)
        {
            FiasAddressLevel fiasLevel = 0;
            foreach (var l in fiasLevels)
            {
                fiasLevel = fiasLevel | l;
            }

            Assert.Equal(gisLevels, fiasLevel.ConvertToGis().ToHashSet());

            foreach (var l in gisLevels)
            {
                Assert.Contains(l.ConvertToFias(), fiasLevels);
            }
        }

        [Theory]
        [MemberData(nameof(HouseTestData))]
        public void Check_Increase_Count_On_Page(string request, GisAddressLevel[] gisLevels, string parentId, string[] results)
        {
            var addressParts = addressService.GetAddressParts(request, gisLevels, parentId);
            var addresses = addressParts.Select(ap => ap.GetFullName());

            foreach (var result in results)
            {
                Assert.Contains(result, addresses);
            }
        }

        [Theory(DisplayName = "КЛАДР")]
        [MemberData(nameof(KladrTestData))]
        public void Check_Kladr(string addressId, bool findRegionCode, KladrAddress resultKladr, string resultString)
        {
            var result = addressService.GetKladrAddressByElements(addressId, findRegionCode ? GetRegionCode : null);

            if (resultKladr is not null)
            {
                Assert.Equal(result.AddressString, resultKladr.AddressString);
                Assert.Equal(result.Building, resultKladr.Building);
                Assert.Equal(result.City, resultKladr.City);
                Assert.Equal(result.District, resultKladr.District);
                Assert.Equal(result.House, resultKladr.House);
                Assert.Equal(result.Index, resultKladr.Index);
                Assert.Equal(result.Locality, resultKladr.Locality);
                Assert.Equal(result.Street, resultKladr.Street);
            }
            else
            {
                Assert.Equal(result.AddressString, resultString);
            }
        }

        [Theory]
        [MemberData(nameof(PostalCodeData))]
        public void Check_Postal_Code_Search(string postalCode, string id)
        {
            var addressParts = addressService.SearchPostalCodeElement(postalCode);

            Assert.Equal(addressParts.id.ToString(), id);
        }

        /// <remarks>Получение кода региона</remarks>
        public string GetRegionCode(string fiasId)
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

        public static IEnumerable<object[]> HousesTestData()
        {
            yield return new object[] { "4", "73b67e9d-4c88-44b2-9758-d7a367c4bc27",
                new object[] { "4", "4, к. 2", "4, стр. 2", "4, к. 2, стр. 1" } };
            yield return new object[] { "2", "25dcbf42-f6a0-4744-8166-6e5ec03bfa79",
                new object[] { "2", "2А", "2Б", "2Д", "2, к. 1", "2, к. 1, стр. 1", "2, стр. 2", "2, стр. 4", "2, стр. 5", "2, стр. 6", "2А, стр. 1", "2А, стр. 2", "2А, стр. 3" } };
            /*yield return new object[] { "3", "4b1fd1ed-6e22-4a33-857b-ccebb07020e1", 
                new object[] { "3А", "3Б", "31", "31А", "37", "37А", "39А", "3, стр. 1", "3, стр. 2", "3, стр. 3" } };*/
            yield return new object[] { "7", "0c5b2444-70a0-4932-980c-b4dc0d3f02b5", new object[0] };
        }

        public static IEnumerable<object[]> CheckTestData()
        {
            yield return new object[] { "73b67e9d-4c88-44b2-9758-d7a367c4bc27", (int)FiasAddressLevel.House, "4, к. 2", true };
            yield return new object[] { "73b67e9d-4c88-44b2-9758-d7a367c4bc27", (int)FiasAddressLevel.House, "4, к. 5", false };
            yield return new object[] { "0c5b2444-70a0-4932-980c-b4dc0d3f02b5", (int)(FiasAddressLevel.Street | FiasAddressLevel.SubAddTerritory), "Красностуденческий проезд", true };
            yield return new object[] { "0c5b2444-70a0-4932-980c-b4dc0d3f02b5", (int)(FiasAddressLevel.Street | FiasAddressLevel.SubAddTerritory), "Красностудсенческий проезд", false };
        }

        public static IEnumerable<object[]> SearchTestData()
        {
            yield return new object[] { "Моск", (int)(FiasAddressLevel.Region | FiasAddressLevel.AO), "", new object[] { "Москва г", "Московская обл" } };
            yield return new object[] { "Смол", (int)(FiasAddressLevel.Street | FiasAddressLevel.SubAddTerritory), "0c5b2444-70a0-4932-980c-b4dc0d3f02b5",
                new object[] { "Смоленская ул", "Смоленская-Сенная пл", "Смоленский б-р", "Смольная ул", "Смоленская наб", "Смоленская пл" } };
            yield return new object[] { "4", (int)FiasAddressLevel.House, "d30ffc84-9b62-42e9-ac95-a63499103c1a",
                new object[] { "4", "41", "41а", "43", "47", "49", "49а", "4а", "4б" } };
            yield return new object[] { "30/53", (int)FiasAddressLevel.House, "62865dd6-13c3-4cd2-b33a-74260fdf682d", new object[] { "30/53" } };
            yield return new object[] { "остров Попова", (int)FiasAddressLevel.Locality, "7b6de6a5-86d0-4735-b11a-499081111af8", new object[] { "Попова остров" } };
        }

        public static IEnumerable<object[]> ConvertTestData()
        {
            yield return new object[] { new HashSet<GisAddressLevel> { GisAddressLevel.City, GisAddressLevel.District },
            new HashSet<FiasAddressLevel> { FiasAddressLevel.City, FiasAddressLevel.District } };

            yield return new object[] { new HashSet<GisAddressLevel> { GisAddressLevel.City, GisAddressLevel.Locality },
            new HashSet<FiasAddressLevel> { FiasAddressLevel.City, FiasAddressLevel.Locality } };

            yield return new object[] { new HashSet<GisAddressLevel> { GisAddressLevel.PlanStructure, GisAddressLevel.House },
            new HashSet<FiasAddressLevel> { FiasAddressLevel.PlanStructure, FiasAddressLevel.House } };
        }

        public static IEnumerable<object[]> HouseTestData()
        {
            yield return new object[]
            {
                "5",
                new GisAddressLevel[] { GisAddressLevel.House },
                "fcbf0d82-176c-473f-80bb-b4048d03f9a0",
                new object[] { "55, к. 1" }
            };
            yield return new object[]
            {
                "61",
                new GisAddressLevel[] { GisAddressLevel.House },
                "fcbf0d82-176c-473f-80bb-b4048d03f9a0",
                new object[] { "61, к. 3А" }
            };
        }

        public static IEnumerable<object[]> KladrTestData()
        {
            yield return new object[]
            {
                "f24926cc-7b05-40e2-8a59-a872618d18bb",
                true,
                null,
                "643,,40,,КАЛУГА Г,,МАКСИМА ГОРЬКОГО УЛ,1Б,,"
            };
            yield return new object[]
            {
                "b502ae45-897e-4b6f-9776-6ff49740b537",
                true,
                null,
                "643,,40,,КАЛУГА Г,,,,,"
            };
            yield return new object[]
            {
                "17075b0b-7375-4e9a-8928-a636709f4fc0",
                false,
                null,
                "643,108830,,ВОРОНОВСКОЕ П,,,420 КВ-Л,,,"
            };
            yield return new object[]
            {
                "f1c72b9d-a2d7-45b7-b9f5-2222c12d5164",
                false,
                null,
                "643,613310,,ВЕРХОШИЖЕМСКИЙ Р-Н,,МОСКВА Д,,,,"
            };
        }

        public static IEnumerable<object[]> PostalCodeData()
        {
            yield return new object[]
            {
                "127434",
                "0c5b2444-70a0-4932-980c-b4dc0d3f02b5"
            };
            yield return new object[]
            {
                "248000",
                "b502ae45-897e-4b6f-9776-6ff49740b537"
            };
            yield return new object[]
            {
                "613310",
                "0b940b96-103f-4248-850c-26b6c7296728"
            };
            yield return new object[]
            {
                "613115",
                "0b940b96-103f-4248-850c-26b6c7296728"
            };
            yield return new object[]
            {
                "103426",
                "57dbff54-7a35-4d5a-ad70-11585bfd5221"
            };
            yield return new object[]
            {
                "600000",
                "f66a00e6-179e-4de9-8ecb-78b0277c9f10"
            };
        }
    }
}
