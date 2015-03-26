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

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class OutlineEffect : MonoBehaviour 
{
	public List<Renderer> outlineRenderers = new List<Renderer>();
    public List<Renderer> eraseRenderers = new List<Renderer>();

    public float lineThickness = 4f;
    public float lineIntensity = .5f;

    public Color lineColor = Color.white;

    private Material outlineSourceMaterial;
    private Material outlineEraseMaterial;
    private Shader outlineTargetShader;
	private Material _outlineMaterial;
	private RenderTexture _renderTexture;
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

    void Awake ()
    {
        outlineEraseMaterial = Resources.Load<Material>("OutlineEffect/OutlineEraseMaterial");
        outlineSourceMaterial = Resources.Load<Material>("OutlineEffect/OutlineSourceMaterial");
        outlineTargetShader = Resources.Load<Shader>("OutlineEffect/OutlineTargetShader");
    }
	
	void Start () 
	{
		CreateMaterialsIfNeeded();
	}

	void OnPreCull()
	{
		Camera camera = GetComponent<Camera>();

		int width = camera.pixelWidth;
		int height = camera.pixelHeight;
		_renderTexture = RenderTexture.GetTemporary(width, height, 16, RenderTextureFormat.Default);

		if( _camera == null)
		{
			GameObject cameraGameObject = new GameObject("OutlineCamera");
			cameraGameObject.hideFlags = HideFlags.HideAndDontSave;
			_camera = cameraGameObject.AddComponent<Camera>();
		}

		_camera.CopyFrom(camera);
		_camera.renderingPath = RenderingPath.Forward;
		_camera.enabled = false;
		_camera.backgroundColor = new Color(0.0f, 0.0f, 1.0f, 0.0f);
		_camera.clearFlags = CameraClearFlags.SolidColor;
		_camera.cullingMask = LayerMask.GetMask("Outline");

		if(outlineRenderers != null)
		{
            originalMaterials = new Material[outlineRenderers.Count];
            originalLayers = new int[outlineRenderers.Count];
            for(int i = 0; i < outlineRenderers.Count; i++)
            {
                if (outlineRenderers[i] != null)
                {
                    originalMaterials[i] = outlineRenderers[i].sharedMaterial;
                    originalLayers[i] = outlineRenderers[i].gameObject.layer;

                    outlineRenderers[i].sharedMaterial = outlineSourceMaterial;
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

		_camera.targetTexture = _renderTexture;
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

	void OnRenderImage( RenderTexture source, RenderTexture destination)
	{
		CreateMaterialsIfNeeded();
		UpdateMaterialsPublicProperties();

		_outlineMaterial.SetTexture("_OutlineSource", _renderTexture);
		Graphics.Blit(source, destination, _outlineMaterial);
		RenderTexture.ReleaseTemporary(_renderTexture);
	}

	private void CreateMaterialsIfNeeded()
	{
		if( _outlineMaterial == null)
		{
			_outlineMaterial = new Material(outlineTargetShader);
			_outlineMaterial.hideFlags = HideFlags.HideAndDontSave;
			_outlineMaterial.SetColor ("_LineColor", lineColor);
		}
	}

	private void DestroyMaterials()
	{
		DestroyImmediate(_outlineMaterial);
		_outlineMaterial = null;
	}

	private void UpdateMaterialsPublicProperties()
	{
		if( _outlineMaterial)
		{
            _outlineMaterial.SetFloat("_LineThicknessX", lineThickness / 1000);
            _outlineMaterial.SetFloat("_LineThicknessY", (lineThickness * 2) / 1000);
            _outlineMaterial.SetFloat("_LineIntensity", lineIntensity);
        }
	}
}