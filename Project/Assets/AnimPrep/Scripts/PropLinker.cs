using System.Collections;
using System.Collections.Generic;
using Valve.VR.InteractionSystem;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public static class MyExtensionMethods
{
	static Component GetComponentMethod<T> (this GameObject go)  where T : Component
	{
		//return go.GetComponent<T> (); //modifying this line to can change how components are located in the main object
		//return go.GetComponentInParent<T> (); //Alternate search option 1
		return go.GetComponentInChildren<T> (); //Alternate search option 2
	}

	public static void DestroyComponentInPrefab<T>(this GameObject go) where T : Component 
	{
		var component = go.GetComponentMethod<T> ();
		if (component == null) 
		{
			return;
		}
		bool isPrefabInstance = PrefabUtility.GetPrefabParent(component) != null && PrefabUtility.GetPrefabObject(component.transform) != null;
		if (isPrefabInstance) 
		{
			GameObject clone = GameObject.Instantiate(go) as GameObject;
			Object.DestroyImmediate (clone.GetComponentMethod<T> ().gameObject);
			try 
			{
				PrefabUtility.ReplacePrefab (clone, PrefabUtility.GetPrefabParent (go));
			} 
			finally 
			{
				GameObject.DestroyImmediate (clone);
			}
		} 
		else 
		{
			Object.DestroyImmediate (component.gameObject);
		}
	}
}

[CustomEditor(typeof(PropLinker))]
class PropLinkerEditor : Editor {

	public override void OnInspectorGUI() {
		GUIStyle customLabel;

		EditorGUILayout.Space ();
		customLabel = new GUIStyle ("Label");
		customLabel.alignment = TextAnchor.LowerLeft;
		customLabel.fontSize = 14;
		customLabel.normal.textColor = Color.black;
		customLabel.fontStyle = FontStyle.Normal;

		PropLinker myScript = (PropLinker)target;

		switch (myScript.propType) {
		case PropLinker.PropType.simple:
			GUILayout.Label ("Simple Grabbable Prop", customLabel);

			if (myScript.GetComponentInChildren<FirearmLinker> () != null) {
				EditorGUILayout.Space ();

				customLabel = new GUIStyle ("Label");
				customLabel.alignment = TextAnchor.MiddleCenter;
				customLabel.fontSize = 12;
				customLabel.normal.textColor = Color.red;
				customLabel.fontStyle = FontStyle.BoldAndItalic;

				GUILayout.Label ("Some components should be removed!", customLabel);

				if (GUILayout.Button ("Remove Unused Components")) {
					var go = myScript.gameObject;
					go.DestroyComponentInPrefab<FirearmLinker> ();


					//go.DestroyComponentInPrefab<FirearmLinker> ();
				}
			}
			break;
		case PropLinker.PropType.firearm:
			GUILayout.Label ("Gun/Firearm Prop", customLabel);

			if (myScript.GetComponentInChildren<FirearmLinker> () == null) {
				EditorGUILayout.Space ();

				customLabel = new GUIStyle ("Label");
				customLabel.alignment = TextAnchor.MiddleCenter;
				customLabel.fontSize = 12;
				customLabel.normal.textColor = Color.red;
				customLabel.fontStyle = FontStyle.BoldAndItalic;

				GUILayout.Label ("Firearm script has not been added!", customLabel);

				if (GUILayout.Button ("Add Firearm Helpers")) {
					var firingPointPrefabPath = "Assets/AnimPrep/Prefabs/FiringPoint.prefab";

					UnityEngine.Object firingPointPrefab = AssetDatabase.LoadAssetAtPath(firingPointPrefabPath, typeof(GameObject));
					GameObject firingPointObject = GameObject.Instantiate(firingPointPrefab, Vector3.zero, Quaternion.identity) as GameObject;
					firingPointObject.name = firingPointPrefab.name;

					firingPointObject.transform.parent = myScript.transform;
					firingPointObject.transform.localPosition = Vector3.zero;
					firingPointObject.transform.localRotation = Quaternion.identity;

					//myScript.gameObject.AddComponent<FirearmLinker> ();
				}
			}
			break;
		} 
			
		DrawButtons ();

		DrawDefaultInspector ();

		serializedObject.ApplyModifiedProperties();
	}

	/*
	public static void DestroyComponentInPrefab<T>(this GameObject go) {
		bool isPrefabInstance = PrefabUtility.GetPrefabParent(go) != null && PrefabUtility.GetPrefabObject(go.transform) != null;
		if (isPrefabInstance) {
			GameObject clone = GameObject.Instantiate(go, Vector3.zero, Quaternion.identity) as GameObject;
			DestroyImmediate (clone.GetComponentInChildren (T).gameObject);
			PrefabUtility.ReplacePrefab (clone, PrefabUtility.GetPrefabParent (go));
			GameObject.DestroyImmediate(clone);
		} else {
			DestroyImmediate (go.GetComponentInChildren<FirearmLinker> ().gameObject);
		}
	}
	*/

	public void DrawButtons() {
		if (!Application.isPlaying) {
			return;
		}

		PropLinker myScript = (PropLinker)target;

		if (myScript.interactable != null) {
			var hand = myScript.interactable.attachedToHand;
			if (hand) {
				if (GUILayout.Button ("Update Controller Offset")) {//must be done in game
					UpdateObjOffset (myScript);
				}
			} else {
				if (GUILayout.Button ("Move To Right Hand")) {//must be done in game
					SetObjOffset(myScript.gameObject, Player.instance.rightHand);
				}
				if (GUILayout.Button ("Move To Left Hand")) {//must be done in game
					SetObjOffset(myScript.gameObject, Player.instance.leftHand);
				}

			}
		}
	}


	public static void SetObjOffset(GameObject sceneObject, Hand hand) {
		var offset = sceneObject.transform.Find ("ControllerOffset");


		if (offset != null) {
			//sceneObject.transform.position = Vector3.zero;
			sceneObject.transform.rotation = Quaternion.identity;

			Transform oldParent = sceneObject.transform.parent;

			Transform tempContainer = new GameObject ("Temp").transform;
			tempContainer.position = offset.position;
			tempContainer.rotation = Quaternion.Inverse( offset.rotation );

			sceneObject.transform.parent = tempContainer;

			tempContainer.position = hand.transform.position; //tip.transform.position;
			tempContainer.rotation = hand.transform.rotation; //tip.transform.rotation;

			sceneObject.transform.parent = oldParent;

			DestroyImmediate (tempContainer.gameObject);
		}
	}

	public static void UpdateObjOffset(PropLinker sceneObject) {
		var offset = sceneObject.transform.Find ("ControllerOffset");
		if (offset != null) {
			offset.localPosition = sceneObject.offsetPos;
			offset.localRotation = sceneObject.offsetRot;

			Selection.activeGameObject = offset.gameObject;
		}
	}

}
#endif

public class PropLinker : MonoBehaviour
{
	public enum PropType {
		simple,
		firearm
	}
    [Tooltip("Allows advanced functionality to be added to a prop.")]
	public PropType propType;

	public enum SkeletalMotionRangeChange
	{
		/// <summary>Estimation of bones in hand while holding a controller</summary>
		WithController = 0,

		/// <summary>Estimation of bones in hand while hand is empty (allowing full fist)</summary>
		WithoutController = 1,
	}
    [Tooltip("Changes how the hand grabbing this prop will be posed.\n\nWithController = Pistol grip\nWithoutController = Flat hand")]
    public SkeletalMotionRangeChange setRangeOfMotionOnPickup = SkeletalMotionRangeChange.WithoutController;  

	Vector3 m_offsetPos;
	public Vector3 offsetPos { get { return m_offsetPos; } }

	Quaternion m_offsetRot;
	public Quaternion offsetRot { get { return m_offsetRot; } }

	[HideInInspector]
	public Interactable interactable;

	void Start() {
		interactable = GetComponent<Interactable> ();

		switch (setRangeOfMotionOnPickup) {
		case SkeletalMotionRangeChange.WithController:
			interactable.setRangeOfMotionOnPickup = Valve.VR.SkeletalMotionRangeChange.WithController;
			break;
		case SkeletalMotionRangeChange.WithoutController:
			interactable.setRangeOfMotionOnPickup = Valve.VR.SkeletalMotionRangeChange.WithoutController;
			break;
		}
	}

	void LateUpdate() {
		if (interactable != null) {
			var hand = interactable.attachedToHand;
			if (hand) {
				m_offsetPos = transform.InverseTransformPoint (hand.transform.position);
				m_offsetRot = Quaternion.Inverse(hand.transform.rotation) * transform.rotation;
			}
		}
	}

}
