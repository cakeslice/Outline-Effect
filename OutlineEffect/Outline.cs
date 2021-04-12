/*
 * Copyright (c) 2015 José Guerreiro. All rights reserved.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace cakeslice
{
	[RequireComponent(typeof(Renderer))]
	/* [ExecuteInEditMode] */
	public class Outline : MonoBehaviour
	{
		public Renderer Renderer { get; private set; }
		public SpriteRenderer SpriteRenderer { get; private set; }
		public SkinnedMeshRenderer SkinnedMeshRenderer { get; private set; }
		public MeshFilter MeshFilter { get; private set; }

		public int color;
		public bool eraseRenderer;

		private void Awake()
		{
			Renderer = GetComponent<Renderer>();
			SkinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
			SpriteRenderer = GetComponent<SpriteRenderer>();
			MeshFilter = GetComponent<MeshFilter>();
		}

		void OnEnable()
		{
			OutlineEffect.Instance?.AddOutline(this);
		}

		void OnDisable()
		{
			OutlineEffect.Instance?.RemoveOutline(this);
		}

		private bool visible;
		private void OnBecameVisible()
		{
			visible = true;
		}
		private void OnBecameInvisible()
		{
			visible = false;
		}
		public bool IsVisible => visible;

		private Material[] _SharedMaterials;
		public Material[] SharedMaterials
		{
			get
			{
				if (_SharedMaterials == null)
					_SharedMaterials = Renderer.sharedMaterials;

				return _SharedMaterials;
			}
		}
	}
}
