using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Renderer))]
public class Outline : MonoBehaviour
{
    public OutlineEffect outlineEffect;

	public int color;
	public bool eraseRenderer;

	[HideInInspector]
	public int originalLayer;
	[HideInInspector]
	public Material[] originalMaterials;

	void Start()
    {
    }

    void OnEnable()
    {
		if(outlineEffect == null)
			outlineEffect = Camera.main.GetComponent<OutlineEffect>();
		outlineEffect.AddOutline(this);
    }

    void OnDisable()
    {
        outlineEffect.RemoveOutline(this);
    }
}
