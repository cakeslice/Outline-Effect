/*
//  Copyright (c) 2015 JosÃ© Guerreiro. All rights reserved.
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
	List<Outline> outlines = new List<Outline>();

    [Range(0, 4)]
    public float lineThickness = 4f;
    [Range(0, 10)]
    public float lineIntensity = .5f;
    [Range(0, 1)]
    public float fillAmount = 0.2f;

    public Color lineColor0 = Color.red;
    public Color lineColor1 = Color.green;
    public Color lineColor2 = Color.blue;

    public bool additiveRendering = true;

    [Header("These settings can affect performance!")]
    public bool cornerOutlines = false;
    public bool addLinesBetweenColors = false;

    [Header("Advanced settings")]
    public bool scaleWithScreenSize = true;
    [Range(0.1f, .9f)]
    public float alphaCutoff = .5f;
    public bool flipY = false;
    public Camera sourceCamera;
    public Camera outlineCamera;
    [Tooltip("Layer index used by the outline camera")]
    public int outlineLayer = 31;

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

		if (outlines != null)
        {
			for (int i = 0; i < outlines.Count; i++)
            {
                if (outlines[i] != null)
                {
					outlines[i].originalMaterials = outlines[i].GetComponent<Renderer>().sharedMaterials;

					outlines[i].originalLayer = outlines[i].gameObject.layer;

                    Material[] outlineMaterials = new Material[outlines[i].originalMaterials.Length];
                    for(int j = 0; j < outlineMaterials.Length; j++)
                    {
                        if(outlines[i].eraseRenderer)
                            outlineMaterials[j] = outlineEraseMaterial;
                        else
                            outlineMaterials[j] = GetMaterialFromID(outlines[i].color);
                    }


                    outlines[i].GetComponent<Renderer>().sharedMaterials = outlineMaterials;

                    for(int m = 0; m < outlines[i].GetComponent<Renderer>().materials.Length; m++)
                    {
                        if(outlines[i].GetComponent<Renderer>() is MeshRenderer)
                            outlines[i].GetComponent<Renderer>().materials[m].mainTexture = outlines[i].originalMaterials[m].mainTexture;  
                    }
           
                    outlines[i].gameObject.layer = outlineLayer;
                }
            }
        }

        outlineCamera.Render();

        if (outlines != null)
        {
            for (int i = 0; i < outlines.Count; i++)
            {
                if (outlines[i] != null)
                {
                    for(int m = 0; m < outlines[i].GetComponent<Renderer>().sharedMaterials.Length; m++)
                    {
                        if(outlines[i].GetComponent<Renderer>() is MeshRenderer)
                        {                    
                            outlines[i].GetComponent<Renderer>().sharedMaterials[m].mainTexture = null;
                        }
                    }

                    outlines[i].GetComponent<Renderer>().sharedMaterials = outlines[i].originalMaterials;

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
        if (outlineShader == null)
            outlineShader = Resources.Load<Shader>("OutlineShader");
        if (outlineBufferShader == null)
            outlineBufferShader = Resources.Load<Shader>("OutlineBufferShader");
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
        if (outlineShaderMaterial)
        {
            float scalingFactor = 1;
            if(scaleWithScreenSize)
                scalingFactor = Screen.width / 720.0f;

            outlineShaderMaterial.SetFloat("_LineThicknessX", scalingFactor * (lineThickness / 1000));
            outlineShaderMaterial.SetFloat("_LineThicknessY", scalingFactor * (lineThickness / 1000));
            outlineShaderMaterial.SetFloat("_LineIntensity", lineIntensity);
            outlineShaderMaterial.SetFloat("_FillAmount", fillAmount);
            outlineShaderMaterial.SetColor("_LineColor1", lineColor0 * lineColor0);
            outlineShaderMaterial.SetColor("_LineColor2", lineColor1 * lineColor1);
            outlineShaderMaterial.SetColor("_LineColor3", lineColor2 * lineColor2);
            if (flipY)
                outlineShaderMaterial.SetInt("_FlipY", 1);
            else
                outlineShaderMaterial.SetInt("_FlipY", 0);
            if (!additiveRendering)
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
        outlineCamera.cullingMask = ~outlineLayer;
        outlineCamera.rect = new Rect(0, 0, 1, 1);
		outlineCamera.enabled = true;
		outlineCamera.targetTexture = renderTexture;
	}

    public void AddOutline(Outline outline)
    {
        if (!outlines.Contains(outline))
        {
			outlines.Add(outline);
        }
    }
    public void RemoveOutline(Outline outline)
	{
		outlines.Remove(outline);
    }

}
