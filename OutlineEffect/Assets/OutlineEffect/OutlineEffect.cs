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

[RequireComponent(typeof(Camera))]
public class OutlineEffect : MonoBehaviour 
{
	public List<Renderer> outlineRenderers = new List<Renderer>();
    public List<int> outlineRendererColors = new List<int>();
    public List<Renderer> eraseRenderers = new List<Renderer>();

    public float lineThickness = 4f;
    public float lineIntensity = .5f;

    public Color lineColor1 = Color.red;
    public Color lineColor2 = Color.green;
    public Color lineColor3 = Color.blue;
    public bool flipY = false;

    private Material outline1Material;
    private Material outline2Material;
    private Material outline3Material;
    private Material outlineEraseMaterial;
    private Shader outlineShader;
    private Shader outlineBufferShader;
	private Material outlineShaderMaterial;
	private RenderTexture renderTexture;
	private Camera _camera;

    Material[] originalMaterials = new Material[1];
    int[] originalLayers = new int[1];
    Material[] originalEraseMaterials = new Material[1];
    int[] originalEraseLayers = new int[1];

	void OnEnable()
	{   
		CreateMaterialsIfNeeded();
	}

	void OnDisable()
	{
		DestroyMaterials();

		if( _camera)
		{
			DestroyImmediate( _camera.gameObject);
			_camera = null;
		}
	}

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

	void Start () 
	{
		CreateMaterialsIfNeeded();
	}

	void OnPreCull()
	{
        if (outlineRenderers != null && outlineRenderers.Count != 0)
        {
            Camera camera = GetComponent<Camera>();

            int width = camera.pixelWidth;
            int height = camera.pixelHeight;
            renderTexture = RenderTexture.GetTemporary(width, height, 16, RenderTextureFormat.Default);

            if (_camera == null)
            {
                GameObject cameraGameObject = new GameObject("OutlineCamera");
                cameraGameObject.hideFlags = HideFlags.HideAndDontSave;
                _camera = cameraGameObject.AddComponent<Camera>();
            }

            _camera.CopyFrom(camera);
            _camera.renderingPath = RenderingPath.Forward;
            _camera.enabled = false;
            _camera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.cullingMask = LayerMask.GetMask("Outline");

            if (outlineRenderers != null)
            {
                originalMaterials = new Material[outlineRenderers.Count];
                originalLayers = new int[outlineRenderers.Count];
                for (int i = 0; i < outlineRenderers.Count; i++)
                {
                    if (outlineRenderers[i] != null)
                    {
                        originalMaterials[i] = outlineRenderers[i].sharedMaterial;
                        originalLayers[i] = outlineRenderers[i].gameObject.layer;

                        if (outlineRendererColors != null && outlineRendererColors.Contains(i))
                            outlineRenderers[i].sharedMaterial = GetMaterialFromID(outlineRendererColors[i]);
                        else
                            outlineRenderers[i].sharedMaterial = outline1Material;

                        outlineRenderers[i].gameObject.layer = LayerMask.NameToLayer("Outline");
                    }
                }
            }
            if (eraseRenderers != null)
            {
                originalEraseMaterials = new Material[eraseRenderers.Count];
                originalEraseLayers = new int[eraseRenderers.Count];
                for (int i = 0; i < eraseRenderers.Count; i++)
                {
                    if (eraseRenderers[i] != null)
                    {
                        originalEraseMaterials[i] = eraseRenderers[i].sharedMaterial;
                        originalEraseLayers[i] = eraseRenderers[i].gameObject.layer;

                        eraseRenderers[i].sharedMaterial = outlineEraseMaterial;
                        eraseRenderers[i].gameObject.layer = LayerMask.NameToLayer("Outline");
                    }
                }
            }

            _camera.targetTexture = renderTexture;
            _camera.Render();

            if (outlineRenderers != null)
            {
                for (int i = 0; i < outlineRenderers.Count; i++)
                {
                    if (outlineRenderers[i] != null)
                    {
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
                        eraseRenderers[i].sharedMaterial = originalEraseMaterials[i];
                        eraseRenderers[i].gameObject.layer = originalEraseLayers[i];
                    }
                }
            }
        }
	}

	void OnRenderImage( RenderTexture source, RenderTexture destination)
	{
		CreateMaterialsIfNeeded();
		UpdateMaterialsPublicProperties();

		outlineShaderMaterial.SetTexture("_OutlineSource", renderTexture);
		Graphics.Blit(source, destination, outlineShaderMaterial);
		RenderTexture.ReleaseTemporary(renderTexture);
	}

	private void CreateMaterialsIfNeeded()
	{
        if(outlineShader == null)
            outlineShader = Resources.Load<Shader>("OutlineEffect/OutlineShader");
        if (outlineBufferShader == null)
            outlineBufferShader = Resources.Load<Shader>("OutlineEffect/OutlineBufferShader");
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

	private void UpdateMaterialsPublicProperties()
	{
		if(outlineShaderMaterial)
		{
            outlineShaderMaterial.SetFloat("_LineThicknessX", lineThickness / 1000);
            outlineShaderMaterial.SetFloat("_LineThicknessY", (lineThickness * 2) / 1000);
            outlineShaderMaterial.SetFloat("_LineIntensity", lineIntensity);
            outlineShaderMaterial.SetColor("_LineColor1", lineColor1);
            outlineShaderMaterial.SetColor("_LineColor2", lineColor2);
            outlineShaderMaterial.SetColor("_LineColor3", lineColor3);
            if(flipY)
                outlineShaderMaterial.SetInt("_FlipY", 1);
            else
                outlineShaderMaterial.SetInt("_FlipY", 0);
        }
	}
}