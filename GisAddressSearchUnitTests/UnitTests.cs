using System;
using Xunit;
using GisAddressSearch;
using System.Collections.Generic;
using System.Linq;

namespace GisAddressSearchUnitTests
{
    public class UnitTests
    {
        GisSearchService addressService;

        public UnitTests()
        {
            addressService = new();
        }

        [Fact]
        public void Get_Only_One_Address()
        {
            var addressParts = addressService.GetAddressParts("Красностуденческий п", GisAddressLevel.Street, "0c5b2444-70a0-4932-980c-b4dc0d3f02b5");
            var fiasAddressParts = addressService.GetAddressParts("Красностуденческий проезд", GisAddressLevel.Street, "0c5b2444-70a0-4932-980c-b4dc0d3f02b5");

            var address = Assert.Single(addressParts);
            var fiasAddress = Assert.Single(fiasAddressParts);

            Assert.Equal(address, fiasAddress);
        }


        [Fact]
        public void Get_Address_Houses()
        {
            var fiasAddressStreet = new FiasAddressPart()
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
            var fiasAddresses = new List<FiasAddressPart>()
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

            var addressParts = addressService.GetAddressParts("4", GisAddressLevel.House, "73b67e9d-4c88-44b2-9758-d7a367c4bc27").ToList();

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
            var addressParts = addressService.GetAddressParts(houseNumber, GisAddressLevel.House, streetId).Select(addr => addr.GetFullName()).ToList();
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

        public static IEnumerable<object[]> HousesTestData()
        {
            yield return new object[] { "4", "73b67e9d-4c88-44b2-9758-d7a367c4bc27", 
                new object[] { "4", "4, к. 2", "4, стр. 2", "4, к. 2, стр. 1" } };
            yield return new object[] { "2", "25dcbf42-f6a0-4744-8166-6e5ec03bfa79", 
                new object[] { "2", "2А", "2Б", "2Д", "2, к. 1", "2, стр. 2", "2, стр. 4", "2, стр. 5", "2, стр. 6", "2А, стр. 1" } };
            yield return new object[] { "3", "4b1fd1ed-6e22-4a33-857b-ccebb07020e1", 
                new object[] { "3А", "3Б", "31", "31А", "37", "37А", "39А", "3, стр. 1", "3, стр. 2", "3, стр. 3" } };
            yield return new object[] { "7", "0c5b2444-70a0-4932-980c-b4dc0d3f02b5", new object[0] };
        }

        public static IEnumerable<object[]> CheckTestData()
        {
            yield return new object[] { "73b67e9d-4c88-44b2-9758-d7a367c4bc27", (int)FiasAddressLevel.House, "4, к. 2", true };
            yield return new object[] { "73b67e9d-4c88-44b2-9758-d7a367c4bc27", (int)FiasAddressLevel.House, "4, к. 5", false };
            yield return new object[] { "0c5b2444-70a0-4932-980c-b4dc0d3f02b5", (int)FiasAddressLevel.Street, "Красностуденческий проезд", true };
            yield return new object[] { "0c5b2444-70a0-4932-980c-b4dc0d3f02b5", (int)FiasAddressLevel.Street, "Красностудсенческий проезд", false };
        }

        public static IEnumerable<object[]> SearchTestData()
        {
            yield return new object[] { "Моск", (int)FiasAddressLevel.Region, "", new object[] { "Москва г", "Московская обл" } };
            yield return new object[] { "Смол", (int)FiasAddressLevel.Street, "0c5b2444-70a0-4932-980c-b4dc0d3f02b5", 
                new object[] { "Смоленская ул", "Смоленская-Сенная пл", "Смоленский б-р", "Смольная ул", "Смоленская наб", "Смоленская пл" } };
        }
    }
}
