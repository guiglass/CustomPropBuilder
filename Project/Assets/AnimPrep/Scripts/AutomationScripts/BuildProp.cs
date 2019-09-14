using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

#if UNITY_EDITOR
using UnityEditor;

public class BuildProp : MonoBehaviour {

	void UpdateMyFormatedName (string assetBundleName) {
		string formatName = "Prop_{0}";

		string prefabName = string.Format(formatName, assetBundleName);

		transform.name = prefabName;
	}

	public void CreateEmptyContianers(string modelAssetName) {
		gameObject.SetActive (true);

		gameObject.AddComponent<PropLinker> ();


		List<Transform> allChildren = new List<Transform>();
		allChildren.Add (transform);
		for (int i = 0; i < transform.childCount; i++) {
			var child = transform.GetChild (i);
			allChildren.Add (child);
		}
		foreach (Transform child in allChildren) {

			if (child.GetComponent<Renderer> ()) {
				child.gameObject.AddComponent<BoxCollider> ();
			}

		}

		var rb = gameObject.AddComponent<Rigidbody> ();
		rb.isKinematic = true;

		//SteamVR interaction system
		var interactable = gameObject.AddComponent<Interactable> ();
		interactable.hideHandOnAttach = false;
		interactable.hideSkeletonOnAttach = false;
		interactable.hideControllerOnAttach = true;
		interactable.handFollowTransform = false;

		var velocityEst = gameObject.AddComponent<VelocityEstimator> ();
		var throwable = gameObject.AddComponent<Throwable> ();
		throwable.restoreOriginalParent = true;


		var controllerOffset = new GameObject ("ControllerOffset").transform;
		controllerOffset.parent = transform;

		UpdateMyFormatedName (modelAssetName);

	}




}
#endif