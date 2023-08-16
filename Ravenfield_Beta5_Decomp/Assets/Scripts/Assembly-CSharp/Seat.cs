using System;
using UnityEngine;

public class Seat : MonoBehaviour
{
	public enum SitAnimation
	{
		Chair = 0,
		Quad = 1
	}

	public enum Type
	{
		Driver = 0,
		Pilot = 1,
		Gunner = 2,
		Passenger = 3
	}

	public const int LAYER = 11;

	public Vehicle vehicle;

	public Type type = Type.Passenger;

	public SitAnimation animation;

	public bool enclosed;

	public Vector3 exitOffset = Vector3.zero;

	public MountedWeapon weapon;

	[NonSerialized]
	public Actor occupant;

	public GameObject hud;

	public float maxOccupantBalance = 200f;

	public bool IsOccupied()
	{
		return occupant != null;
	}

	public void SetOccupant(Actor actor)
	{
		occupant = actor;
		if (HasMountedWeapon())
		{
			weapon.user = occupant;
		}
		if (!occupant.aiControlled && hud != null)
		{
			hud.SetActive(true);
		}
		vehicle.OccupantEntered(this);
	}

	public void OccupantLeft()
	{
		Actor leaver = occupant;
		occupant = null;
		if (HasMountedWeapon())
		{
			weapon.StopFire();
			weapon.user = null;
		}
		if (hud != null)
		{
			hud.SetActive(false);
		}
		vehicle.OccupantLeft(this, leaver);
	}

	private void Update()
	{
		if (IsOccupied())
		{
			occupant.balance = Mathf.Min(occupant.balance, maxOccupantBalance);
		}
	}

	public bool CanUseWeapon()
	{
		return type == Type.Passenger;
	}

	public bool HasMountedWeapon()
	{
		return weapon != null;
	}
}
