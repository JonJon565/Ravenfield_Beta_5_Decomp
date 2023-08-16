using UnityEngine;

public static class ColorScheme
{
	public static Color TeamColor(int team)
	{
		switch (team)
		{
		case 0:
			return Color.blue;
		case 1:
			return Color.red;
		default:
			return Color.Lerp(Color.gray, Color.white, 0.5f);
		}
	}
}
