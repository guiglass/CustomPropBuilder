using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using Valve.VR;
using UnityEngine.Events;
using VR_SniperScope;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor( typeof( FirearmLinker ) )]
public class FirearmLinkerEditor : Editor
{
	public float arrowSize = 1;

	void OnSceneGUI( )
	{
		FirearmLinker t = target as FirearmLinker;

		Handles.color = Color.white;
		Handles.ArrowCap( 0, t.transform.position, t.transform.rotation , arrowSize ); //the firing point forward indicator arrow
	}
}
#endif

public class FirearmLinker : MonoBehaviour
{
	[Header ("Prefabs")]
	public GameObject muzzleFlashParticlesPrefab;
	public GameObject bulletPrefab;
	public GameObject bulletTracerPrefab;

	[Header ("Muzzle sounds FX")]
	public AudioClip[] firingSoundsNear;
	public AudioClip[] firingSoundsDistant;

	public AudioClip[] flybysound;

	[Header ("Accessorie sounds FX")]
	public AudioClip changeModeClip;

	[Header ("Audio Sources")]
	public AudioSource audioNear;
	public AudioSource audioDistant;

	public int bulletForwardVelocity = 500; //M/sec
	public float firingRate = 0.1f;

	[Header ("Interactable Firearm")]
	public SteamVR_Action_Boolean firearm_fire;
	public SteamVR_Action_Boolean firearm_mode;
	public SteamVR_Action_Boolean firearm_zoom;




	//Functional Helper Script
	Interactable interactable;
	private Coroutine firingRoutine;

	public UnityEvent onScopeZoomLevelChanged = new UnityEvent ();

	protected static int mod(int k, int n) {  return ((k %= n) < 0) ? k+n : k;  } //https://stackoverflow.com/a/23214321/3961748  (Modulo for negative numbers)

	int bulletsIndex = 0;
	const int maxBullets = 16;
	const int tracerIncrement = 4;

	public enum FiringMode {
		single,
		three,
		auto
	}
	public FiringMode mode = FiringMode.single;

	private List<GameObject> bulletPrefabsList = new List<GameObject>();
	protected void OnDisable () {
		foreach (GameObject obj in bulletPrefabsList) {
			if (obj != null) {// && !obj.activeSelf) {
				Destroy (obj);
			}
		}
		bulletPrefabsList = new List<GameObject>();
	}

	#if UNITY_EDITOR
	void Awake () {
		if (bulletPrefab != null) {
			bulletPrefab.SetActive (false);
		}
		if (bulletTracerPrefab != null) {
			bulletTracerPrefab.SetActive (false);
		}
		if (muzzleFlashParticlesPrefab != null) {
			muzzleFlashParticlesPrefab.SetActive (true);
		}

		if (audioNear != null) {
			audioNear.gameObject.SetActive (true);
		}
		if (audioDistant != null) {
			audioDistant.gameObject.SetActive (true);
		}

		if (!GameObject.Find ("BulletTargetPrefab")) {
			var bulletTargetPrefabPath = "Assets/AnimPrep/Prefabs/BulletTargetPrefab.prefab";

			UnityEngine.Object bulletTargetPrefab = AssetDatabase.LoadAssetAtPath(bulletTargetPrefabPath, typeof(GameObject));
			GameObject bulletTargetObject = GameObject.Instantiate(bulletTargetPrefab, Vector3.zero, Quaternion.identity) as GameObject;
			bulletTargetObject.name = bulletTargetPrefab.name;
		}

		var scopeController = GetComponentInChildren<SniperScopeController> ();
		if (scopeController != null) {
			onScopeZoomLevelChanged.AddListener (scopeController.OnScopeZoomChanged);
		}
	}
	#endif

	void OnEnable () {
		interactable = GetComponentInParent<Interactable> ();

		var bulletsContainer = new GameObject (transform.name + "_BulletsContainer");
		if (bulletPrefabsList.Count == 0) {
			for (int i = 0; i < maxBullets; i++) {
				GameObject bullet = Instantiate (
					bulletTracerPrefab == null || tracerIncrement < 1 ? 
					bulletPrefab : i % tracerIncrement == 1 ? 
					bulletTracerPrefab : bulletPrefab
				);
				bullet.transform.parent = bulletsContainer.transform;
				bullet.SetActive (false);
				bullet.GetComponent<BulletPlayerProximitySound> ().flybysound = flybysound;
				bulletPrefabsList.Add (bullet);

				//bullet.GetComponent<BulletHitBase>().firearmProp = this;
			}
		}
		bulletsIndex = 0;

	}

	protected void LateUpdate() {
		if (interactable.attachedToHand) {
			if (firearm_fire.GetStateDown (interactable.attachedToHand.handType)) {

				if (firingRoutine == null) {
					firingRoutine = StartCoroutine (DoTrigger (interactable.attachedToHand));
				}
			}

			if (firearm_zoom.GetStateDown (interactable.attachedToHand.handType)) {
				onScopeZoomLevelChanged.Invoke ();
			}

			if (firearm_mode.GetStateDown (interactable.attachedToHand.handType)) {
				var cnt = System.Enum.GetValues (typeof(FiringMode)).Length;
				mode = (FiringMode) mod ((int)mode + 1, cnt);

				PlayOneshotAudio (changeModeClip);
			}
		}
	}

	IEnumerator DoTrigger(Hand hand) {
		int fired = 0;

		while (isActiveAndEnabled && firearm_fire.GetState(hand.handType)) {
			yield return new WaitForEndOfFrame (); // Do this so the animator is finished and the hand is aligned with weapon, more accuracy and removes body animation noise

			PrepareFireBullet();

			fired += 1;

			switch (mode) {
			case FiringMode.single:
				firingRoutine = null;
				yield break;
				break;
			case FiringMode.three:
				if (fired >= 3) {
					firingRoutine = null;
					yield break;
				}
				break;
			case FiringMode.auto:
				break;
			}

			yield return new WaitForSeconds(firingRate);
		}
		firingRoutine = null;
	}

	public void PrepareFireBullet() {
		var forward = transform.forward;
		var position = transform.position + Vector3.Scale(transform.lossyScale, forward * 0.25f);

		FireBullet (position, forward);
	}


	public void FireBullet(Vector3 position, Vector3 forward) {

		GameObject bullet = bulletPrefabsList[bulletsIndex];
		bullet.SetActive(false);
		bullet.GetComponent<BulletPlayerProximitySound> ().enabled = true;//!isLocalPlayer || isAiPlayer;

		var trail = bullet.GetComponent<TrailRenderer> ();
		trail.Clear ();
		trail.time = Mathf.Min(5f, bullet.GetComponent<TrailRenderer> ().time);
		trail.enabled = true;

		bullet.transform.position = position;
		bullet.transform.forward = forward;

		var rb = bullet.GetComponent<Rigidbody> ();
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
		rb.velocity = forward * bulletForwardVelocity;


		StartParticleSystems (muzzleFlashParticlesPrefab.transform);

		bulletsIndex++;
		bulletsIndex %= maxBullets;

		bullet.SetActive(true);

		audioNear.clip = firingSoundsNear[Random.Range(0,firingSoundsNear.Length)];
		audioNear.Play ();

		audioDistant.clip = firingSoundsNear[Random.Range(0,firingSoundsDistant.Length)];
		audioDistant.Play ();	
	}

	void StartParticleSystems (Transform t) {
		foreach (Transform child in t) {
			var p = child.GetComponent<ParticleSystem> ();
			ParticleSystem.MainModule newMain = p.main;
			newMain.simulationSpeed = 1;
			p.Play();
		}
	}


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
}
