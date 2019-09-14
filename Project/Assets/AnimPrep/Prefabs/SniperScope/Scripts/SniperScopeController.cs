using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.PostProcessing;

namespace VR_SniperScope
	{
	public class SniperScopeController : MonoBehaviour {

		[SerializeField]
		AudioClip changeZoomClip;

		public enum ZoomLevel {
			low,
			mid,
			high
		}

		int[] fovLevels = new int[]{15, 5, 2};

		public ZoomLevel zoom = ZoomLevel.low;

		protected static int mod(int k, int n) {  return ((k %= n) < 0) ? k+n : k;  } //https://stackoverflow.com/a/23214321/3961748  (Modulo for negative numbers)

		public static AudioSource PlayOneshotAudio(AudioClip clip, float volume = 1f, float pitch = 1f, bool play = true, bool bypassEffects = false) {
			var obj = new GameObject ();
			obj.name = "oneshot_audio";
			var oneshotAudio = obj.AddComponent<AudioSource> ();
			oneshotAudio.clip = clip;
			oneshotAudio.volume = volume;
			oneshotAudio.pitch = pitch;

			oneshotAudio.bypassReverbZones = bypassEffects;
			oneshotAudio.bypassListenerEffects = bypassEffects;
			oneshotAudio.bypassEffects = bypassEffects;

			if (play) {
				oneshotAudio.Play ();
			}
			Destroy(oneshotAudio.gameObject, clip.length * (1f/pitch));
			return oneshotAudio;
		}


		public void OnScopeZoomChanged() {
			var cnt = System.Enum.GetValues (typeof(ZoomLevel)).Length;
			zoom = (ZoomLevel)mod ((int)zoom + 1, cnt);

			m_camera.fieldOfView = fovLevels[(int)zoom];

			PlayOneshotAudio (changeZoomClip);
		}

		void Awake() {
			m_camera.fieldOfView = fovLevels[(int)zoom];//set the default fov
			//m_camera.cullingMask = Instance.propCameraCullingMask;
			m_camera.nearClipPlane = 0.75f;
			m_camera.farClipPlane = 10000;
		}



		[SerializeField]
		[Tooltip("The scope camera.")]
		private Camera m_camera;

		[SerializeField] 
		[Tooltip("The uv unwrapped lens mesh, origin is also the surface center point where if player's eye proximity would equal zero if touching it.")]
		private Transform eyePieceLens;

		[SerializeField] 
		[Tooltip("The shader that will be used to create and apply a material to the eyepiece, this shader must accept _LeftEyeTexture and _RightEyeTexture variables.")]
		private Shader eyePieceShader;

		[SerializeField] 
		[Tooltip("The shader that will be used to draw the vignette (scope shadow) circle and adjust it's intensity and center, this shader must accept the _Center variable.")]
		private Shader vignetteShader;

		[SerializeField] 
		[Tooltip("Shader responsible for bluring the edges of the vignette (scope shadow) circle.")]
		private Shader separableBlurShader;

		[SerializeField] 
		[Tooltip("Shader for adding chromatic aberration to the vignette and reticle if present, but is not used in the example setup.")]
		private Shader chromAberrationShader;

		[SerializeField]
		[Tooltip("The material used by Blit to overlay the reticle.")]
		private Material reticleMaterial;

		[SerializeField]
		[Tooltip("The texture used by Blit to overlay the reticle.")]
		private Texture reticleTexture;

		[SerializeField]
		[Tooltip("The profile to be applied to the scope camera for final post processing.")]
		private PostProcessingProfile postProcessingProfile;

		//Variable Declarations:
		private enum ScopeMode
		{
			traverse,locked,bypass
		}
		[SerializeField] 
		[Tooltip("Traverse: FX go through minIntensity.\nLocked: FX go to minIntensity.\nBypass: Don't compute.")]
		ScopeMode scopeMode; // Default is traverse

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


		private Vector3 cameraInitialPosition;
		private UnityEngine.XR.XRNode activeEye; // The VRNode (left right eye) that is currently closest to the centerSurfacePoint.

		private SniperScopeVignetting vignetteLeft; // Get the vignette from the post processing profile.
		private SniperScopeVignetting vignetteRight; // Get the vignette from the post processing profile.

		private Vector3 localEyePos; // The selected player's eye in local space.
		private Vector3 eyePosition; // The selected player's eye in world space.

		private float slopeProx; // Predetermined slope computed by the max and min eye distance.
		private float yInterceptProx; // The point on the lone where vignetteIntensity (aka the x axis) is equal to zero (or where the render texture plane is actually touching the player's eye).

		private Vector2 vignetteCenter; //As eye moves around, center will be moved aswell.
		private bool enableReticule; // Will be set true if there is both a texture and material present.


		//(De)Initialization:
		private void OnEnable () 
		{
			cameraInitialPosition = m_camera.transform.localPosition;

			var leftEyeRenderTexture = CreateNewRenderTexure ();
			var rightEyeRenderTexture = CreateNewRenderTexure ();

			var renderer = eyePieceLens.GetComponent<MeshRenderer> ();
			renderer.material = new Material (eyePieceShader);
			renderer.material.SetTexture("_LeftEyeTexture", leftEyeRenderTexture);
			renderer.material.SetTexture("_RightEyeTexture", rightEyeRenderTexture);


			// Setup the postprocessing stack (initialization order matters!!).
			if (!m_camera.gameObject.GetComponent<PostProcessingBehaviour> ()) {
				InitializePostProcessingProfile ();
			}

			if (reticleMaterial != null && reticleTexture != null) {
				InitializeReticle ();
			}

			vignetteRight = InitializeVignette (rightEyeRenderTexture);
			vignetteLeft = InitializeVignette (leftEyeRenderTexture);

			m_camera.enabled = true;
			m_camera.targetTexture = leftEyeRenderTexture; //just set this so OpenVR doesn't take the camera away.

			//playerCameraParent = GameObject.FindGameObjectWithTag ("MainCamera").transform.parent;

			// Precompute the constants for the linear equation:
			//    m   = (PT2_Y -    PT1_Y      ) / (    PT2_X      -      PT1_X    ) 
			slopeProx = (1.0f - eyeMinIntensity) / (eyeMaxDistance - eyeMinDistance); // Precomputed slope: m = (y2-y1) / (x2-x1)

			//    b        =     PT1_Y       - (    m     *     PT1_X     )
			yInterceptProx = eyeMinIntensity - (slopeProx * eyeMinDistance); // Given the slope, substitue a value for PT1 and solve for Y.

			//Debug and print the constants for the linear equation:
			//Debug.Log("y2=" + 1 + " y1=" + eyeMinIntensity + " x2=" + eyeMaxDistance + " x1=" + eyeMinDistance);
			//Debug.Log("m=" + slopeProx + " b=" + yInterceptProx);
		}
		// Application:
		private void Update () 
		{

			if (Camera.main == null) {
				return;
			}

			var leftEyePos = GetEyePosition (UnityEngine.XR.XRNode.LeftEye);
			var rightEyePos = GetEyePosition (UnityEngine.XR.XRNode.RightEye);

			var leftEyeProx = GetEyeProximity (leftEyePos);
			var rightEyeProx = GetEyeProximity (rightEyePos);

			activeEye = leftEyeProx > rightEyeProx ? UnityEngine.XR.XRNode.LeftEye : UnityEngine.XR.XRNode.RightEye;
			float eyeProxmity = activeEye == UnityEngine.XR.XRNode.LeftEye ? leftEyeProx : rightEyeProx;
			//Debug.Log("X IN=" + eyeProxmity); // Debug and print the input value X for the linear equation.

			if (eyeProxmity <= 5 + eyeMaxDistance) {

				m_camera.enabled = true;

				eyeProxmity = GetLinearResult (eyeProxmity);
				//Debug.Log("Y OUT=" + eyeProxmity); // Debug and print the output value Y from the linear equation.

				ComputeDeltas (leftEyePos, activeEye == UnityEngine.XR.XRNode.LeftEye);
				DoLensEffect (vignetteLeft, leftEyeProx); 

				ComputeDeltas (rightEyePos, activeEye == UnityEngine.XR.XRNode.RightEye);
				DoLensEffect (vignetteRight, rightEyeProx); 

			} else {
				m_camera.transform.localPosition = cameraInitialPosition; // Move the camera to align with the camera eye and mimic a lens refraction effect.
				m_camera.enabled = false;
			}
		}


		private RenderTexture CreateNewRenderTexure() {
			var rt = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB64);
			rt.Create();
			return rt;
		}



		private Vector3 GetEyePosition (UnityEngine.XR.XRNode eye) 
		{
			return  Camera.main.transform.parent.TransformPoint (UnityEngine.XR.InputTracking.GetLocalPosition (eye));
		}

		private float GetEyeProximity (Vector3 eyePos) 
		{
			float eyeProxmity = Vector3.Distance (eyePieceLens.position, eyePos);

			switch(scopeMode) 
			{
			case ScopeMode.traverse :
				eyeProxmity = eyeProxmity < eyeMinDistance ? eyeMinDistance + (eyeMinDistance - eyeProxmity) : eyeProxmity;
				break;
			case ScopeMode.locked :
				eyeProxmity = eyeProxmity < eyeMinDistance ? eyeMinDistance : eyeProxmity;
				break;
			}

			return eyeProxmity;
		}

		private float GetLinearResult (float x) {
			//Plug all values into the linear eqution and generate a value for eyeProxmity (keep in mind that eyeProxmity represents Y).
			return (slopeProx * x) + yInterceptProx; // The linear equation: y = m*x+b
		}

		//Postprocessing Helpers:	
		private PostProcessingBehaviour InitializePostProcessingProfile () {

			var post = m_camera.gameObject.AddComponent<PostProcessingBehaviour> ();
			post.profile = postProcessingProfile;

			return post;
		}

		private SniperScopeReticle InitializeReticle () {
			var reticle = m_camera.gameObject.AddComponent<SniperScopeReticle> ();
			reticle.reticleTexture = reticleTexture;
			reticle.reticleMaterial = reticleMaterial;
			return reticle;
		}


		private SniperScopeVignetting InitializeVignette (RenderTexture texture) {
			var vignette = m_camera.gameObject.AddComponent<SniperScopeVignetting> ();
			vignette.vignetteShader = vignetteShader;
			vignette.separableBlurShader = separableBlurShader;
			vignette.chromAberrationShader = chromAberrationShader;
			vignette.texture = texture;
			return vignette;
		}


		private void ComputeDeltas(Vector3 eyePosition, bool moveCamera = false) 
		{
			localEyePos = eyePieceLens.InverseTransformPoint (eyePosition);

			vignetteCenter.x = (localEyePos.x * scopeShadowVelocity);
			vignetteCenter.y = (localEyePos.y * scopeShadowVelocity);

			localEyePos.z = 0; // Don't allow the camera to move in and out from the scope's objective lens.
			if (moveCamera) {
				m_camera.transform.localPosition = localEyePos * lensRefractionVelocity; // Move the camera to align with the camera eye and mimic a lens refraction effect.
			}
		}

		private void DoLensEffect(SniperScopeVignetting vignette, float eyeProxmity) 
		{
			vignette.center.x = vignetteCenter.x;
			vignette.center.y = vignetteCenter.y;
			vignette.intensity = Mathf.Abs(eyeProxmity);
		}

	}
}
