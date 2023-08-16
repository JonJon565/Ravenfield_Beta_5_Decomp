public class ToggleableItem : Weapon
{
	public virtual void Toggle()
	{
	}

	public override void CullFpsObjects()
	{
	}

	public override bool IsToggleable()
	{
		return true;
	}

	public override void Unholster()
	{
	}

	public override void Holster()
	{
	}
}
