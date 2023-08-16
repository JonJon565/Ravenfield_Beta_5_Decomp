using UnityEngine;

namespace InternalRealtimeCSG
{
	public struct MeshInstanceKey
	{
		public SurfaceFlags surfaceFlags;

		public int renderMaterialInstanceID;

		public int physicsMaterialInstanceID;

		private MeshInstanceKey(SurfaceFlags _surfaceFlags, int _renderMaterialInstanceID, int _physicsMaterialInstanceID)
		{
			surfaceFlags = _surfaceFlags;
			renderMaterialInstanceID = _renderMaterialInstanceID;
			physicsMaterialInstanceID = _physicsMaterialInstanceID;
		}

		public static MeshInstanceKey GenerateKey(SurfaceFlags surfaceFlags, Material renderMaterial, PhysicMaterial physicsMaterial)
		{
			Material material = renderMaterial;
			if ((surfaceFlags & SurfaceFlags.DisableRenderable) != 0)
			{
				material = null;
			}
			PhysicMaterial physicMaterial = physicsMaterial;
			if ((surfaceFlags & SurfaceFlags.DisableCollidable) != 0)
			{
				physicMaterial = null;
			}
			return new MeshInstanceKey(surfaceFlags, (material == null) ? 1 : material.GetInstanceID(), (physicMaterial == null) ? 1 : physicMaterial.GetInstanceID());
		}

		public override int GetHashCode()
		{
			int hashCode = surfaceFlags.GetHashCode();
			int hashCode2 = renderMaterialInstanceID.GetHashCode();
			int hashCode3 = physicsMaterialInstanceID.GetHashCode();
			int num = hashCode;
			num *= 389 + hashCode2;
			num *= 397 + hashCode3;
			return num + (hashCode ^ hashCode2 ^ hashCode3) + (hashCode + hashCode2 + hashCode3) + hashCode * hashCode2 * hashCode3;
		}
	}
}
