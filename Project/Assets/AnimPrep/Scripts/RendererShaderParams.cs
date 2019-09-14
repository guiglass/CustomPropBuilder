using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RendererShaderParams))]
class RendererShaderParamsEditor : Editor {
	public override void OnInspectorGUI() {

		RendererShaderParams myScript = (RendererShaderParams)target;
		if (GUILayout.Button ("Store Parameters")) {
			myScript.StoreParams ();
		}

		DrawDefaultInspector ();
	}
}
#endif
public class RendererShaderParams : MonoBehaviour {
	/// <summary>
	/// Because unity assetbundles and standard shaders do not automatically take into account the 
	/// currently set keyword, this must be done manually. This script stores the keywords that were
	/// present at the time the character was created and represend the shader's behavior for the renderer
	/// which this component is attached to.
	/// </summary>
	/// <value>The shader keyword parameters.</value>

	public static void StoreAllRenderers(GameObject go) {

		foreach (var renderer in go.GetComponentsInChildren<Renderer>()) {
			if (renderer.GetComponent<RendererShaderParams> ()) {
				renderer.GetComponent<RendererShaderParams> ().StoreParams ();
			} else {
				var hasMats = false;
				foreach (var mat in renderer.sharedMaterials) {
					hasMats |= mat != null;
				}
				if (hasMats) {
					renderer.gameObject.AddComponent<RendererShaderParams> ().StoreParams ();
				}
			}
		}

	}



	[Serializable]
	public struct MaterialParams {
		public Material material;

		public int renderQueue;

		public ShaderKeywordParams[] shaderKeywordParams;
		public ShaderValuesParams[] shaderValuesParams;
	}

	public MaterialParams[] materialsParams;

	[Serializable]
	public struct ShaderKeywordParams {
		public string key;
		public bool value;
	}
	[Serializable]
	public struct ShaderValuesParams {
		public string key;
		public float value;
	}

	static string[] keywordsBoolean = new string[] {
		"_SPECGLOSSMAP",
		"_METALLICGLOSSMAP",
		"_EMISSION",
		"_ALPHATEST_ON",
		"_ALPHABLEND_ON",
		"_ALPHAPREMULTIPLY_ON",
		"LIGHTPROBE_SH",
		"DIRECTIONAL",
		"SHADOWS_SCREEN",
		"VERTEXLIGHT_ON",
		"POINT",
		"SPOT",
		"POINT_COOKIE",
		"DIRECTIONAL_COOKIE",
		"SHADOWS_DEPTH",
		"SHADOWS_CUBE",
	};


	static string[] keywordsFloats = new string[] {
		"_SrcBlend",
		"_DstBlend",
		"_Cull",
		"_ZWrite",
		"_Cutoff",
		"_Glossiness",
		"_GlossMapScale",
		"_ZTest"
	};


	public void StoreParams() {

		var mats = GetComponent<Renderer> ().sharedMaterials;
		materialsParams = new MaterialParams[mats.Length];

		for (int n = 0; n < mats.Length; n++) {
			var mat = mats[n];

			List<ShaderKeywordParams> shaderKeywordParamsList = new List<ShaderKeywordParams> ();
			for (int i = 0; i < keywordsBoolean.Length; i++) {
				var keyword = keywordsBoolean [i];
				shaderKeywordParamsList.Add ( new ShaderKeywordParams () {
				//materialsParams[n].shaderKeywordParams [i] = new ShaderKeywordParams () {
					key = keyword,
					value = mat.IsKeywordEnabled (keyword),
				});
			}

			List<ShaderValuesParams> shaderValuesParamsList = new List<ShaderValuesParams> ();
			for (int i = 0; i < keywordsFloats.Length; i++) {
				var keyword = keywordsFloats [i];
				if (!mat.HasProperty (keyword)) {
					continue;//might be hair or a different shader than standard
				}
				shaderValuesParamsList.Add ( new ShaderValuesParams () {
				//materialsParams[n].shaderValuesParams [i] = new ShaderValuesParams () {
					key = keyword,
					value = mat.GetFloat (keyword),
				});
			}

			//mat.renderQueue = 3000;

			materialsParams[n] = new MaterialParams () { 
				material = mat,

				renderQueue = mat.renderQueue,

				shaderKeywordParams = shaderKeywordParamsList.ToArray (),// new ShaderKeywordParams[keywordsBoolean.Length],
				shaderValuesParams = shaderValuesParamsList.ToArray (),// new ShaderValuesParams[keywordsFloats.Length],
			};

		}
	}




}
