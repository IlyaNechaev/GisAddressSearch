using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Text;
using FiasCode;
using GisAddressSearch;

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
        /// <remarks>Регион</remarks>
        Region = 1,

        /// <remarks>Автономный округ (устаревшее)</remarks>
        AO = 2,

        /// <remarks>Район</remarks>
        District = 3,

        /// <remarks>Город</remarks>
        City = 4,

        /// <remarks>Внутригородская территория (устаревшее)</remarks>
        IntraArea = 5,

        /// <remarks>Населенный пункт</remarks>
        Locality = 6,

        /// <remarks>Улица</remarks>
        Street = 7,

        /// <remarks>Здание, сооружение</remarks>
        House = 8,

        /// <remarks>Помещение</remarks>
        Flat = 9,

        /// <remarks>Городские и сельские поселения</remarks>
        Settlements = 35,

        /// <remarks>Планировочная структура</remarks>
        PlanStructure = 65,

        /// <remarks>Земельный участок</remarks>
        LandPlot = 75
    }

    public static class GisFiasAddressExtensions
    {
        public static GisAddressLevel[] ConvertToGis(this FiasAddressLevel level)
        {
            var levels = ((IEnumerable<FiasAddressLevel>)Enum.GetValues(typeof(FiasAddressLevel)))
                .Where(l => (level & l) == l)
                .ToArray();

            // Словарь, в котором хранятся следующие пары элементов
            // [Key]: устаревшие адресные уровни
            // [Value]: уровни, которые используются в ГИС ПА (вместо устаревших)
            var reductDic = new Dictionary<FiasAddressLevel, string>();
            reductDic.Add(FiasAddressLevel.AO, Enum.GetName(typeof(FiasAddressLevel), FiasAddressLevel.Region));
            reductDic.Add(FiasAddressLevel.SubAddTerritory, Enum.GetName(typeof(FiasAddressLevel), FiasAddressLevel.Street));
            reductDic.Add(FiasAddressLevel.AddTerritory, Enum.GetName(typeof(FiasAddressLevel), FiasAddressLevel.PlanStructure));

            var gisAddressLevels = ((IEnumerable<GisAddressLevel>)Enum.GetValues(typeof(GisAddressLevel)))
                .ToDictionary(value => Enum.GetName(typeof(GisAddressLevel), value));

            HashSet<GisAddressLevel> gisAddressLevel = new HashSet<GisAddressLevel>();
            foreach (var l in levels)
            {
                if (reductDic.ContainsKey(l))
                {
                    gisAddressLevel.Add(gisAddressLevels[reductDic[l]]);
                }
                else
                {
                    gisAddressLevel.Add(gisAddressLevels[Enum.GetName(typeof(FiasAddressLevel), l)]);
                }
            }

            return gisAddressLevel.ToArray();
        }

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
        #region URLs

        // URL адреса сервиса ГИС ПА
        private string _mainUrl;
        private string _childrenUrl;

        #endregion

        private string _authToken;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authToken">Bearer-токен для аутентификации при использовании сервиса ГИС ПА</param>
        /// <param name="mainUrl">Основной URL для доступа к сервису ГИС ПА</param>
        /// <param name="childrenUrl">URL для доступа к методу получения чилдренов (дочерних элементов адреса)</param>
        public GisSearchService(string authToken, string mainUrl, string childrenUrl)
        {
            _authToken = authToken.Contains("Bearer") ? authToken : "Bearer " + authToken.Trim();

            _mainUrl = mainUrl;
            _childrenUrl = childrenUrl;
        }

        private string PerformRequest(string url, string data)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
            
            httpRequest.Method = "POST";
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

        private TResult PerformGisRequest<TResult, TParam>(string url, TParam gisRequest) where TResult : class
        {
            var data = JsonHelper.Serialize(gisRequest);

            // Замена производится, поскольку при передаче в параметре stringSrc последовательности "/" или "\/"
            // запрос возвращает Internal server error. В случае с "\\/" запрос возвращает корректный ответ
            data = data.Replace("/", @"\/");

            var result = PerformRequest(url, data);

            if (string.IsNullOrEmpty(result))
                throw new WebException("Не удалось получить адрес", WebExceptionStatus.ReceiveFailure);

            TResult gisResponse;
            try
            {
                gisResponse = JsonHelper.Deserialize<TResult>(result);
            }
            catch
            {
                throw;
            }

            return gisResponse;
        }

        // Запрос адреса у ГИС ПА
        public GisAddressSearchResponse GetAddress(string address)
        {
            var gisRequest = new GisAddressSearchRequest
            {
                appName = "ПК МС",
                reqId = Guid.NewGuid().ToString(),
                rmFedCities = true,
                addr = address,
                addressType = 1
            };

            var gisResponse = PerformGisRequest<GisAddressSearchResponse, GisAddressSearchRequest>(_mainUrl, gisRequest);

            return gisResponse;
        }
         // Запрос всех потомков элемента адреса у ГИС ПА
        public GisAddressChildrenSearchResponse GetAddressChildren(string addressId, int? countOnPage = null, string address = null)
        {
            var gisResponse = new GisAddressChildrenSearchResponse();

            // Если не задано количество запрашиваемых адресов, то необходимо запросить максимально возможное их количество
            // (пока количество возвращаемых адресов перестанет увеличиваться)
            if (countOnPage == null)
            {
                var gisRequests = new GisAddressChildrenSearchRequest[2];
                var gisResponses = new GisAddressChildrenSearchResponse[2];

                countOnPage = 0;

                // Текущий запрос
                gisRequests[0] = new GisAddressChildrenSearchRequest
                {
                    appName = "ПК МС",
                    reqId = Guid.NewGuid().ToString(),
                    countOnPage = 0,
                    parentGuid = addressId,
                    stringSrc = address
                };
                // Запрос, количество запрашиваемых адресов в котором на 500 больше чем в текущем
                gisRequests[1] = new GisAddressChildrenSearchRequest
                {
                    appName = "ПК МС",
                    reqId = Guid.NewGuid().ToString(),
                    countOnPage = 0,
                    parentGuid = addressId,
                    stringSrc = address
                };

                do
                {
                    // С каждой итерацией количество запрашиваемых адресов будет увеличиваться на 500
                    countOnPage += 500;

                    gisRequests[0].countOnPage = countOnPage.Value;
                    gisRequests[1].countOnPage = countOnPage.Value + 500;

                    gisResponses[0] = PerformGisRequest<GisAddressChildrenSearchResponse, GisAddressChildrenSearchRequest>(_childrenUrl, gisRequests[0]);
                    gisResponses[1] = PerformGisRequest<GisAddressChildrenSearchResponse, GisAddressChildrenSearchRequest>(_childrenUrl, gisRequests[1]);
                }
                while (gisResponses[0].children.Count < gisResponses[1].children.Count);

                gisResponse = gisResponses[0];
            }
            // Если задано количество запрашиваемых адресов
            else
            {
                var gisRequest = new GisAddressChildrenSearchRequest
                {
                    appName = "ПК МС",
                    reqId = Guid.NewGuid().ToString(),
                    countOnPage = countOnPage.Value,
                    parentGuid = addressId
                };

                gisResponse = PerformGisRequest<GisAddressChildrenSearchResponse, GisAddressChildrenSearchRequest>(_childrenUrl, gisRequest);
            }

            return gisResponse;
        }

        // Получить список возможных значений элемента адреса
        public GisAddressPart[] GetAddressParts(string address, GisAddressLevel[] level, string parentId = "")
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
                return new GisAddressPart[0];

            IDictionary<Guid, GisAddressPart> gisAddressParts = new Dictionary<Guid, GisAddressPart>();

            // Если ищем регион
            if (level.Contains(GisAddressLevel.Region))
            {
                GisAddressSearchResponse gisAddressResponse;
                try
                {
                    gisAddressResponse = GetAddress(address);
                }
                catch (GisSearchException)
                {
                    throw;
                }

                if (!string.IsNullOrEmpty(gisAddressResponse.errorCode) && gisAddressResponse.addr?.Count == 0)
                {
                    throw new GisSearchException($"Не удалось получить адреса. ErrorCode: {gisAddressResponse.errorCode}");
                }

                foreach (var gisAddress in gisAddressResponse.addr)
                {
                    var addressPart = GetAddressPart(gisAddress, level);

                    // Если элемент адреса уже не присутствует в коллекции, начинается с запрашиваемого названия (address), то добавляем элемент в коллекцию
                    if (addressPart != null &&
                        !gisAddressParts.ContainsKey(addressPart.fiasId) &&
                        addressPart.GetFullName().ToLower().StartsWith(address.ToLower()))
                    {
                        gisAddressParts.Add(addressPart.fiasId, addressPart);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(parentId))
            {
                var gisResponse = GetAddressChildren(parentId, null, address);

                if (!string.IsNullOrEmpty(gisResponse.errorCode) && gisResponse.children?.Count == 0)
                {
                    throw new GisSearchException($"Не удалось получить адреса. ErrorCode: {gisResponse.errorCode}");
                }

                foreach (var gisAddress in gisResponse.children)
                {
                    var addressPart = GetAddressPart(gisAddress, level);

                    if (addressPart != null && 
                        !gisAddressParts.ContainsKey(addressPart.fiasId) &&
                        addressPart.GetFullName().StartsWith(address, StringComparison.CurrentCultureIgnoreCase))
                    {
                        gisAddressParts.Add(addressPart.fiasId, addressPart);
                    }
                }
            }
            else
            {
                return new GisAddressPart[0];
            }

            return gisAddressParts.Values.ToArray();
        }

        public GisAddressPart SearchPostalCodeElement(string postalCode)
        {
            var request = new GisAddressSearchRequest
            {
                appName = "ПК МС",
                reqId = Guid.NewGuid().ToString(),
                addressType = 1,
                rmFedCities = true,
                addr = postalCode
            };

            var response = PerformGisRequest<GisAddressSearchResponse, GisAddressSearchRequest>(_mainUrl, request);

            var addresses = new List<GisAddressPart>(response.addr.Count);

            // Словарь выполняет роль дерева, в котором соединены узлы элементов адресов
            // (в качестве ключа используется предок, а в качестве значения все его потомки)
            var distinctAddresses = new Dictionary<Guid, HashSet<Guid>>();
            GisAddressPart tempAddress;

            foreach (var addr in response.addr)
            {
                try
                {
                    addresses.Add(GetAddressPart(addr, null));
                }
                catch (FormatException)
                {
                    continue;
                }

                tempAddress = addresses.Last();

                while (tempAddress.parent != null)
                {
                    if (distinctAddresses.ContainsKey(tempAddress.parent.id))
                    {
                        var children = distinctAddresses[tempAddress.parent.id];
                        if (!children.Contains(tempAddress.id))
                        {
                            children.Add(tempAddress.id);
                        }
                    }
                    else
                    {
                        distinctAddresses.Add(tempAddress.parent.id, new HashSet<Guid> { tempAddress.id });
                    }
                    tempAddress = tempAddress.parent;
                }
            }

            if (addresses.Count == 0)
            {
                throw new GisSearchException("Не удалось получить адрес");
            }

            // Ищем элемент адреса с наибольшим уровнем (корень дерева)
            tempAddress = addresses[0];
            while (tempAddress.parent != null)
            {
                tempAddress = tempAddress.parent;
            }

            var tempGuid = tempAddress.id;
            while (distinctAddresses.ContainsKey(tempGuid) && distinctAddresses[tempGuid].Count == 1)
            {
                tempGuid = distinctAddresses[tempGuid].Single();
            }

            GisAddressPart result = null;
            foreach (var address in addresses)
            {
                result = address.StartsWith(tempGuid);
                if (result != null)
                    break;
            }
            return result;
        }

        /// <summary>
        /// Возвращает адрес в формате КЛАДР
        /// </summary>
        /// <param name="addressId">Идентификатор адреса в ФИАС</param>
        /// <param name="getRegionCode">Функция, возвращающая код региона по его идентификатору</param>
        /// <returns></returns>
        public FiasCode.KladrAddress GetKladrAddressByElements(string addressId, Func<string, string> getRegionCode = null)
        {
            var kladrFiasDict = new Dictionary<string, FiasAddressLevel>();
            kladrFiasDict.Add(nameof(KladrAddress.District),  FiasAddressLevel.District);
            kladrFiasDict.Add(nameof(KladrAddress.City),  FiasAddressLevel.City);
            kladrFiasDict.Add(nameof(KladrAddress.Locality),  FiasAddressLevel.IntraArea | FiasAddressLevel.Locality);
            kladrFiasDict.Add(nameof(KladrAddress.Street),  FiasAddressLevel.Street | FiasAddressLevel.PlanStructure);

            var address = GetAddressById(addressId);
            var addressPart = GetAddressPart(address, null);

            var addressParts = addressPart.GetAllParts();

            #region Index

            string index = null;
            if (!string.IsNullOrEmpty(address.index))
            {
                // Уровни адреса, у которых не должен считываться почтовый индекс
                var levelsWithoutIndex = new HashSet<FiasAddressLevel>
                {
                    FiasAddressLevel.Region,
                    FiasAddressLevel.City
                };

                if (!levelsWithoutIndex.Contains(addressPart.level))
                    index = address.index;
            }

            #endregion

            #region RegionCode

            string regionCode = null;
            if (getRegionCode != null)
            {
                try
                {
                    var regionId = GetAddressPart(address, GisAddressLevel.Region).id.ToString();
                    regionCode = getRegionCode?.Invoke(regionId);
                }
                catch { }
            }

            #endregion

            #region District

            var district = addressParts.FirstOrDefault(part => (part.level & kladrFiasDict[nameof(KladrAddress.District)]) == part.level)?.name.ToUpper();

            #endregion

            #region City

            var city = addressParts.FirstOrDefault(part => (part.level & kladrFiasDict[nameof(KladrAddress.City)]) == part.level)?.name.ToUpper();

            #endregion

            #region Locality

            var locality = addressParts.FirstOrDefault(part => (part.level & kladrFiasDict[nameof(KladrAddress.Locality)]) == part.level)?.name.ToUpper();

            #endregion

            #region Street

            var street = addressParts.FirstOrDefault(part => (part.level & kladrFiasDict[nameof(KladrAddress.Street)]) == part.level)?.name.ToUpper();

            #endregion

            #region House

            var house = addressParts.FirstOrDefault(part => part.level.Equals(FiasAddressLevel.House))?.name;

            if (!string.IsNullOrEmpty(house))
            {
                // Если элемент является владением или домовладением
                if (house.ToLower().Contains("влд"))
                {
                    house = house.Replace(". ", "");
                }

                house = house.ToUpper();
            }

            #endregion

            #region Building

            var currentHouse = addressParts.FirstOrDefault(part => part.level.Equals(FiasAddressLevel.House));

            string building = null;
            if (currentHouse != null)
            {
                building = string.IsNullOrEmpty(currentHouse.housings) ? string.Empty : currentHouse.housings;
                building += string.IsNullOrEmpty(currentHouse.structure) ? string.Empty : $"СТР{currentHouse.structure}";
            }

            #endregion


            var kladr = new KladrAddress
            {
                AddressString = null,
                CountryCode = "643",
                Index = index,
                RegionCode = regionCode,
                District = district,
                City = city,
                Locality = locality,
                Street = street,
                House = house,
                Building = building
            };

            kladr.AddressString = string.Concat(
                string.IsNullOrEmpty(kladr.CountryCode) ? string.Empty : kladr.CountryCode, ",",
                string.IsNullOrEmpty(kladr.Index) ? string.Empty : kladr.Index, ",",
                string.IsNullOrEmpty(kladr.RegionCode) ? string.Empty : kladr.RegionCode, ",",
                string.IsNullOrEmpty(kladr.District) ? string.Empty : kladr.District, ",",
                string.IsNullOrEmpty(kladr.City) ? string.Empty : kladr.City, ",",
                string.IsNullOrEmpty(kladr.Locality) ? string.Empty : kladr.Locality, ",",
                string.IsNullOrEmpty(kladr.Street) ? string.Empty : kladr.Street, ",",
                string.IsNullOrEmpty(kladr.House) ? string.Empty : kladr.House, ",",
                string.IsNullOrEmpty(kladr.Building) ? string.Empty : kladr.Building, ","
                );

            return kladr;
        }


        private GisAddress GetAddressById(string addressId)
        {
            try
            {
                new Guid(addressId);
            }
            catch
            {
                throw;
            }

            var gisRequest = new GisAddressSearchRequest
            {
                appName = "ПК МС",
                reqId = Guid.NewGuid().ToString(),
                addressType = 1,
                rmFedCities = true,
                guid = addressId,
                addr = null
            };

            var gisResponse = PerformGisRequest<GisAddressSearchResponse, GisAddressSearchRequest>(_mainUrl, gisRequest);

            return gisResponse.addr[0];
        }

        private GisAddressPart GetAddressPart(GisAddress address, params GisAddressLevel[] level)
        {
            int[] intLevels = level?.Select(l => (int)l).ToArray();
            int indexOfElement = -1;
            for (int i = 0; i < address.elements.Count; i++)
            {
                // Определяем индекс элемента адреса, уровень которого совпадает с любым уровнем level
                if (intLevels?.Contains(address.elements[i].level) ?? false)
                {
                    indexOfElement = i;
                    break;
                }
            }
            if (level == null) indexOfElement = address.elements.Count - 1;
            else if (indexOfElement == -1) return null;

            var addressPart = new GisAddressPart();
            addressPart.FromAddress(address, indexOfElement);

            return addressPart;
        }
    }

    public class GisAddressSearchRequest
    {
        /// <summary>
        /// Имя и версия Сервиса Подсказок
        /// </summary>
        public string appName { get; set; }
        public string reqId { get; set; }
        /// <summary>
        /// Из итоговой строки адреса, удалять дублирующиеся название городов федерального значения 
        /// </summary>
        public bool rmFedCities { get; set; }
        public string addr { get; set; }
        public int addressType { get; set; }

        public string outLang { get; set; }
        public bool outOriginalEng { get; set; }
        public List<string> searchRange { get; set; }
        public List<string> missElements { get; set; }
        public List<int> searchLevelRange { get; set; }
        public List<int> missLevels { get; set; }
        public List<string> guidsRestr { get; set; }
        public List<string> indexRestr { get; set; }
        public bool woFlat { get; set; }
        public bool withPhantomFlats { get; set; }
        public string guid { get; set; }
        public int origin { get; set; } = 1;
        public int historical { get; set; } = 1;
        public List<string> geoProperties { get; set; }

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

        public int firstElemNum { get; set; }
        public string childGuid { get; set; }
        public string stringSrc { get; set; }
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
        public string reqId { get; set; }
        public int firstElemNum { get; set; }
        public string errorCode { get; set; }
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

        //public FiasAddress ToFiasAddress()
        //{
        //    new FiasAddress()
        //    {
        //        ClassificationCode = null,
        //        Housings = null,
        //        Id = null,
        //        IdAddressElement = null,
        //        IdRequest = null,
        //        IntCode = null,
        //        Level = null,
        //        Name = null,
        //        OKATO = null,
        //        OKTMO = null,
        //        OwningCode = null,
        //        Parent = null,
        //        PostCode = null,
        //        ShortName = null,
        //        SortOrder = null,
        //        Structure = null,
        //        StructureCode = null,
        //        Type = null,
        //        TypeCode = null
        //    }
        //}
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

    public class GisAddressPart
    {
        public Guid fiasId;
        // id дублирует fiasId, поскольку у ГИС ПА в отличие от ФИАС нет
        // идентификатора элемента адреса, но в ССД он используется
        public Guid id;

        public FiasAddressLevel level;

        public string name;
        public string index;

        public string structure;
        public string housings;

        public GisAddressPart parent;

        public GisAddressPart()
        {
            fiasId = new Guid();
            level = 0;
            name = string.Empty;
            index = null;
            structure = null;
            housings = null;
            parent = null;
        }

        public string GetFullName()
        {
            if (level == FiasAddressLevel.House)
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

        public string GetFullAddress()
        {
            var address = parent == null ? string.Empty : parent.GetFullAddress();

            address += $", {GetFullName()}";

            return address;
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
                parent = new GisAddressPart();
                parent.FromAddress(address, indexOfElement - 1);
            }
        }

        // Возвращает часть адреса, fiasId которой равен addressId, и всех предков
        public GisAddressPart StartsWith(Guid addressId)
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

        public GisAddressPart[] GetAllParts()
        {
            if (parent == null)
            {
                return new GisAddressPart[] { this };
            }
            else
            {
                var addressParts = parent.GetAllParts();
                var newAddressParts = new GisAddressPart[addressParts.Length + 1];

                addressParts.CopyTo(newAddressParts, 0);
                newAddressParts[newAddressParts.Length - 1] = this;

                return newAddressParts;
            }
        }

        //static public void writeTo(GisAddressPart gisAddressPart, TextWriter writer)
        //{

        //    writer.Write("{\"id\":\"" + gisAddressPart.id + "\"");

        //    writer.Write(",\"fiasId\":\"" + gisAddressPart.fiasId + "\"");

        //    writer.Write(",\"level\":");
        //    writer.Write((int)gisAddressPart.level);

        //    writer.Write(",\"name\":\"");
        //    writer.Write(gisAddressPart.name != null ? JSONUtil.EscapeString(gisAddressPart.name) : "");
        //    writer.Write("\"");


        //    writer.Write(",\"index\":");
        //    if (gisAddressPart.index != null)
        //    {
        //        writer.Write("\"" + gisAddressPart.index + "\"");
        //    }
        //    else
        //    {
        //        writer.Write("null");
        //    }

        //    writer.Write(",\"parent\":");
        //    if (gisAddressPart.parent != null)
        //    {
        //        writeTo(gisAddressPart.parent, writer);
        //    }
        //    else
        //    {
        //        writer.Write("null");
        //    }

        //    if (gisAddressPart.level == FiasAddressLevel.House)
        //    {

        //        writer.Write(",\"structure\":");
        //        if (gisAddressPart.structure != null)
        //        {
        //            writer.Write("\"" + gisAddressPart.structure + "\"");
        //        }
        //        else
        //        {
        //            writer.Write("null");
        //        }

        //        writer.Write(",\"housings\":");
        //        if (gisAddressPart.housings != null)
        //        {
        //            writer.Write("\"" + gisAddressPart.housings + "\"");
        //        }
        //        else
        //        {
        //            writer.Write("null");
        //        }
        //    }

        //    writer.Write("}");
        //}

        public override bool Equals(object obj)
        {
            if (obj is GisAddressPart)
            {
                var address = (GisAddressPart)obj;
                bool isEqual = fiasId.Equals(address.fiasId) &&
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

    public class GisPostalCodeSearchRequest
    {
        string query { get; set; }
        int limit { get; set; }
        string fromBound { get; set; }

        public GisPostalCodeSearchRequest(string postalCode)
        {
            query = postalCode;
            limit = 10;
            fromBound = "CITY";
        }

        public GisPostalCodeSearchRequest(string postalCode, int limit)
        {
            query = postalCode;
            this.limit = limit;
            fromBound = "CITY";
        }

        public GisPostalCodeSearchRequest(string postalCode, string bound)
        {
            query = postalCode;
            limit = 10;
            fromBound = bound;
        }

        public GisPostalCodeSearchRequest(string postalCode, int limit, string bound)
        {
            query = postalCode;
            this.limit = limit;
            fromBound = bound;
        }
    }

    public class GisSearchException : Exception
    {
        public GisSearchException(string message) : base(message)
        {
        }
    }
}

namespace FiasCode
{
    public partial class FiasAddress
    {

        private string classificationCodeField;

        private string housingsField;

        private string idField;

        private int idAddressElementField;

        private bool idAddressElementFieldSpecified;

        private System.Nullable<int> idRequestField;

        private bool idRequestFieldSpecified;

        private int intCodeField;

        private bool intCodeFieldSpecified;

        private System.Nullable<FiasAddressLevel> levelField;

        private bool levelFieldSpecified;

        private string nameField;

        private string oKATOField;

        private string oKTMOField;

        private System.Nullable<int> owningCodeField;

        private bool owningCodeFieldSpecified;

        private System.Nullable<int> parentField;

        private bool parentFieldSpecified;

        private string postCodeField;

        private string shortNameField;

        private System.Nullable<short> sortOrderField;

        private bool sortOrderFieldSpecified;

        private string structureField;

        private System.Nullable<int> structureCodeField;

        private bool structureCodeFieldSpecified;

        private string typeField;

        private System.Nullable<int> typeCodeField;

        private bool typeCodeFieldSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 0)]
        public string ClassificationCode
        {
            get
            {
                return this.classificationCodeField;
            }
            set
            {
                this.classificationCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 1)]
        public string Housings
        {
            get
            {
                return this.housingsField;
            }
            set
            {
                this.housingsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 2)]
        public string Id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public int IdAddressElement
        {
            get
            {
                return this.idAddressElementField;
            }
            set
            {
                this.idAddressElementField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool IdAddressElementSpecified
        {
            get
            {
                return this.idAddressElementFieldSpecified;
            }
            set
            {
                this.idAddressElementFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 4)]
        public System.Nullable<int> IdRequest
        {
            get
            {
                return this.idRequestField;
            }
            set
            {
                this.idRequestField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool IdRequestSpecified
        {
            get
            {
                return this.idRequestFieldSpecified;
            }
            set
            {
                this.idRequestFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public int IntCode
        {
            get
            {
                return this.intCodeField;
            }
            set
            {
                this.intCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool IntCodeSpecified
        {
            get
            {
                return this.intCodeFieldSpecified;
            }
            set
            {
                this.intCodeFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 6)]
        public System.Nullable<FiasAddressLevel> Level
        {
            get
            {
                return this.levelField;
            }
            set
            {
                this.levelField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool LevelSpecified
        {
            get
            {
                return this.levelFieldSpecified;
            }
            set
            {
                this.levelFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 7)]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 8)]
        public string OKATO
        {
            get
            {
                return this.oKATOField;
            }
            set
            {
                this.oKATOField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 9)]
        public string OKTMO
        {
            get
            {
                return this.oKTMOField;
            }
            set
            {
                this.oKTMOField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 10)]
        public System.Nullable<int> OwningCode
        {
            get
            {
                return this.owningCodeField;
            }
            set
            {
                this.owningCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool OwningCodeSpecified
        {
            get
            {
                return this.owningCodeFieldSpecified;
            }
            set
            {
                this.owningCodeFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 11)]
        public System.Nullable<int> Parent
        {
            get
            {
                return this.parentField;
            }
            set
            {
                this.parentField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ParentSpecified
        {
            get
            {
                return this.parentFieldSpecified;
            }
            set
            {
                this.parentFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 12)]
        public string PostCode
        {
            get
            {
                return this.postCodeField;
            }
            set
            {
                this.postCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 13)]
        public string ShortName
        {
            get
            {
                return this.shortNameField;
            }
            set
            {
                this.shortNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 14)]
        public System.Nullable<short> SortOrder
        {
            get
            {
                return this.sortOrderField;
            }
            set
            {
                this.sortOrderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool SortOrderSpecified
        {
            get
            {
                return this.sortOrderFieldSpecified;
            }
            set
            {
                this.sortOrderFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 15)]
        public string Structure
        {
            get
            {
                return this.structureField;
            }
            set
            {
                this.structureField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 16)]
        public System.Nullable<int> StructureCode
        {
            get
            {
                return this.structureCodeField;
            }
            set
            {
                this.structureCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool StructureCodeSpecified
        {
            get
            {
                return this.structureCodeFieldSpecified;
            }
            set
            {
                this.structureCodeFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 17)]
        public string Type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 18)]
        public System.Nullable<int> TypeCode
        {
            get
            {
                return this.typeCodeField;
            }
            set
            {
                this.typeCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool TypeCodeSpecified
        {
            get
            {
                return this.typeCodeFieldSpecified;
            }
            set
            {
                this.typeCodeFieldSpecified = value;
            }
        }
    }

    public partial class KladrAddress
    {

        private string addressStringField;

        private string buildingField;

        private string cityField;

        private string countryCodeField;

        private string districtField;

        private string houseField;

        private string indexField;

        private string localityField;

        private string regionCodeField;

        private string streetField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 0)]
        public string AddressString
        {
            get
            {
                return this.addressStringField;
            }
            set
            {
                this.addressStringField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 1)]
        public string Building
        {
            get
            {
                return this.buildingField;
            }
            set
            {
                this.buildingField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 2)]
        public string City
        {
            get
            {
                return this.cityField;
            }
            set
            {
                this.cityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 3)]
        public string CountryCode
        {
            get
            {
                return this.countryCodeField;
            }
            set
            {
                this.countryCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 4)]
        public string District
        {
            get
            {
                return this.districtField;
            }
            set
            {
                this.districtField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 5)]
        public string House
        {
            get
            {
                return this.houseField;
            }
            set
            {
                this.houseField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 6)]
        public string Index
        {
            get
            {
                return this.indexField;
            }
            set
            {
                this.indexField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 7)]
        public string Locality
        {
            get
            {
                return this.localityField;
            }
            set
            {
                this.localityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 8)]
        public string RegionCode
        {
            get
            {
                return this.regionCodeField;
            }
            set
            {
                this.regionCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 9)]
        public string Street
        {
            get
            {
                return this.streetField;
            }
            set
            {
                this.streetField = value;
            }
        }
    }
}
