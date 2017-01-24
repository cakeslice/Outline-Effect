using UnityEngine;

namespace cakeslice
{
    [RequireComponent(typeof(Renderer))]
    public class Outline : MonoBehaviour
    {
        public Renderer Renderer { get; private set; }

        public int color;
        public bool eraseRenderer;

        [HideInInspector]
        public int originalLayer;
        [HideInInspector]
        public Material[] originalMaterials;

        private void Awake()
        {
            Renderer = GetComponent<Renderer>();
        }

        void OnEnable()
        {
            OutlineEffect.Instance.AddOutline(this);
        }

        void OnDisable()
        {
            OutlineEffect.Instance.RemoveOutline(this);
        }
    }
}