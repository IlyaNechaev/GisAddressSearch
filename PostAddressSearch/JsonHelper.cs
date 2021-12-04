using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GisAddressSearch
{
	public class JsonHelper
	{
		public static string Serialize<T>(T obj)
		{
			var serializer = new DataContractJsonSerializer(obj.GetType());
			string result = null;
			using (var ms = new MemoryStream())
			{
				serializer.WriteObject(ms, obj);
				result = Encoding.UTF8.GetString(ms.ToArray());
			}
			return result;
		}

		public static T Deserialize<T>(string json) where T : class
		{
			T result = null;
			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
			{
				var serializer = new DataContractJsonSerializer(typeof(T));
				result = (T)serializer.ReadObject(ms);
				ms.Close();
			}
			return result;
		}
	}
}
