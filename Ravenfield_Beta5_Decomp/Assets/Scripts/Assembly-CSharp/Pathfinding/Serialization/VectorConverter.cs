using System;
using System.Collections.Generic;
using Pathfinding.Serialization.JsonFx;
using UnityEngine;

namespace Pathfinding.Serialization
{
	public class VectorConverter : JsonConverter
	{
		public override bool CanConvert(Type type)
		{
			return object.Equals(type, typeof(Vector2)) || object.Equals(type, typeof(Vector3)) || object.Equals(type, typeof(Vector4));
		}

		public override object ReadJson(Type type, Dictionary<string, object> values)
		{
			if (object.Equals(type, typeof(Vector2)))
			{
				return new Vector2(CastFloat(values["x"]), CastFloat(values["y"]));
			}
			if (object.Equals(type, typeof(Vector3)))
			{
				return new Vector3(CastFloat(values["x"]), CastFloat(values["y"]), CastFloat(values["z"]));
			}
			if (object.Equals(type, typeof(Vector4)))
			{
				return new Vector4(CastFloat(values["x"]), CastFloat(values["y"]), CastFloat(values["z"]), CastFloat(values["w"]));
			}
			throw new NotImplementedException("Can only read Vector2,3,4. Not objects of type " + type);
		}

		public override Dictionary<string, object> WriteJson(Type type, object value)
		{
			if (object.Equals(type, typeof(Vector2)))
			{
				Vector2 vector = (Vector2)value;
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary.Add("x", vector.x);
				dictionary.Add("y", vector.y);
				return dictionary;
			}
			if (object.Equals(type, typeof(Vector3)))
			{
				Vector3 vector2 = (Vector3)value;
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary.Add("x", vector2.x);
				dictionary.Add("y", vector2.y);
				dictionary.Add("z", vector2.z);
				return dictionary;
			}
			if (object.Equals(type, typeof(Vector4)))
			{
				Vector4 vector3 = (Vector4)value;
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary.Add("x", vector3.x);
				dictionary.Add("y", vector3.y);
				dictionary.Add("z", vector3.z);
				dictionary.Add("w", vector3.w);
				return dictionary;
			}
			throw new NotImplementedException("Can only write Vector2,3,4. Not objects of type " + type);
		}
	}
}
