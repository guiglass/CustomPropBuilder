using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class BulletPlayerProximitySound : MonoBehaviour {

	Transform playersHead;
	Vector3 startForward; //Used as the forward direction of the projectile's path for calculating the closest passing distance to the player for flyby sound effect.

	public AudioClip[] flybysound;

	[SerializeField] private float radius = 5.0f;
	//private AudioSource m_audio;

	/*
	private void Start () {
		var globalAudio = GameObject.Find ("[GlobalAudioSource]");
		if (globalAudio == null) {
			Debug.LogWarning ("There is no [GlobalAudioSource] object in the scene");
			this.enabled = false;
			return;
		}
		//m_audio = GameObject.Find ("[GlobalAudioSource]").GetComponent<AudioSource> ();
	}
	*/

	private void OnEnable () {
		startForward = transform.forward;
		playersHead = Player.instance.headCollider.transform;// VRTK_DeviceFinder.HeadsetTransform();
		played = false;
	}

	private Vector3 projectedPoint;
	private Vector3 gizmoLinePos0; //used only by the gizmos
	private Vector3 gizmoLinePos1; //used only by the gizmos

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

	private bool played = false;
	private float distanceToPlayer;
	private void FixedUpdate () {

		if (playersHead == null) {
			return;
		}

		distanceToPlayer = Vector3.Distance (playersHead.position, transform.position);

		if (!played && distanceToPlayer <= radius) {
			played = true;
			//Get the distance from the closest point of a ray that passes through a sphere and use it to adjust the volume of the flyby sound effect.
			//https://forum.unity.com/threads/computing-smallest-distance-from-center-of-a-sphere-to-a-ray-passing-through-it.501217/

			projectedPoint = ProjectPointOnLine(transform.position, startForward.normalized, playersHead.position); 
			var distance = Vector3.Distance (projectedPoint, playersHead.position);


			var playerFacing = transform.position - playersHead.position;
			var frontal = Mathf.Clamp01(1f - Vector3.Dot (playerFacing, startForward) );


			var volume = frontal * Mathf.Clamp01((radius - distance) / radius); //based on distance from flyby's closest point, adjust volume from 0 to 1

			if (flybysound.Length > 0) {
				PlayOneshotAudio (flybysound [Random.Range (0, flybysound.Length)], volume, 1);
			}
			//m_audio.clip = flybysound[Random.Range(0, flybysound.Length)];
			//m_audio.Play ();



			Debug.DrawRay (transform.position, Vector3.up * frontal, Color.red);
			//gizmoLinePos0 = transform.position; //used only by the gizmos
			//gizmoLinePos1 = transform.position + startForward.normalized * 20; //used only by the gizmos
		}
	}

	//http://wiki.unity3d.com/index.php?title=3d_Math_functions
	//This function returns a point which is a projection from a point to a line.
	//The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
	public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point){		

		//get vector from point on line to point in space
		Vector3 linePointToPoint = point - linePoint;

		float t = Vector3.Dot(linePointToPoint, lineVec);

		return linePoint + lineVec * t;
	}

	/*
	void OnDrawGizmos() {
		if (played) {
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(gizmoLinePos0, gizmoLinePos1);

			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere (projectedPoint, 1);
		}
	}*/
}
