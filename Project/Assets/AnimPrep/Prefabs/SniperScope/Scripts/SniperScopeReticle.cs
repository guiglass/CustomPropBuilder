using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SniperScopeReticle : MonoBehaviour {

	[HideInInspector]
	public Texture reticleTexture;
	[HideInInspector]
	public Material reticleMaterial;

	void OnRenderImage (RenderTexture source, RenderTexture destination)
	{	// Postprocess the image and add the reticle to the output buffers.
		Graphics.Blit (source, destination); // first apply the renderTexture from all lower depth cameras.
		Graphics.Blit (reticleTexture, destination, reticleMaterial); // Now apply the reticle texture to the output buffers.
	}

}
