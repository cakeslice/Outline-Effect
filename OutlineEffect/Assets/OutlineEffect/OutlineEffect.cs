/*
//  Copyright (c) 2015 José Guerreiro. All rights reserved.
//
//  MIT license, see http://www.opensource.org/licenses/mit-license.php
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
*/

using UnityEngine;
using System.Collections.Generic;

namespace cakeslice
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class OutlineEffect : MonoBehaviour
    {
        private static OutlineEffect m_instance;
        public static OutlineEffect Instance
        {
            get
            {
                if(Equals(m_instance, null))
                {
                    return m_instance = FindObjectOfType(typeof(OutlineEffect)) as OutlineEffect;
                }

                return m_instance;
            }
        }
        private OutlineEffect() { }

        private readonly List<Outline> outlines = new List<Outline>();

        [Range(1.0f, 6.0f)]
        public float lineThickness = 1.25f;
        [Range(0, 10)]
        public float lineIntensity = .5f;
        [Range(0, 1)]
        public float fillAmount = 0.2f;

        public Color lineColor0 = Color.red;
        public Color lineColor1 = Color.green;
        public Color lineColor2 = Color.blue;

        public bool additiveRendering = false;

        [Header("These settings can affect performance!")]
        public bool cornerOutlines = false;
        public bool addLinesBetweenColors = false;

        [Header("Advanced settings")]
        public bool scaleWithScreenSize = true;
        [Range(0.1f, .9f)]
        public float alphaCutoff = .5f;
        public bool flipY = false;
        public Camera sourceCamera;

        [HideInInspector]
        public Camera outlineCamera;
        Material outline1Material;
        Material outline2Material;
        Material outline3Material;
        Material outlineEraseMaterial;
        Shader outlineShader;
        Shader outlineBufferShader;
        Material outlineShaderMaterial;
        RenderTexture renderTexture;
        RenderTexture extraRenderTexture;

        Material GetMaterialFromID(int ID)
        {
            if(ID == 0)
                return outline1Material;
            else if(ID == 1)
                return outline2Material;
            else
                return outline3Material;
        }

        Material CreateMaterial(Color emissionColor)
        {
            Material m = new Material(outlineBufferShader);
            m.SetColor("_Color", emissionColor);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            return m;
        }

        private void Awake()
        {
            m_instance = this;
        }

        void Start()
        {
            CreateMaterialsIfNeeded();
            UpdateMaterialsPublicProperties();

            if(sourceCamera == null)
            {
                sourceCamera = GetComponent<Camera>();

                if(sourceCamera == null)
                    sourceCamera = Camera.main;
            }

            if(outlineCamera == null)
            {
                GameObject cameraGameObject = new GameObject("Outline Camera");
                cameraGameObject.transform.parent = sourceCamera.transform;
                outlineCamera = cameraGameObject.AddComponent<Camera>();
            }

            renderTexture = new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 16, RenderTextureFormat.Default);
            extraRenderTexture = new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 16, RenderTextureFormat.Default);
            UpdateOutlineCameraFromSource();
        }

        void OnDestroy()
        {
            if(renderTexture != null)
                renderTexture.Release();
            if(extraRenderTexture != null)
                extraRenderTexture.Release();
            DestroyMaterials();
        }

        void OnPreCull()
        {
            if(renderTexture.width != sourceCamera.pixelWidth || renderTexture.height != sourceCamera.pixelHeight)
            {
                renderTexture = new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 16, RenderTextureFormat.Default);
                extraRenderTexture = new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 16, RenderTextureFormat.Default);
                outlineCamera.targetTexture = renderTexture;
            }
            UpdateMaterialsPublicProperties();
            UpdateOutlineCameraFromSource();

            if(outlines != null)
            {
                for(int i = 0; i < outlines.Count; i++)
                {
                    LayerMask l = sourceCamera.cullingMask;
                    if(outlines[i] != null && l == (l | (1 << outlines[i].originalLayer)))
                    {
                        outlines[i].originalMaterials = outlines[i].Renderer.sharedMaterials;

                        outlines[i].originalLayer = outlines[i].gameObject.layer;

                        Material[] outlineMaterials = new Material[outlines[i].originalMaterials.Length];
                        for(int j = 0; j < outlineMaterials.Length; j++)
                        {
                            if(outlines[i].eraseRenderer)
                                outlineMaterials[j] = outlineEraseMaterial;
                            else
                                outlineMaterials[j] = GetMaterialFromID(outlines[i].color);
                        }


                        outlines[i].Renderer.sharedMaterials = outlineMaterials;

                        for(int m = 0; m < outlines[i].Renderer.materials.Length; m++)
                        {
                            if(outlines[i].Renderer is MeshRenderer)
                                outlines[i].Renderer.materials[m].mainTexture = outlines[i].originalMaterials[m].mainTexture;
                        }

                        outlines[i].gameObject.layer = 5;
                    }
                }
            }

            outlineCamera.Render();

            if(outlines != null)
            {
                for(int i = 0; i < outlines.Count; i++)
                {
                    LayerMask l = sourceCamera.cullingMask;
                    if(outlines[i] != null && l == (l | (1 << outlines[i].originalLayer)))
                    {
                        for(int m = 0; m < outlines[i].Renderer.sharedMaterials.Length; m++)
                        {
                            if(outlines[i].Renderer is MeshRenderer)
                            {
                                outlines[i].Renderer.sharedMaterials[m].mainTexture = null;
                            }
                        }

                        outlines[i].Renderer.sharedMaterials = outlines[i].originalMaterials;

                        outlines[i].gameObject.layer = outlines[i].originalLayer;
                    }
                }
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            outlineShaderMaterial.SetTexture("_OutlineSource", renderTexture);
 
            if(addLinesBetweenColors)
            {
                Graphics.Blit(source, extraRenderTexture, outlineShaderMaterial, 0);
                outlineShaderMaterial.SetTexture("_OutlineSource", extraRenderTexture);
            }
            Graphics.Blit(source, destination, outlineShaderMaterial, 1);
        }

        private void CreateMaterialsIfNeeded()
        {
            if(outlineShader == null)
                outlineShader = Resources.Load<Shader>("OutlineShader");
            if(outlineBufferShader == null)
                outlineBufferShader = Resources.Load<Shader>("OutlineBufferShader");
            if(outlineShaderMaterial == null)
            {
                outlineShaderMaterial = new Material(outlineShader);
                outlineShaderMaterial.hideFlags = HideFlags.HideAndDontSave;
                UpdateMaterialsPublicProperties();
            }
            if(outlineEraseMaterial == null)
                outlineEraseMaterial = CreateMaterial(new Color(0, 0, 0, 0));
            if(outline1Material == null)
                outline1Material = CreateMaterial(new Color(1, 0, 0, 0));
            if(outline2Material == null)
                outline2Material = CreateMaterial(new Color(0, 1, 0, 0));
            if(outline3Material == null)
                outline3Material = CreateMaterial(new Color(0, 0, 1, 0));
        }

        private void DestroyMaterials()
        {
            DestroyImmediate(outlineShaderMaterial);
            DestroyImmediate(outlineEraseMaterial);
            DestroyImmediate(outline1Material);
            DestroyImmediate(outline2Material);
            DestroyImmediate(outline3Material);
            outlineShader = null;
            outlineBufferShader = null;
            outlineShaderMaterial = null;
            outlineEraseMaterial = null;
            outline1Material = null;
            outline2Material = null;
            outline3Material = null;
        }

        public void UpdateMaterialsPublicProperties()
        {
            if(outlineShaderMaterial)
            {
                float scalingFactor = 1;
                if(scaleWithScreenSize)
                {
                    // If Screen.height gets bigger, outlines gets thicker
                    scalingFactor = Screen.height / 360.0f;
                }

                // If scaling is too small (height less than 360 pixels), make sure you still render the outlines, but render them with 1 thickness
                if(scaleWithScreenSize && scalingFactor < 1)
                {
                    outlineShaderMaterial.SetFloat("_LineThicknessX", (1 / 1000.0f) * (1.0f / Screen.width) * 1000.0f);
                    outlineShaderMaterial.SetFloat("_LineThicknessY", (1 / 1000.0f) * (1.0f / Screen.height) * 1000.0f);
                }
                else
                {
                    outlineShaderMaterial.SetFloat("_LineThicknessX", scalingFactor * (lineThickness / 1000.0f) * (1.0f / Screen.width) * 1000.0f);
                    outlineShaderMaterial.SetFloat("_LineThicknessY", scalingFactor * (lineThickness / 1000.0f) * (1.0f / Screen.height) * 1000.0f);
                }
                outlineShaderMaterial.SetFloat("_LineIntensity", lineIntensity);
                outlineShaderMaterial.SetFloat("_FillAmount", fillAmount);
                outlineShaderMaterial.SetColor("_LineColor1", lineColor0 * lineColor0);
                outlineShaderMaterial.SetColor("_LineColor2", lineColor1 * lineColor1);
                outlineShaderMaterial.SetColor("_LineColor3", lineColor2 * lineColor2);
                if(flipY)
                    outlineShaderMaterial.SetInt("_FlipY", 1);
                else
                    outlineShaderMaterial.SetInt("_FlipY", 0);
                if(!additiveRendering)
                    outlineShaderMaterial.SetInt("_Dark", 1);
                else
                    outlineShaderMaterial.SetInt("_Dark", 0);
                if(cornerOutlines)
                    outlineShaderMaterial.SetInt("_CornerOutlines", 1);
                else
                    outlineShaderMaterial.SetInt("_CornerOutlines", 0);

                Shader.SetGlobalFloat("_OutlineAlphaCutoff", alphaCutoff);
            }
        }

        void UpdateOutlineCameraFromSource()
        {
            outlineCamera.CopyFrom(sourceCamera);
            outlineCamera.renderingPath = RenderingPath.Forward;
            outlineCamera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            outlineCamera.clearFlags = CameraClearFlags.SolidColor;
            outlineCamera.rect = new Rect(0, 0, 1, 1);
            outlineCamera.enabled = true;
            outlineCamera.cullingMask = 1 << 5; // UI layer
            outlineCamera.targetTexture = renderTexture;
        }

        public void AddOutline(Outline outline)
        {
            if(!outlines.Contains(outline))
            {
                outlines.Add(outline);
            }
        }
        public void RemoveOutline(Outline outline)
        {
            outlines.Remove(outline);
        }
    }
}