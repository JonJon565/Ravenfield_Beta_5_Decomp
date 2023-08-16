using System;
using System.Collections.Generic;
using Pathfinding.Serialization.JsonFx;
using UnityEngine;

namespace Pathfinding.Serialization
{
	public class BoundsConverter : JsonConverter
	{
		public override bool CanConvert(Type type)
		{
			return object.Equals(type, typeof(Bounds));
		}

		public override object ReadJson(Type objectType, Dictionary<string, object> values)
		{
			Bounds bounds = default(Bounds);
			bounds.center = new Vector3(CastFloat(values["cx"]), CastFloat(values["cy"]), CastFloat(values["cz"]));
			bounds.extents = new Vector3(CastFloat(values["ex"]), CastFloat(values["ey"]), CastFloat(values["ez"]));
			return bounds;
		}

		public override Dictionary<string, object> WriteJson(Type type, object value)
		{
			Bounds bounds = (Bounds)value;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary.Add("cx", bounds.center.x);
			dictionary.Add("cy", bounds.center.y);
			dictionary.Add("cz", bounds.center.z);
			dictionary.Add("ex", bounds.extents.x);
			dictionary.Add("ey", bounds.extents.y);
			dictionary.Add("ez", bounds.extents.z);
			return dictionary;
		}
	}
}
