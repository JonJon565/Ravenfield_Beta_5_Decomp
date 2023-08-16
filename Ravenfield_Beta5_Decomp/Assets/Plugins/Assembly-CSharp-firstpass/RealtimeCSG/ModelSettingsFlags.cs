using System;

namespace RealtimeCSG
{
	[Serializable]
	[Flags]
	public enum ModelSettingsFlags
	{
		ShadowCastingModeFlags = 7,
		ReceiveShadows = 8,
		DoNotRender = 0x10,
		NoCollider = 0x20,
		IsTrigger = 0x40,
		InvertedWorld = 0x80,
		SetColliderConvex = 0x100,
		SelfGenerateRigidBody = 0x200
	}
}
