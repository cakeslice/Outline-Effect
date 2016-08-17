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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Camera))]
public class OutlineEffect : MonoBehaviour
{
    List<Renderer> outlineRenderers = new List<Renderer>();
    List<Renderer> eraseRenderers = new List<Renderer>();

    public Camera sourceCamera;
    public Camera outlineCamera;

    public List<int> outlineRendererColors = new List<int>();

    public float lineThickness = 4f;
    public float lineIntensity = .5f;

    public Color lineColor0 = Color.red;
    public Color lineColor1 = Color.green;
    public Color lineColor2 = Color.blue;
    public bool flipY = false;
    public bool darkOutlines = false;
    public float alphaCutoff = .5f;

    Material outline1Material;
    Material outline2Material;
    Material outline3Material;
    Material outlineEraseMaterial;
    Shader outlineShader;
    Shader outlineBufferShader;
    Material outlineShaderMaterial;
    RenderTexture renderTexture;

    List<Material> originalMaterials = new List<Material>();
    List<int> originalLayers = new List<int>();
    List<Material> originalEraseMaterials = new List<Material>();
    List<int> originalEraseLayers = new List<int>();

    Material GetMaterialFromID(int ID)
    {
        if (ID == 0)
            return outline1Material;
        else if (ID == 1)
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

    void Start()
    {
        CreateMaterialsIfNeeded();
        UpdateMaterialsPublicProperties();

        if (sourceCamera == null)
        {
            sourceCamera = GetComponent<Camera>();

            if (sourceCamera == null)
                sourceCamera = Camera.main;
        }

        if (outlineCamera == null)
        {
            GameObject cameraGameObject = new GameObject("Outline Camera");
            cameraGameObject.transform.parent = sourceCamera.transform;
            outlineCamera = cameraGameObject.AddComponent<Camera>();
        }

        UpdateOutlineCameraFromSource();

        renderTexture = new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 16, RenderTextureFormat.Default);
        outlineCamera.targetTexture = renderTexture;
    }

    void OnDestroy()
    {
        renderTexture.Release();
        DestroyMaterials();
    }

    void OnPreCull()
    {
        while (outlineRendererColors.Count < outlineRenderers.Count)
            outlineRendererColors.Add(0);

        if (outlineRenderers != null)
        {
            for (int i = 0; i < outlineRenderers.Count; i++)
            {
                if (outlineRenderers[i] != null)
                {
                    originalMaterials[i] = outlineRenderers[i].sharedMaterial;
                    originalLayers[i] = outlineRenderers[i].gameObject.layer;

                    if (outlineRendererColors != null && outlineRendererColors.Count > i)
                        outlineRenderers[i].sharedMaterial = GetMaterialFromID(outlineRendererColors[i]);
                    else
                        outlineRenderers[i].sharedMaterial = outline1Material;

                    if (outlineRenderers[i] is MeshRenderer)
                    {
                        outlineRenderers[i].sharedMaterial.mainTexture = originalMaterials[i].mainTexture;
                    }

                    outlineRenderers[i].gameObject.layer = LayerMask.NameToLayer("Outline");
                }
            }
        }
        if (eraseRenderers != null)
        {
            for (int i = 0; i < eraseRenderers.Count; i++)
            {
                if (eraseRenderers[i] != null)
                {
                    originalEraseMaterials[i] = eraseRenderers[i].sharedMaterial;
                    originalEraseLayers[i] = eraseRenderers[i].gameObject.layer;

                    eraseRenderers[i].sharedMaterial = outlineEraseMaterial;

                    if (eraseRenderers[i] is MeshRenderer)
                        eraseRenderers[i].sharedMaterial.mainTexture = originalEraseMaterials[i].mainTexture;

                    eraseRenderers[i].gameObject.layer = LayerMask.NameToLayer("Outline");
                }
            }
        }

        outlineCamera.Render();

        if (outlineRenderers != null)
        {
            for (int i = 0; i < outlineRenderers.Count; i++)
            {
                if (outlineRenderers[i] != null)
                {
                    if (outlineRenderers[i] is MeshRenderer)
                        outlineRenderers[i].sharedMaterial.mainTexture = null;

                    outlineRenderers[i].sharedMaterial = originalMaterials[i];
                    outlineRenderers[i].gameObject.layer = originalLayers[i];
                }
            }
        }
        if (eraseRenderers != null)
        {
            for (int i = 0; i < eraseRenderers.Count; i++)
            {
                if (eraseRenderers[i] != null)
                {
                    if (eraseRenderers[i] is MeshRenderer)
                        eraseRenderers[i].sharedMaterial.mainTexture = null;

                    eraseRenderers[i].sharedMaterial = originalEraseMaterials[i];
                    eraseRenderers[i].gameObject.layer = originalEraseLayers[i];
                }
            }
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        outlineShaderMaterial.SetTexture("_OutlineSource", renderTexture);
        Graphics.Blit(source, destination, outlineShaderMaterial);
    }

    private void CreateMaterialsIfNeeded()
    {
        if (outlineShader == null)
            outlineShader = Resources.Load<Shader>("OutlineEffect/OutlineShader");
        if (outlineBufferShader == null)
            outlineBufferShader = Resources.Load<Shader>("OutlineEffect/OutlineBufferShader");
        if (outlineShaderMaterial == null)
        {
            outlineShaderMaterial = new Material(outlineShader);
            outlineShaderMaterial.hideFlags = HideFlags.HideAndDontSave;
            UpdateMaterialsPublicProperties();
        }
        if (outlineEraseMaterial == null)
            outlineEraseMaterial = CreateMaterial(new Color(0, 0, 0, 0));
        if (outline1Material == null)
            outline1Material = CreateMaterial(new Color(1, 0, 0, 0));
        if (outline2Material == null)
            outline2Material = CreateMaterial(new Color(0, 1, 0, 0));
        if (outline3Material == null)
            outline3Material = CreateMaterial(new Color(0, 0, 1, 0));

        outline1Material.SetFloat("_AlphaCutoff", alphaCutoff);
        outline2Material.SetFloat("_AlphaCutoff", alphaCutoff);
        outline3Material.SetFloat("_AlphaCutoff", alphaCutoff);
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

    private void UpdateMaterialsPublicProperties()
    {
        if (outlineShaderMaterial)
        {
            outlineShaderMaterial.SetFloat("_LineThicknessX", lineThickness / 1000);
            outlineShaderMaterial.SetFloat("_LineThicknessY", lineThickness / 1000);
            outlineShaderMaterial.SetFloat("_LineIntensity", lineIntensity);
            outlineShaderMaterial.SetColor("_LineColor1", lineColor0);
            outlineShaderMaterial.SetColor("_LineColor2", lineColor1);
            outlineShaderMaterial.SetColor("_LineColor3", lineColor2);
            if (flipY)
                outlineShaderMaterial.SetInt("_FlipY", 1);
            else
                outlineShaderMaterial.SetInt("_FlipY", 0);
            if (darkOutlines)
                outlineShaderMaterial.SetInt("_Dark", 1);
            else
                outlineShaderMaterial.SetInt("_Dark", 0);
        }
    }

    // Call this when source camera has been changed.
    public void UpdateFromSource()
    {
        renderTexture.width = sourceCamera.pixelWidth;
        renderTexture.height = sourceCamera.pixelHeight;

        UpdateOutlineCameraFromSource();
    }

    void UpdateOutlineCameraFromSource()
    {
        outlineCamera.CopyFrom(sourceCamera);
        outlineCamera.renderingPath = RenderingPath.Forward;
        outlineCamera.enabled = false;
        outlineCamera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        outlineCamera.clearFlags = CameraClearFlags.SolidColor;
        outlineCamera.cullingMask = LayerMask.GetMask("Outline");
        outlineCamera.rect = new Rect(0, 0, 1, 1);
    }

    public void AddOutlineRenderer(Renderer renderer)
    {
        if (!outlineRenderers.Contains(renderer))
        {
            outlineRenderers.Add(renderer);
            originalMaterials.Add(null);
            originalLayers.Add(0);
        }
    }

    public void RemoveOutlineRenderer(Renderer renderer)
    {
        if (outlineRenderers.Remove(renderer))
        {
            originalMaterials.RemoveAt(originalLayers.Count - 1);
            originalLayers.RemoveAt(originalLayers.Count - 1);
        }
    }

    public void AddEraseRenderer(Renderer renderer)
    {
        if (!eraseRenderers.Contains(renderer))
        {
            eraseRenderers.Add(renderer);
            originalEraseMaterials.Add(null);
            originalEraseLayers.Add(0);
        }
    }

    public void RemoveEraseRenderer(Renderer renderer)
    {
        if (eraseRenderers.Remove(renderer))
        {
            originalEraseMaterials.RemoveAt(eraseRenderers.Count - 1);
            originalEraseLayers.RemoveAt(eraseRenderers.Count - 1);
        }
    }
}
