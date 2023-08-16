using UnityEngine;
using UnityEngine.UI;

public class DropdownExplanation : MonoBehaviour
{
	private Dropdown dropdown;

	public int item;

	private void Awake()
	{
		dropdown = GetComponentInParent<Dropdown>();
		dropdown.onValueChanged.AddListener(ValueChanged);
		ValueChanged(dropdown.value);
	}

	private void ValueChanged(int newValue)
	{
		base.gameObject.SetActive(dropdown.value == item);
	}
}
