using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class Outline : MonoBehaviour
{
    public OutlineEffect outlineEffect;
    public new Renderer renderer;

    void Start()
    {
        if (outlineEffect == null)
            outlineEffect = Camera.main.GetComponent<OutlineEffect>();

        if (renderer == null)
            renderer = GetComponent<Renderer>();
    }

    void OnEnable()
    {
        outlineEffect.AddOutlineRenderer(renderer);
    }

    void OnDisable()
    {
        outlineEffect.RemoveOutlineRenderer(renderer);
    }
}
