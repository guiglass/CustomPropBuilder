using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.PostProcessing;

[RequireComponent(typeof(Camera))]
public class SniperScopeCamera : MonoBehaviour {

	//Variable Declarations:
	UnityEngine.XR.XRNode eye; // Default is left eye
	private enum EyeSelector
	{
		left,right 
	}
	[SerializeField] 
	[Tooltip("The eye slection determines which VRNode will be used to align cameras effects.")]
	private EyeSelector EyeSelect;

	private enum ScopeMode
	{
		traverse,locked,disable
	}
	[SerializeField] 
	[Tooltip("Traverse: FX go through minIntensity.\nLocked: FX go to minIntensity.\nDisable: Don't compute.")]
	ScopeMode scopeMode; // Default is traverse

	[SerializeField] 
	[Tooltip("A point that lies at the center of the eyepiece lens and is on plane with it's surface (approximations are ok).")]
	private Transform centerSurfacePoint;

	[SerializeField] 
	[Tooltip("Linear end distance from lens surface, when eye is at this distance eyeProxmity factor will 1.")]
	[Range(0.1f,1.0f)]
	private float eyeMaxDistance = 0.2f; // When considered as part of the linear equation this value represents PT2_X.

	[SerializeField] 
	[Tooltip("Linear start distance from lens surface, when eye is at this distance eyeProxmity factor will eyeMinIntensity.")]
	[Range(0.0f,0.25f)]
	private float eyeMinDistance = 0.1f; // When considered as part of the linear equation this value represents PT1_X.

	[SerializeField] 
	[Tooltip("The minimum intensity (higher values mean effects will be more noticable as lens sruface approaches player's eye).")]
	[Range(0.0f,1.0f)]
	private float eyeMinIntensity = 0.5f; // When considered as part of the linear equation this value represents PT1_Y.

	[SerializeField] 
	[Tooltip("Multiplier for moving the center of the scope shadow (Vignetting) relative to the player's eye.")]
	[Range(0.0f,100.0f)]
	private int scopeShadowVelocity = 50;

	[SerializeField]
	[Tooltip("Multiplier which affects the amount that the image in the lens will appear to bend as player's eye is moved off center of eyepeice.")]
	[Range(0.0f,1.0f)]
	private float lensRefractionVelocity = 0.2f;

	[SerializeField]
	[Tooltip("The material used by Blit to overlay the reticle.")]
	private Material reticleMaterial;

	[SerializeField]
	[Tooltip("The texture used by Blit to overlay the reticle.")]
	private Texture reticleTexture;

	private Camera cam;
	private Transform cameraParent; // The parent object so we can use its position to calculat local position of player's eye relative to the plane with the renter texture.
	private PostProcessingBehaviour post;
	private VignetteModel.Settings vignette; // Get the vignette from the post processing profile.

	private Vector3 localEyePos; // The selected player's eye in local space.
	private Vector3 eyePosition; // The selected player's eye in world space.

	private float slopeProx; // Predetermined slope computed by the max and min eye distance.
	private float yInterceptProx; // The point on the lone where vignetteIntensity (aka the x axis) is equal to zero (or where the render texture plane is actually touching the player's eye).

	private float eyeProxmity; // How much vignetteing to apply to this eye's render texture.
	private Vector2 vignetteCenter; //As eye moves around, center will be moved aswell.

	private bool enableReticule; // Will be set true if there is both a texture and material present.

	//(De)Initialization:
	private void Start () 
	{
		post = GetComponent<PostProcessingBehaviour> ();
		post.profile.vignette.enabled = true;
		vignette = post.profile.vignette.settings;

		enableReticule = reticleMaterial != null && reticleTexture != null;

		// Determine which VRNode to use based on selected eye for the attached camera.
		var isDefaultEye = EyeSelect == EyeSelector.left;
		eye = isDefaultEye ? UnityEngine.XR.XRNode.LeftEye : UnityEngine.XR.XRNode.RightEye;

		// Precompute the constants for the linear equation:
		//    m   = (PT2_Y -    PT1_Y      ) / (    PT2_X      -      PT1_X    ) 
		slopeProx = (1.0f - eyeMinIntensity) / (eyeMaxDistance - eyeMinDistance); // Precomputed slope: m = (y2-y1) / (x2-x1)

		//    b        =     PT1_Y       - (    m     *     PT1_X     )
		yInterceptProx = eyeMinIntensity - (slopeProx * eyeMinDistance); // Given the slope, substitue a value for PT1 and solve for Y.

		//Debug and print the constants for the linear equation:
		//Debug.Log("y2=" + 1 + " y1=" + eyeMinIntensity + " x2=" + eyeMaxDistance + " x1=" + eyeMinDistance);
		//Debug.Log("m=" + slopeProx + " b=" + yInterceptProx);
	}

	private void OnEnable () 
	{
		cam = GetComponent<Camera> ();
		cam.enabled = true;
		cameraParent = GameObject.FindGameObjectWithTag ("MainCamera").transform.parent;
	}

	private void OnDestroy() 
	{
		if (isActiveAndEnabled) {
			post.profile.vignette.enabled = false;
		}
	}

	private void Update () 
	{
		if (!cam.enabled) 
		{
			UpdateEye ();
		}
		cam.enabled = eyeProxmity <= 5 + eyeMaxDistance;
	}


	// Application:
	private void UpdateEye () 
	{
		UpdateEyePosition ();
		UpdateEyeProximity ();
	}

	private void UpdateEyePosition () 
	{
		eyePosition = cameraParent.TransformPoint (UnityEngine.XR.InputTracking.GetLocalPosition (eye));
	}

	private void UpdateEyeProximity () 
	{
		eyeProxmity = Vector3.Distance (centerSurfacePoint.position, eyePosition);

		switch(scopeMode) 
		{
		case ScopeMode.traverse :
			eyeProxmity = eyeProxmity < eyeMinDistance ? eyeMinDistance + (eyeMinDistance - eyeProxmity) : eyeProxmity;
			break;
		case ScopeMode.locked :
			eyeProxmity = eyeProxmity < eyeMinDistance ? eyeMinDistance : eyeProxmity;
			break;
		}

		//Debug.Log("X IN=" + eyeProxmity); // Debug and print the input value X for the linear equation.

		//Plug all values into the linear eqution and generate a value for eyeProxmity (keep in mind that eyeProxmity represents Y).
		eyeProxmity = (slopeProx * eyeProxmity) + yInterceptProx; // The linear equation: y = m*x+b
		//Debug.Log("Y OUT=" + eyeProxmity); // Debug and print the output value Y from the linear equation.
	}


	//Postprocessing Helpers:	
	private void ComputeDeltas() 
	{

		localEyePos = centerSurfacePoint.InverseTransformPoint (eyePosition);

		vignetteCenter.x = (localEyePos.x * scopeShadowVelocity) + 0.5f;
		vignetteCenter.y = (localEyePos.y * scopeShadowVelocity) + 0.5f;

		localEyePos.z = 0; // Don't allow the camera to move in and out from the scope's objective lens.
		transform.localPosition = localEyePos * lensRefractionVelocity; // Move the camera to align with the camera eye and mimic a lens refraction effect.
	}

	private void DoLensEffect() 
	{
		vignette.center.x = vignetteCenter.x;
		vignette.center.y = vignetteCenter.y;
		vignette.intensity = Mathf.Abs(eyeProxmity);

		post.profile.vignette.settings = vignette;
	}


	//Rendering:
	void OnRenderImage (RenderTexture source, RenderTexture destination)
	{	// Postprocess the image and add the reticle to the output buffers.
		Graphics.Blit (source, destination); // first apply the renderTexture from all lower depth cameras.

		if (enableReticule) { // If there is a texture and material present.
			Graphics.Blit (reticleTexture, destination, reticleMaterial); // Now apply the reticle texture to the output buffers.
		}
	}

	private void OnPreRender ()
	{
		UpdateEye ();
		ComputeDeltas ();
		DoLensEffect (); 
	}

}




/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.PostProcessing;

[RequireComponent(typeof(Camera))]
public class SniperScopeCamera : MonoBehaviour {

	//Variable Declarations:
	VRNode eye; // Default is left eye
	private enum EyeSelector
	{
		left,right 
	}
	[SerializeField] 
	[Tooltip("The eye slection determines which VRNode will be used to align cameras effects.")]
	private EyeSelector EyeSelect;

	private enum ScopeMode
	{
		traverse,locked,disable
	}
	[SerializeField] 
	[Tooltip("Traverse: FX go through minIntensity.\nLocked: FX go to minIntensity.\nDisable: Don't compute.")]
	ScopeMode scopeMode; // Default is traverse

	[SerializeField] 
	[Tooltip("A point that lies at the center of the eyepiece lens and is on plane with it's surface (approximations are ok).")]
	private Transform centerSurfacePoint;

	[SerializeField] 
	[Tooltip("Linear end distance from lens surface, when eye is at this distance eyeProxmity factor will 1.")]
	[Range(0.1f,1.0f)]
	private float eyeMaxDistance = 0.2f; // When considered as part of the linear equation this value represents PT2_X.

	[SerializeField] 
	[Tooltip("Linear start distance from lens surface, when eye is at this distance eyeProxmity factor will eyeMinIntensity.")]
	[Range(0.0f,0.25f)]
	private float eyeMinDistance = 0.1f; // When considered as part of the linear equation this value represents PT1_X.

	[SerializeField] 
	[Tooltip("The minimum intensity (higher values mean effects will be more noticable as lens sruface approaches player's eye).")]
	[Range(0.0f,1.0f)]
	private float eyeMinIntensity = 0.5f; // When considered as part of the linear equation this value represents PT1_Y.

	[SerializeField] 
	[Tooltip("Multiplier for moving the center of the scope shadow (Vignetting) relative to the player's eye.")]
	[Range(0.0f,100.0f)]
	private int scopeShadowVelocity = 50;

	[SerializeField]
	[Tooltip("Multiplier which affects the amount that the image in the lens will appear to bend as player's eye is moved off center of eyepeice.")]
	[Range(0.0f,1.0f)]
	private float lensRefractionVelocity = 0.2f;

	[SerializeField]
	[Tooltip("The material used by Blit to overlay the reticle.")]
	private Material reticleMaterial;

	[SerializeField]
	[Tooltip("The texture used by Blit to overlay the reticle.")]
	private Texture reticleTexture;

	private Camera cam;
	private Transform cameraParent; // The parent object so we can use its position to calculat local position of player's eye relative to the plane with the renter texture.
	private PostProcessingBehaviour post;
	private VignetteModel.Settings vignette; // Get the vignette from the post processing profile.

	private Vector3 localEyePos; // The selected player's eye in local space.
	private Vector3 eyePosition; // The selected player's eye in world space.

	private float slopeProx; // Predetermined slope computed by the max and min eye distance.
	private float yInterceptProx; // The point on the lone where vignetteIntensity (aka the x axis) is equal to zero (or where the render texture plane is actually touching the player's eye).

	private float eyeProxmity; // How much vignetteing to apply to this eye's render texture.
	private Vector2 vignetteCenter; //As eye moves around, center will be moved aswell.

	private bool enableReticule; // Will be set true if there is both a texture and material present.

	//(De)Initialization:
	private void Start () 
	{
		post = GetComponent<PostProcessingBehaviour> ();
		post.profile.vignette.enabled = true;
		vignette = post.profile.vignette.settings;

		enableReticule = reticleMaterial != null && reticleTexture != null;

		// Determine which VRNode to use based on selected eye for the attached camera.
		var isDefaultEye = EyeSelect == EyeSelector.left;
		eye = isDefaultEye ? VRNode.LeftEye : VRNode.RightEye;

		// Precompute the constants for the linear equation:
		//    m   = (PT2_Y -    PT1_Y      ) / (    PT2_X      -      PT1_X    ) 
		slopeProx = (1.0f - eyeMinIntensity) / (eyeMaxDistance - eyeMinDistance); // Precomputed slope: m = (y2-y1) / (x2-x1)

		//    b        =     PT1_Y       - (    m     *     PT1_X     )
		yInterceptProx = eyeMinIntensity - (slopeProx * eyeMinDistance); // Given the slope, substitue a value for PT1 and solve for Y.

		//Debug and print the constants for the linear equation:
		//Debug.Log("y2=" + 1 + " y1=" + eyeMinIntensity + " x2=" + eyeMaxDistance + " x1=" + eyeMinDistance);
		//Debug.Log("m=" + slopeProx + " b=" + yInterceptProx);
	}

	private void OnEnable () 
	{
		cam = GetComponent<Camera> ();
		cam.enabled = true;
		cameraParent = GameObject.FindGameObjectWithTag ("MainCamera").transform.parent;
	}

	private void OnDestroy() 
	{
		post.profile.vignette.enabled = false;
	}

	private void Update () 
	{
		if (!cam.enabled) 
		{
			UpdateEye ();
		}
		cam.enabled = eyeProxmity <= 5 + eyeMaxDistance;
	}


	// Application:
	private void UpdateEye () 
	{
		UpdateEyePosition ();
		UpdateEyeProximity ();
	}

	private void UpdateEyePosition () 
	{
		eyePosition = cameraParent.TransformPoint (InputTracking.GetLocalPosition (eye));
	}

	private void UpdateEyeProximity () 
	{
		eyeProxmity = Vector3.Distance (centerSurfacePoint.position, eyePosition);

		switch(scopeMode) 
		{
		case ScopeMode.traverse :
			eyeProxmity = eyeProxmity < eyeMinDistance ? eyeMinDistance + (eyeMinDistance - eyeProxmity) : eyeProxmity;
			break;
		case ScopeMode.locked :
			eyeProxmity = eyeProxmity < eyeMinDistance ? eyeMinDistance : eyeProxmity;
			break;
		}

		//Debug.Log("X IN=" + eyeProxmity); // Debug and print the input value X for the linear equation.

		//Plug all values into the linear eqution and generate a value for eyeProxmity (keep in mind that eyeProxmity represents Y).
		eyeProxmity = (slopeProx * eyeProxmity) + yInterceptProx; // The linear equation: y = m*x+b
		//Debug.Log("Y OUT=" + eyeProxmity); // Debug and print the output value Y from the linear equation.
	}


	//Postprocessing Helpers:	
	private void ComputeDeltas() 
	{

		localEyePos = centerSurfacePoint.InverseTransformPoint (eyePosition);

		vignetteCenter.x = (localEyePos.x * scopeShadowVelocity) + 0.5f;
		vignetteCenter.y = (localEyePos.y * scopeShadowVelocity) + 0.5f;

		localEyePos.z = 0; // Don't allow the camera to move in and out from the scope's objective lens.
		transform.localPosition = localEyePos * lensRefractionVelocity; // Move the camera to align with the camera eye and mimic a lens refraction effect.
	}

	private void DoLensEffect() 
	{
		vignette.center.x = vignetteCenter.x;
		vignette.center.y = vignetteCenter.y;
		vignette.intensity = Mathf.Abs(eyeProxmity);

		post.profile.vignette.settings = vignette;
	}


	//Rendering:
	void OnRenderImage (RenderTexture source, RenderTexture destination)
	{	// Postprocess the image and add the reticle to the output buffers.
		Graphics.Blit (source, destination); // first apply the renderTexture from all lower depth cameras.

		if (enableReticule) { // If there is a texture and material present.
			Graphics.Blit (reticleTexture, destination, reticleMaterial); // Now apply the reticle texture to the output buffers.
		}
	}

	private void OnPreRender ()
	{
		UpdateEye ();
		ComputeDeltas ();
		DoLensEffect (); 
	}

}*/