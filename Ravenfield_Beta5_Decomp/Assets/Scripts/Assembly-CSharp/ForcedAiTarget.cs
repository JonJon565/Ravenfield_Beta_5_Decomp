using UnityEngine;

public class ForcedAiTarget : Actor
{
	public TargetType type;

	private int hits;

	public override void Awake()
	{
	}

	private new void Start()
	{
		ActorManager.SetAlive(this);
	}

	protected override void FixedUpdate()
	{
	}

	public override void Update()
	{
		Highlight();
	}

	public override Vector3 CenterPosition()
	{
		return base.transform.position;
	}

	public override Vector3 Position()
	{
		return base.transform.position;
	}

	public override Vector3 Velocity()
	{
		return Vector3.zero;
	}

	public override TargetType GetTargetType()
	{
		return type;
	}

	public override bool Damage(float healthDamage, float balanceDamage, bool piercing, Vector3 point, Vector3 direction, Vector3 impactForce)
	{
		hits++;
		Debug.Log("Took damage: " + healthDamage + " (balance: " + balanceDamage + " Hits: " + hits);
		return true;
	}
}
