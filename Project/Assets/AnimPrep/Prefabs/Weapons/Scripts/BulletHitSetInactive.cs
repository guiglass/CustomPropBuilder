using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor;

public class BulletHitSetInactive : BulletHitBase {

	/*[SerializeField] GameObject LandHitExplosion;
	[SerializeField] GameObject WaterHitExplosion;

	private GameObject m_HitExplosion;

	void OnDestroy () {
		if (m_HitExplosion != null) {
			Destroy (m_HitExplosion);
		}

	}

	void StartParticleSystems (Transform t) {
		foreach (Transform child in t) {
			var parts = child.GetComponent<ParticleSystem> ();
			parts.Stop ();
			parts.Play ();
		}

	}


	void CreateHitExplosion (GameObject explosionType) {

		if (m_HitExplosion == null) {
			m_HitExplosion = Instantiate (explosionType);
		} else if (m_HitExplosion.name != explosionType.name+"(Clone)") {
			Destroy (m_HitExplosion);
			m_HitExplosion = Instantiate (explosionType);
		}

		m_HitExplosion.transform.position = transform.position;
		StartParticleSystems (m_HitExplosion.transform);
	}

	void OnCollisionEnter (Collision col) {
		var otherCollider = col.contacts [0].otherCollider;
		switch (otherCollider.tag) 
		{
		case "Water":
			CreateHitExplosion (WaterHitExplosion);
			break;

		case "Land":
			CreateHitExplosion (LandHitExplosion);
			break;

		default:
			otherCollider.transform.SendMessage ("HitDamage", null, SendMessageOptions.DontRequireReceiver);
			break;
		}
			

		this.gameObject.SetActive(false);
	}
*/

	public int bulletLifeSeconds = 5;
	protected override void OnEnable () {
		if (deactivateBulletRoutine != null) {
			StopCoroutine (deactivateBulletRoutine);
		}
		deactivateBulletRoutine = StartCoroutine (DeactivateBulletRoutine());
	}

	Coroutine deactivateBulletRoutine;
	IEnumerator DeactivateBulletRoutine() {
		yield return new WaitForSeconds (bulletLifeSeconds);
		//gameObject.GetComponent<TrailRenderer> ().enabled = false;
		gameObject.SetActive (false);
	}


	public override void OnCollisionEnter (Collision col)
	{
		base.OnCollisionEnter (col);
		this.gameObject.SetActive(false);
	}
}
