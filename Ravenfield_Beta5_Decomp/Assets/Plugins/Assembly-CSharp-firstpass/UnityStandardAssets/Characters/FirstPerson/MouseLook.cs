using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Characters.FirstPerson
{
	[Serializable]
	public class MouseLook
	{
		private const float ROTATION_PADDING = 3f;

		public static bool paused;

		public float XSensitivity = 2f;

		public float YSensitivity = 2f;

		public float sensitivityMultiplier = 1f;

		public bool invertY;

		public bool clampVerticalRotation = true;

		public float MinimumX = -90f;

		public float MaximumX = 90f;

		public bool smooth;

		public float smoothTime = 5f;

		[NonSerialized]
		public bool enabled = true;

		private Quaternion m_CharacterTargetRot;

		private Quaternion m_CameraTargetRot;

		public void Init(Transform character, Transform camera)
		{
			m_CharacterTargetRot = character.localRotation;
			m_CameraTargetRot = camera.localRotation;
		}

		public void LookRotation(Transform character, Transform camera)
		{
			if (!enabled || paused)
			{
				return;
			}
			float num = ((!invertY) ? 1f : (-1f));
			float y = CrossPlatformInputManager.GetAxis("Mouse X") * XSensitivity * sensitivityMultiplier;
			float num2 = CrossPlatformInputManager.GetAxis("Mouse Y") * YSensitivity * sensitivityMultiplier * num;
			m_CameraTargetRot = camera.localRotation;
			Vector3 euler = m_CameraTargetRot.eulerAngles + new Vector3(0f - num2, y, 0f);
			if (clampVerticalRotation)
			{
				if (euler.x > 180f && euler.x < 273f)
				{
					euler.x = 273f;
				}
				else if (euler.x < 180f && euler.x > 87f)
				{
					euler.x = 87f;
				}
			}
			m_CameraTargetRot = Quaternion.Euler(euler);
			if (smooth)
			{
				camera.localRotation = Quaternion.Slerp(camera.localRotation, m_CameraTargetRot, smoothTime * Time.deltaTime);
			}
			else
			{
				camera.localRotation = m_CameraTargetRot;
			}
		}

		private Quaternion ClampRotationAroundXAxis(Quaternion q)
		{
			q.x /= q.w;
			q.y /= q.w;
			q.z /= q.w;
			q.w = 1f;
			float value = 114.59156f * Mathf.Atan(q.x);
			value = Mathf.Clamp(value, MinimumX, MaximumX);
			q.x = Mathf.Tan((float)Math.PI / 360f * value);
			return q;
		}
	}
}
