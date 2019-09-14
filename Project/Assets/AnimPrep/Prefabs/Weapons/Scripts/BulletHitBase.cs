using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;


public class BulletHitBase : MonoBehaviour {

	Rigidbody rb;

	//Vector3 startForward; //Used as the forward direction of the projectile's path for calculating the closest passing distance to the player for flyby sound effect.
	Vector3 startVelocity; //Used as the forward direction of the projectile's path for calculating the closest passing distance to the player for flyby sound effect.
	/*
	private Transform m_localPlayer; // The parent object so we can use its position to calculat local position of player's eye relative to the plane with the renter texture.
	private Transform localPlayer { 
		get { 
			if (m_localPlayer == null) {
				m_localPlayer = GameObject.FindGameObjectWithTag ("VRLocalPlayer").transform;
			}
			return m_localPlayer; 
		}
	}*/

	/*
	private LocalPlayerDamagePreRender m_localDamage; // The parent object so we can use its position to calculat local position of player's eye relative to the plane with the renter texture.
	private LocalPlayerDamagePreRender localDamage { 
		get { 
			if (m_localDamage == null) {
				m_localDamage = GameObject.FindGameObjectWithTag ("MainCamera").GetComponent<LocalPlayerDamagePreRender> ();
			}
			return m_localDamage; 
		}
	}
	*/
	public ParticleSystem blood;
	[SerializeField] GameObject LandHitExplosion;
	[SerializeField] GameObject WaterHitExplosion;

	[SerializeField]
	private int hitDamageAmount = 10;

	[SerializeField]
	private float explosionDestroyDelay = 0;
	public GameObject m_HitExplosion = null;

	private void Awake() {
		rb = GetComponent<Rigidbody> ();
		rb.useGravity = false;
	}

	protected virtual void OnEnable () {
		//startForward = transform.forward;
		startVelocity = rb.velocity;
	}

	void FixedUpdate () {
		rb.AddForce (Physics.gravity, ForceMode.Acceleration);
	}

	void OnDestroy () {
		if (m_HitExplosion != null) {
			Destroy (m_HitExplosion, explosionDestroyDelay);
		}
	}

	void StartParticleSystems (Transform t) {
		foreach (Transform child in t) {
			var parts = child.GetComponent<ParticleSystem> ();
			parts.Stop ();

			ParticleSystem.MainModule newMain = parts.main;
			newMain.simulationSpeed = 1;

			parts.Play ();
		}
	}

	void CreateHitExplosion (GameObject explosionType) {
		if (explosionType == null) {
			return;
		}
		if (m_HitExplosion == null) {
			m_HitExplosion = Instantiate (explosionType);
		} else if (m_HitExplosion.name != explosionType.name+"(Clone)") {
			Destroy (m_HitExplosion);
			m_HitExplosion = Instantiate (explosionType);
		}
		m_HitExplosion.transform.parent = transform.parent;
		m_HitExplosion.transform.position = transform.position;

		StartParticleSystems (m_HitExplosion.transform);
	}




	public virtual void OnCollisionEnter (Collision col) {

		switch (col.gameObject.layer) {

		case 0://Default
			CreateHitExplosion (LandHitExplosion);
			break;

		case 4://Water
			CreateHitExplosion (WaterHitExplosion);
			break;

		case 11://Ragdoll
			var hitPoint = col.contacts [0].point;
			var direction = Vector3.forward;// transform.position - col.transform.position;

			var _blood = Instantiate (blood);

			Transform[] allChildren = _blood.GetComponentsInChildren<Transform>();
			foreach (Transform child in allChildren) {//ensure all particle systems are set to layer zero (default) so they will be rendered and visible to camcorders
				child.gameObject.layer = 0;
			}

			_blood.transform.position = hitPoint;
			_blood.transform.rotation = Quaternion.LookRotation (-direction);
			_blood.Emit (5);

			foreach (Transform child in _blood.transform) {
				var parts = child.GetComponent<ParticleSystem> ();
				ParticleSystem.MainModule newMain = parts.main;
				newMain.simulationSpeed = 1;
			}

			Destroy (_blood.gameObject, 5);
			break;

		default:
			
			var proximityDamage = col.transform.GetComponent<ExplosionHitBase> ();
			if (proximityDamage != null) { //this is something else, check to see if it can receive damage
				proximityDamage.Detonate();
				break;
			}

			break;
		}
	}


}
