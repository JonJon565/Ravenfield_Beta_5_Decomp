using UnityEngine;
using UnityEngine.UI;

public class Binoculars : ScopedWeapon
{
	private int lastRangeReading;

	private Action findRangeAction = new Action(0.3f);

	public GameObject effectPrefab;

	public Canvas rangeCanvas;

	public Text rangeText;

	protected override void Update()
	{
		base.Update();
		if (findRangeAction.TrueDone() && scope.activeInHierarchy)
		{
			Ray ray = new Ray(configuration.muzzle.position, configuration.muzzle.forward);
			RaycastHit hitInfo;
			if (Physics.Raycast(ray, out hitInfo, 999f))
			{
				lastRangeReading = Mathf.RoundToInt(hitInfo.distance);
			}
			else
			{
				lastRangeReading = 999;
			}
			rangeText.text = lastRangeReading.ToString();
			findRangeAction.Start();
		}
	}

	public override void Fire(Vector3 direction, bool useMuzzleDirection)
	{
		if (CoolingDown())
		{
			return;
		}
		Ray ray = new Ray(configuration.muzzle.position, configuration.muzzle.forward);
		RaycastHit hitInfo;
		if (!Physics.Raycast(ray, out hitInfo, 999f))
		{
			return;
		}
		if (HasActiveAnimator())
		{
			animator.SetTrigger("fire");
		}
		Object.Instantiate(effectPrefab, hitInfo.point + hitInfo.normal * 0.2f, Quaternion.identity);
		lastFired = Time.time;
		Actor actor = null;
		float num = 999999f;
		foreach (Actor actor2 in ActorManager.instance.actors)
		{
			if (!actor2.aiControlled || actor2.team != user.team || !((AiActorController)actor2.controller).InSquad())
			{
				continue;
			}
			Squad squad = ((AiActorController)actor2.controller).squad;
			if (!squad.HasVehicle())
			{
				float num2 = Vector3.Distance(hitInfo.point, actor2.Position());
				if (num2 < num)
				{
					actor = actor2;
					num = num2;
				}
			}
		}
		if (actor != null)
		{
			Squad squad2 = ((AiActorController)actor.controller).squad;
			if (squad2.HasVehicle())
			{
				squad2.MoveTo(hitInfo.point);
			}
			else
			{
				squad2.MoveToAndDigIn(hitInfo.point);
			}
		}
	}
}
