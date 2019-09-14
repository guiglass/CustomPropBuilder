using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletHitHealthDamage : MonoBehaviour {

	private Collider m_collider;
	public Collider collider { get { return m_collider; } }

	////[SerializeField]
	////StairDismount ragdoll;

	[SerializeField] 
	private GameObject m_hitExplosion;
	public GameObject getHitExplosion { get { return m_hitExplosion; } }

	[SerializeField] 
	private int m_damageMultiplier = 1;
	public int getDamageMultiplier { get { return m_damageMultiplier; } }

	private void Start() {
		m_collider = GetComponent<Collider>();
	}

	public void TakeDamageRagdoll(Vector3 force, Collider col) {
		////ragdoll.RagdollImpact (force, col);
	}

	public void TakeDamage(int amount) {
		////transform.root.GetComponent<PlayerHealth>().TakeDamage(amount * m_damageMultiplier);
	}
}
