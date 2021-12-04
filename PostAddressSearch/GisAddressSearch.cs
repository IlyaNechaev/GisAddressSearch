using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Text;

namespace GisAddressSearch
{
    public enum FiasAddressLevel
    {
        /// <remarks/>
        Region = 1,

        /// <remarks/>
        AO = 2,

        /// <remarks/>
        District = 4,

        /// <remarks/>
        City = 8,

        /// <remarks/>
        IntraArea = 16,

        /// <remarks/>
        Locality = 32,

        /// <remarks/>
        Street = 64,

        /// <remarks/>
        House = 128,

        /// <remarks/>
        AddTerritory = 256,

        /// <remarks/>
        SubAddTerritory = 512,

        /// <remarks/>
        Flat = 1024,

        /// <remarks/>
        Settlements = 2048,

        /// <remarks/>
        PlanStructure = 4096,

        /// <remarks/>
        LandPlot = 8192,

        /// <remarks/>
        AllLevels = 16384,
    }
    public enum GisAddressLevel
    {
        /// <remarks/>
        Region = 1,

        /// <remarks/>
        District = 3,

        /// <remarks/>
        City = 4,

        /// <remarks/>
        Locality = 6,

        /// <remarks/>
        Street = 7,

        /// <remarks/>
        House = 8,

        PlanStructure = 65
    }

    public static class GisFiasAddressExtensions
    {
        public static GisAddressLevel ConvertToGis(this FiasAddressLevel level)
        {
            var levels = ((IEnumerable<FiasAddressLevel>)Enum.GetValues(typeof(FiasAddressLevel)))
                .Where(l => (level & l) == l)
                .ToArray();

            var reductDic = new Dictionary<FiasAddressLevel, string>();
            reductDic.Add(FiasAddressLevel.AO, Enum.GetName(typeof(FiasAddressLevel), FiasAddressLevel.Region));
            reductDic.Add(FiasAddressLevel.SubAddTerritory, Enum.GetName(typeof(FiasAddressLevel), FiasAddressLevel.Street));
            reductDic.Add(FiasAddressLevel.AddTerritory, Enum.GetName(typeof(FiasAddressLevel), FiasAddressLevel.PlanStructure));

            var gisAddressLevels = ((IEnumerable<GisAddressLevel>)Enum.GetValues(typeof(GisAddressLevel)))
                .ToDictionary(value => Enum.GetName(typeof(GisAddressLevel), value));

            GisAddressLevel gisAddressLevel = 0;
            foreach (var l in levels)
            {
                if (reductDic.ContainsKey(l))
                {
                    gisAddressLevel = gisAddressLevel | gisAddressLevels[reductDic[l]];
                }
                else
                {
                    gisAddressLevel = gisAddressLevel | gisAddressLevels[Enum.GetName(typeof(FiasAddressLevel), l)];
                }
            }

            return gisAddressLevel;
        }

        public static FiasAddressLevel ConvertMultipleToFias(this GisAddressLevel level, string stname = "")
        {
            var levels = ((IEnumerable<GisAddressLevel>)Enum.GetValues(typeof(GisAddressLevel)))
                 .Where(l => (level & l) == l)
                 .ToArray();

            // У ГИС ПА уровни AO и Region объединены в Region и различаются только по приставке "АО"
            var reductDic = new Dictionary<string, FiasAddressLevel>();
            reductDic.Add("АО", FiasAddressLevel.AO);

            var gisAddressLevels = ((IEnumerable<FiasAddressLevel>)Enum.GetValues(typeof(FiasAddressLevel)))
                .ToDictionary(value => Enum.GetName(typeof(FiasAddressLevel), value));

            FiasAddressLevel gisAddressLevel = reductDic.ContainsKey(stname) ? reductDic[stname] : 0;

            foreach (var l in levels)
            {
                gisAddressLevel = gisAddressLevel | gisAddressLevels[Enum.GetName(typeof(GisAddressLevel), l)];
            }

            return gisAddressLevel;           
        }

        // level должен принимать только одно значение GisAddressLevel, поскольку 
        public static FiasAddressLevel ConvertToFias(this GisAddressLevel level, string stname = "")
        {
            var addressLevelName = Enum.GetName(typeof(GisAddressLevel), level);
            var fiasAddressLevel = ((IEnumerable<FiasAddressLevel>)Enum.GetValues(typeof(FiasAddressLevel)))
                .FirstOrDefault(value => string.Equals(Enum.GetName(typeof(FiasAddressLevel), value), addressLevelName));

            // У ГИС ПА уровни AO и Region объединены в Region и различаются только по приставке "АО"
            return fiasAddressLevel.Equals(FiasAddressLevel.Region) ?
                (string.Equals(stname, "АО") ? FiasAddressLevel.AO : FiasAddressLevel.Region) :
                fiasAddressLevel;
        }
    }

    public class GisSearchService
    {
        private string _mainUrl = "https://address.pochta.ru/suggest/api/v4_5";
        private string _childrenUrl = "https://address.pochta.ru/suggest/api/v4_5/children";

        private string PerformGisRequest(string url, string data, string method)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            var httpRequest = (HttpWebRequest)WebRequest.Create(url);

            httpRequest.Method = method;
            httpRequest.ContentType = "application/json";
            httpRequest.Headers["Authorization"] = "Bearer 4319be2a-2253-47b3-8944-0b69c7134d36";

            using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
            {
                streamWriter.Write(data);
            }

            HttpWebResponse httpResponse = null;

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

        // Получить адреса по его части
        public GisAddressSearchResponse GetAddress(string address)
        {
            var gisRequest = new GisAddressSearchRequest
            {
                appName = "ПК МС",
                reqId = Guid.NewGuid().ToString(),
                rmFedCities = false,
                addr = address,
                addressType = 1
            };

            var data = JsonHelper.Serialize(gisRequest);

            var result = PerformGisRequest(_mainUrl, data, "POST");

            if (string.IsNullOrEmpty(result))
                throw new WebException("Не удалось получить адрес", WebExceptionStatus.ReceiveFailure);

            GisAddressSearchResponse gisResponse = new GisAddressSearchResponse();
            try
            {
                gisResponse = JsonHelper.Deserialize<GisAddressSearchResponse>(result);
            }
            catch (Exception)
            {
                throw;
            }

            return gisResponse;
        }

        public GisAddressChildrenSearchResponse GetAddressChildren(string addressId, int countOnPage = 300)
        {
            var gisRequest = new GisAddressChildrenSearchRequest()
            {
                appName = "ПК МС",
                reqId = Guid.NewGuid().ToString(),
                countOnPage = countOnPage,
                parentGuid = addressId
            };

            var data = JsonHelper.Serialize(gisRequest);

            var result = PerformGisRequest(_childrenUrl, data, "POST");

            if (string.IsNullOrEmpty(result))
                throw new WebException("Не удалось получить адрес", WebExceptionStatus.ReceiveFailure);

            GisAddressChildrenSearchResponse gisResponse = new GisAddressChildrenSearchResponse();
            try
            {
                gisResponse = JsonHelper.Deserialize<GisAddressChildrenSearchResponse>(result);
            }
            catch (Exception)
            {
                throw;
            }

            return gisResponse;
        }

        // Получить список возможных значений элемента адреса
        public FiasAddressPart[] GetAddressParts(string address, GisAddressLevel level, string parentId = "")
        {
            try
            {
                new Guid(parentId);
            }
            catch (FormatException)
            {
                parentId = string.Empty;
            }

            if (string.IsNullOrEmpty(address))
                return new FiasAddressPart[0];

            IDictionary<Guid, FiasAddressPart> gisAddressParts = new Dictionary<Guid, FiasAddressPart>();
            var fullAddress = address;

            if (!string.IsNullOrEmpty(parentId))
            {
                fullAddress = $"{GetAddressPartById(parentId).name}, {address}";
            }
            else if (level != GisAddressLevel.Region)
            {
                return new FiasAddressPart[0];
            }

            var gisAddressResponse = GetAddress(fullAddress);
            if (!string.IsNullOrEmpty(gisAddressResponse.errorCode) && gisAddressResponse.addr?.Count == 0)
            {
                throw new GisSearchException($"Не удалось получить адреса. ErrorCode: {gisAddressResponse.errorCode}");
            }

            foreach (var gisAddress in gisAddressResponse.addr)
            {
                var addressPart = GetAddressPart(gisAddress, level);

                // Если элемент адреса уже не присутствует в коллекции, начинается с запрашиваемого названия (address) и
                // имеет предка с parentId, то добавляем элемент в коллекцию
                if (addressPart != null &&
                    !gisAddressParts.ContainsKey(addressPart.fiasId) &&
                    addressPart.GetFullName().ToLower().StartsWith(address.ToLower()) &&
                    (string.IsNullOrEmpty(parentId) || addressPart.StartsWith(new Guid(parentId)) != null))
                {
                    gisAddressParts.Add(addressPart.fiasId, addressPart);
                }
            }

            return gisAddressParts.Values.ToArray();
        }

        private FiasAddressPart GetAddressPartById(string addressId)
        {
            var gisResponse = GetAddressChildren(addressId, 1);

            if (gisResponse.children.Count < 1)
                return null;

            var fiasAddressPart = GetAddressPart(gisResponse.children[0], (GisAddressLevel)gisResponse.children[0].level);
            var addressPart = fiasAddressPart.StartsWith(new Guid(addressId));

            return addressPart;
        }

        private FiasAddressPart GetAddressPart(GisAddress address, GisAddressLevel level)
        {

            int indexOfElement = -1;
            for (int i = 0; i < address.elements.Count; i++)
            {
                if (address.elements[i].level == (int)level)
                {
                    indexOfElement = i;
                    break;
                }
            }
            if (indexOfElement == -1) return null;

            var addressPart = new FiasAddressPart();
            addressPart.FromAddress(address, indexOfElement);

            return addressPart;
        }

        private GisAddressElement GetAddressElement(string elementId)
        {
            throw new NotImplementedException();
        }
    }


    public class GisAddressSearchRequest
    {
        public GisAddressSearchRequest()
        {
            appName = string.Empty;
            reqId = string.Empty;
            rmFedCities = false;
            addr = string.Empty;
            addressType = 1;
        }
        public string appName { get; set; }
        public string reqId { get; set; }
        public bool rmFedCities { get; set; }
        public string addr { get; set; }
        public int addressType { get; set; }
    }

    public class GisAddressChildrenSearchRequest
    {
        public GisAddressChildrenSearchRequest()
        {
            appName = string.Empty;
            reqId = string.Empty;
            countOnPage = 0;
            parentGuid = string.Empty;
        }
        public string appName { get; set; }
        public string reqId { get; set; }
        public int countOnPage { get; set; }
        public string parentGuid { get; set; }

    }

    public class GisAddressSearchResponse
    {
        public GisAddressSearchResponse()
        {
            appName = string.Empty;
            reqId = string.Empty;
            request = string.Empty;
            errorCode = string.Empty;
            addr = new List<GisAddress>();
        }
        public string appName { get; set; }
        public string reqId { get; set; }
        public string request { get; set; }
        public string errorCode { get; set; }
        public List<GisAddress> addr { get; set; }
    }

    public class GisAddressChildrenSearchResponse
    {
        public string appName { get; set; }
        public Guid reqId { get; set; }
        public int firstElemNum { get; set; }
        public bool isEnd { get; set; }
        public GisAddressChildrenSearchRequest request { get; set; }
        public List<GisAddress> children { get; set; }
    }

    public class GisAddress
    {
        public string addressGuid { get; set; }
        public int historical { get; set; }
        public string outAddr { get; set; }
        public string outAddrHighlighted { get; set; }
        public string missing { get; set; }
        public string index { get; set; }
        public int deliveryArea { get; set; }
        public string oktmo { get; set; }
        public string oktmoName { get; set; }
        public string parentOktmo { get; set; }
        public string parentOktmoName { get; set; }
        public int addressType { get; set; }
        public int origin { get; set; }
        public int level { get; set; }
        public List<GisAddressElement> elements { get; set; }
    }

    public class GisAddressElement
    {
        public string guid { get; set; }
        public string val { get; set; }
        public string tname { get; set; }
        public string valWithType { get; set; }
        public string stname { get; set; }
        public string content { get; set; }
        public int origin { get; set; }
        public int historical { get; set; }
        public int level { get; set; }
    }

    public class FiasAddressPart
    {
        public Guid id;
        public Guid fiasId;

        public FiasAddressLevel level;

        public string name;
        public string index;

        public string structure;
        public string housings;

        public FiasAddressPart parent;

        public FiasAddressPart()
        {
            id = new Guid();
            fiasId = new Guid();
            level = FiasAddressLevel.AllLevels;
            name = string.Empty;
            index = null;
            structure = null;
            housings = null;
            parent = null;
        }

        public string GetFullName()
        {
            if ((level & FiasAddressLevel.House) == FiasAddressLevel.House)
            {
                return name +
                    (!string.IsNullOrEmpty(housings) ? $", к. {housings}" : string.Empty) +
                    (!string.IsNullOrEmpty(structure) ? $", стр. {structure}" : string.Empty);
            }
            else
            {
                return name;
            }
        }

        public void FromAddress(GisAddress address, int indexOfElement)
        {
            var element = address.elements[indexOfElement];
            level = ((GisAddressLevel)element.level).ConvertToFias(element.stname);
            name = level == FiasAddressLevel.House ? element.val : element.valWithType;
            fiasId = new Guid(element.guid);
            id = fiasId;


            if ((GisAddressLevel)element.level == GisAddressLevel.House)
            {
                index = address.index;
                // Если в элементе указана дополнительная информация про дом (корпус, строение)
                if (address.elements.Count - 1 > indexOfElement)
                {
                    for (int i = address.elements.Count - 1; i > indexOfElement; i--)
                    {
                        element = address.elements[i];
                        switch (element.content)
                        {
                            case "E":
                                housings = element.val;
                                break;
                            case "B":
                                structure = element.val;
                                break;
                        }
                    }
                }
            }

            if ((GisAddressLevel)element.level == GisAddressLevel.Region)
            {
                parent = null;
            }
            else
            {
                parent = new FiasAddressPart();
                parent.FromAddress(address, indexOfElement - 1);
            }
        }

        // Возвращает часть адреса, fiasId которой равен addressId, и всех предков
        public FiasAddressPart StartsWith(Guid addressId)
        {
            if (fiasId.Equals(addressId))
                return this;
            else
            {
                if (parent == null)
                    return null;
                else
                    return parent.StartsWith(addressId);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is FiasAddressPart)
            {
                var address = (FiasAddressPart)obj;
                bool isEqual = (id == address.id) &&
                    fiasId.Equals(address.fiasId) &&
                    level == address.level &&
                    name.Equals(address.name) &&
                    ((index == null && address.index == null) || index.Equals(address.index)) &&
                    ((structure == null && address.structure == null) || structure.Equals(address.structure)) &&
                    ((housings == null && address.housings == null) || housings.Equals(address.housings)) &&
                    ((parent == null && address.parent == null) || parent.Equals(address.parent));

                return isEqual;
            }
            else
            {
                return base.Equals(obj);
            }
        }
    }

    public class GisSearchException : Exception
    {
        public GisSearchException(string message) : base(message)
        {
        }
    }
}
