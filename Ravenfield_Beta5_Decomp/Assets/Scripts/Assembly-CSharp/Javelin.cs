using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Javelin : ScopedWeapon
{
	private const float MAX_DISTANCE = 1000f;

	private const int TARGET_LOS_MASK = 1;

	private const int TARGET_LAYER_MASK = 5377;

	private const float LOCK_ON_DOT = 0.99f;

	public Transform pointSampler;

	public RawImage lockImage;

	public RawImage targetImage;

	public Texture2D lockingTexture;

	public Texture2D lockedTexture;

	public Renderer crosshair;

	private bool hasManualTarget;

	private bool lockingOnManualTarget;

	private Vehicle target;

	private Vector3 manualTargetPoint;

	private Action lockOnAction = new Action(2f);

	private Action lockOnStayAction = new Action(1f);

	public override void Unholster()
	{
		base.Unholster();
		hasManualTarget = false;
		if (ammo == 0)
		{
			ReloadDone();
		}
	}

	protected override Projectile SpawnProjectile(Vector3 direction)
	{
		Projectile projectile = base.SpawnProjectile(direction);
		JavelinMissile javelinMissile = (JavelinMissile)projectile;
		Ray ray = new Ray(pointSampler.position, pointSampler.forward);
		if (hasManualTarget)
		{
			javelinMissile.targetPoint = manualTargetPoint;
		}
		else
		{
			javelinMissile.target = target.transform;
			if (target.HasDriver() && target.directJavelinPath)
			{
				javelinMissile.ForceDirectMode();
			}
		}
		return projectile;
	}

	public override void SetAiming(bool aiming)
	{
		base.SetAiming(aiming);
		if (aiming)
		{
			hasManualTarget = false;
		}
	}

	private void LateUpdate()
	{
		if (aiming)
		{
			bool flag = lockingOnManualTarget;
			lockingOnManualTarget = hasManualTarget && (IsInFov(manualTargetPoint) || !lockOnStayAction.TrueDone());
			Vehicle vehicle = null;
			bool flag2;
			if (lockingOnManualTarget)
			{
				flag2 = !flag && lockingOnManualTarget;
				if (flag2)
				{
					lockOnAction.Start();
				}
				target = null;
			}
			else
			{
				vehicle = FindTarget();
				flag2 = vehicle != target;
			}
			if (hasManualTarget)
			{
				if (IsInFov(manualTargetPoint))
				{
					lockOnStayAction.Start();
				}
			}
			else if (!flag2 && IsLocking() && HasLock())
			{
				lockOnStayAction.Start();
			}
			if (flag2 && lockOnStayAction.TrueDone())
			{
				if (!lockingOnManualTarget)
				{
					target = vehicle;
				}
				if (IsLocking())
				{
					lockOnAction.Start();
				}
				else
				{
					lockOnAction.Stop();
				}
			}
		}
		else
		{
			target = null;
			lockingOnManualTarget = false;
			lockOnAction.Stop();
		}
		if (!(user != null) || user.aiControlled)
		{
			return;
		}
		if (IsLocking())
		{
			lockImage.enabled = true;
			if (hasManualTarget)
			{
				lockImage.rectTransform.position = Camera.main.WorldToScreenPoint(manualTargetPoint);
			}
			else
			{
				lockImage.rectTransform.position = Camera.main.WorldToScreenPoint(target.transform.position);
			}
		}
		else
		{
			lockImage.enabled = false;
		}
		crosshair.enabled = !HasLock() && !hasManualTarget;
		if (aiming && hasManualTarget)
		{
			targetImage.enabled = true;
			targetImage.rectTransform.position = Camera.main.WorldToScreenPoint(manualTargetPoint);
		}
		else
		{
			targetImage.enabled = false;
		}
		if (HasLock())
		{
			lockImage.texture = lockedTexture;
		}
		else
		{
			lockImage.texture = lockingTexture;
		}
	}

	public override void Fire(Vector3 direction, bool useMuzzleDirection)
	{
		if (HasLock())
		{
			base.Fire(direction, useMuzzleDirection);
			Reload();
		}
		else if (!hasManualTarget)
		{
			Ray ray = new Ray(pointSampler.position, pointSampler.forward);
			RaycastHit hitInfo;
			if (Physics.Raycast(ray, out hitInfo, 1000f, 1))
			{
				manualTargetPoint = hitInfo.point;
				hasManualTarget = true;
			}
		}
	}

	private bool IsLocking()
	{
		return target != null || lockingOnManualTarget;
	}

	private bool HasLock()
	{
		return IsLocking() && lockOnAction.Done();
	}

	private Vehicle FindTarget()
	{
		List<Vehicle> sortedTargets = GetSortedTargets();
		foreach (Vehicle item in sortedTargets)
		{
			Vector3 direction = item.transform.position - base.transform.position;
			if (IsInFov(item.transform.position))
			{
				Ray ray = new Ray(MuzzlePosition(), direction);
				if (!Physics.Raycast(ray, direction.magnitude, 1))
				{
					return item;
				}
			}
		}
		return null;
	}

	private bool IsInFov(Vector3 point)
	{
		return Vector3.Dot((point - base.transform.position).normalized, pointSampler.forward) > 0.99f;
	}

	private List<Vehicle> GetSortedTargets()
	{
		List<Vehicle> list = new List<Vehicle>(ActorManager.instance.vehicles);
		if (user.IsSeated())
		{
			list.Remove(user.seat.vehicle);
		}
		Dictionary<Vehicle, bool> isEnemy = new Dictionary<Vehicle, bool>();
		foreach (Vehicle item in list)
		{
			if (item.HasDriver())
			{
				isEnemy.Add(item, item.Driver().team != user.team);
			}
			else
			{
				isEnemy.Add(item, false);
			}
		}
		list.Sort((Vehicle x, Vehicle y) => (isEnemy[x] != isEnemy[y]) ? isEnemy[y].CompareTo(isEnemy[x]) : Vector3.Distance(base.transform.position, x.transform.position).CompareTo(Vector3.Distance(base.transform.position, y.transform.position)));
		return list;
	}

	public override bool CanBeAimed()
	{
		return base.CanBeAimed() && HasLoadedAmmo();
	}
}
