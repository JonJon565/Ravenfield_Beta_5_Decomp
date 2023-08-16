using System;
using System.Collections.Generic;
using Pathfinding.Serialization.JsonFx;
using UnityEngine;

namespace Pathfinding.Serialization
{
	public class LayerMaskConverter : JsonConverter
	{
		public override bool CanConvert(Type type)
		{
			return object.Equals(type, typeof(LayerMask));
		}

		public override object ReadJson(Type type, Dictionary<string, object> values)
		{
			return (LayerMask)(int)values["value"];
		}

		public override Dictionary<string, object> WriteJson(Type type, object value)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary.Add("value", ((LayerMask)value).value);
			return dictionary;
		}
	}
}
