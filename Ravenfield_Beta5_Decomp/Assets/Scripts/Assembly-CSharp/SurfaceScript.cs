using UnityEngine;

public class SurfaceScript : MonoBehaviour
{
	private void Start()
	{
		Material material = ((base.transform.parent.GetComponent<MarkerScript>().objectScript.materialType != 0) ? ((Material)Object.Instantiate(Resources.Load("surfaceAlphaMaterial", typeof(Material)))) : ((Material)Object.Instantiate(Resources.Load("surfaceMaterial", typeof(Material)))));
		Color color = material.color;
		color.a = base.transform.parent.GetComponent<MarkerScript>().objectScript.surfaceOpacity;
		base.gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
	}
}
