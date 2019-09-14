using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionHitBase : MonoBehaviour {

	[SerializeField] private float detectRadius = 1f;

	[SerializeField] private float blastRadius = 10f;

	[SerializeField] private GameObject explosionHelper;

	public Coroutine delayedDetonateRoutine;

	private Transform m_localPlayer; // The parent object so we can use its position to calculat local position of player's eye relative to the plane with the renter texture.
	private Transform localPlayer { 
		get { 
			var _localPlayer = GameObject.FindGameObjectWithTag ("VRLocalPlayer");
			if (m_localPlayer == null && _localPlayer != null) {
				m_localPlayer = _localPlayer.transform;
			}
			return m_localPlayer; 
		}
	}
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
	void OnEnable() {
		var pitch = Random.Range (0.9f, 1.1f);
		foreach (AudioSource audio in explosionHelper.GetComponentsInChildren<AudioSource>(true)) {
			audio.pitch = pitch;
		}
		InvokeRepeating ("CheckProximity", 0, 0.5f);
		//AreaDamage (transform.position, 10, 5);
	}
		
	[SerializeField] bool detonate = false;
	void Update () {
		if (detonate) {
			detonate = false;
			Detonate ();
		}
	} 

	public void Detonate() {
		if (explosionHelper.activeSelf) {
			explosionHelper.SetActive (false);
		}
		explosionHelper.SetActive (true);
		AreaDamage (transform.position, blastRadius, 5);
		CreateCrater ();
		Destroy (gameObject);
	}

	public void DelayedDetonate(float range) {
		if (delayedDetonateRoutine != null) {
			StopCoroutine (delayedDetonateRoutine);
			delayedDetonateRoutine = null;
			return;
		}

		if (Random.Range (0, 100) < 75) { //sometimes they don't set eachother off...
			return;
		}
		var delay = Random.Range(1f, 5* range);
		delayedDetonateRoutine = StartCoroutine (DelayedDetonateRoutine(delay));
	}
	IEnumerator DelayedDetonateRoutine (float delay) {
		yield return new WaitForSeconds (delay);
		Detonate ();
	}

	void CheckProximity() {
		Collider[] objectsInRange = Physics.OverlapSphere(transform.position, detectRadius);
		foreach (Collider col in objectsInRange) 
		{
			var hitDamage = col.transform.GetComponent<BulletHitHealthDamage> ();
			if (hitDamage != null) {
				Detonate ();
			}
		}
	}

	void AreaDamage(Vector3 location, float radius, float damage)
	{
		Collider[] objectsInRange = Physics.OverlapSphere(location, radius);
		foreach (Collider col in objectsInRange)
		{

			if (col.transform.IsChildOf(transform.parent)) {
				continue;
			}
			/*Enemy enemy = col.GetComponent<Enemy>();
			if (enemy != null)
			{
				// linear falloff of effect
				float proximity = (location - enemy.transform.position).magnitude;
				float effect = 1 - (proximity / radius);

				enemy.ApplyDamage(damage * effect);
			}*/


			switch (col.transform.tag) {
			/*
			case "Water":
				CreateHitExplosion (WaterHitExplosion);
				break;

			case "Land":
				CreateHitExplosion (LandHitExplosion);
				break;

			case "Drivable":
				CreateHitExplosion (LandHitExplosion);
				break;
			*/
			default:
				/*
			if (col.gameObject.layer == LayerMask.NameToLayer ("PlayerAvatarRig")) {
				var damage = col.transform.GetComponent<BulletHitHealthDamage> ();
				damage.TakeDamage (hitDamageAmount);
				CreateHitExplosion (damage.getHitExplosion);
			}*/
				//print (col.transform.name);
				//var force = startVelocity * rb.mass;

				var range = 1 - (Mathf.Clamp ((col.transform.position - transform.position).magnitude, 0, radius) / radius);
				var force = (col.transform.position - transform.position).normalized * range * damage;
				//print (col.name + " " + range);

				var _localPlayer = localPlayer;
				if (_localPlayer != null && col.transform.IsChildOf (_localPlayer)) { //If the collider was part of the VRLocalPlayer
					//localDamage.StartHitRoutine (force.normalized);
					break;
				}

				var proximityDamage = col.transform.GetComponent<ExplosionHitBase> ();
				if (proximityDamage != null) { //this is something else, check to see if it can receive damage
					proximityDamage.DelayedDetonate(range);
					break;
				}
				var hitDamage = col.transform.GetComponent<BulletHitHealthDamage> ();
				if (hitDamage != null) { //this is something else, check to see if it can receive damage
					//print ("RB Mass " + rb.mass + " RB magnitude " + rb.velocity.magnitude + " RB Vel " + rb.velocity);
					hitDamage.TakeDamageRagdoll (force, col);
					break;
				}

				break;
			}
		}
	}


	//https://answers.unity.com/questions/211544/making-duplicate-terrain-unique.html
	//https://answers.unity.com/questions/11093/modifying-terrain-height-under-a-gameobject-at-run.html

	[SerializeField] private int terrainCraterWidth = 5; // the diameter of terrain portion that will raise under the game object float desiredHeight = 0; // the height we want that portion of terrain to be

	[SerializeField] private float terrainCraterDepth = 0.1f;

	public static Terrain crater_terr; // terrain to modify int hmWidth; // heightmap width int hmHeight; // heightmap height
	public static TerrainData crater_newTerrainData;
	public static TerrainCollider crater_tc; 

	public static int crater_hmWidth;
	public static int crater_hmHeight;

	int crater_posXInTerrain; // position of the game object in terrain width (x axis) int posYInTerrain; // position of the game object in terrain height (z axis)
	int crater_posYInTerrain; // position of the game object in terrain width (x axis) int posYInTerrain; // position of the game object in terrain height (z axis)

	void Start () {
		if (crater_newTerrainData == null) { //was first to see the public static var, so let this instance set up the values.
			var oldTerr = Terrain.activeTerrain;
			crater_terr = (Terrain)Object.Instantiate (oldTerr);
			crater_newTerrainData = (TerrainData)Object.Instantiate (crater_terr.terrainData);
			crater_terr.terrainData = crater_newTerrainData;

			crater_terr.transform.position = oldTerr.transform.position;
			oldTerr.gameObject.SetActive (false);

			crater_hmWidth = crater_terr.terrainData.heightmapWidth;
			crater_hmHeight = crater_terr.terrainData.heightmapHeight;

			crater_tc = crater_terr.gameObject.GetComponent<TerrainCollider> ();
			crater_tc.terrainData = crater_newTerrainData;

			terrainCraterDepth = Mathf.Clamp (terrainCraterDepth, -1.0f, 1.0f);
			print ("Crater info: depth " + (terrainCraterDepth) + " meters (" + (terrainCraterDepth / crater_newTerrainData.size.y * 100.0) + "%)");
		}
	}

	void CreateCrater() {
		// get the normalized position of this game object relative to the terrain
		Vector3 tempCoord = (transform.position - crater_terr.gameObject.transform.position);
		Vector3 coord;
		coord.x = tempCoord.x / crater_newTerrainData.size.x;
		coord.y = tempCoord.y / crater_newTerrainData.size.y;
		coord.z = tempCoord.z / crater_newTerrainData.size.z;

		// get the position of the terrain heightmap where this game object is
		crater_posXInTerrain = (int) (coord.x * crater_hmWidth); 
		crater_posYInTerrain = (int) (coord.z * crater_hmHeight);

		// we set an offset so that all the raising terrain is under this game object
		int offset = terrainCraterWidth / 2;
		// get the heights of the terrain under this game object
		float[,] heights = crater_newTerrainData.GetHeights(crater_posXInTerrain-offset,crater_posYInTerrain-offset,terrainCraterWidth,terrainCraterWidth);

		print (heights.Length + " " + heights[0,0]);
		// we set each sample of the terrain in the size to the desired height
		for (int i=0; i < terrainCraterWidth; i++)
			for (int j=0; j < terrainCraterWidth; j++)
				heights[i,j] -= terrainCraterDepth/crater_newTerrainData.size.y;
		// set the new height
		crater_newTerrainData.SetHeights(crater_posXInTerrain-offset,crater_posYInTerrain-offset,heights);

		//AstarPath.active.UpdateGraphs (new Bounds(transform.position, Vector3.one * terrainCraterWidth * 2));
	}

}

