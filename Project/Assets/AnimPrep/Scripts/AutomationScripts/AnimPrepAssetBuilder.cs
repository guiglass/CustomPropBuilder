#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;

[ExecuteInEditMode] public class AnimPrepAssetBuilder : EditorWindow {

	[Header("References")]

	Texture2D logotex;

	//============================================
	[MenuItem("AnimPrep/Prop Importer")]
	static void Init()
	{
		AnimPrepAssetBuilder window = (AnimPrepAssetBuilder)GetWindow(typeof(AnimPrepAssetBuilder));
		window.maxSize = new Vector2(300f, 335f);
		window.minSize = window.maxSize;
	}

	void OnEnable() {
		string[] tex = AssetDatabase.FindAssets("logotex", new[] {"Assets/AnimPrep"});
		if (tex.Length != 0) {
			byte[] fileData;
			fileData = File.ReadAllBytes (AssetDatabase.GUIDToAssetPath(tex[0]));
			logotex = new Texture2D (50, 50);
			logotex.LoadImage (fileData); //..this will auto-resize the texture dimensions.
		}

		if (!Directory.Exists (AnimPrepAssetPostprocessor.assetBundlesFolder)) {
			Directory.CreateDirectory (AnimPrepAssetPostprocessor.assetBundlesFolder);
		}

		CheckBlenderAppExists ();
	}


	string modelPathLast = "";
	const string blenderAppPathDefault = "C:\\Program Files\\Blender Foundation\\Blender\\blender.exe";
	string blenderAppPath = blenderAppPathDefault;
	bool blenderAppExists = false;

	void OnGUI()
	{
		GUIStyle customLabel;

		customLabel = new GUIStyle ("Label");
		customLabel.fixedHeight = 50;

		GUI.Box (new Rect (0,0,50,50), new GUIContent("", logotex), customLabel);


		GUILayout.BeginHorizontal();

		var customButton = new GUIStyle ("Button");
		customButton.alignment = TextAnchor.MiddleCenter;
		GUILayout.FlexibleSpace();
		customButton.fontSize = 10;
		customButton.normal.textColor = new Color(0.2f,0.2f,0.2f);
		customButton.fontStyle = FontStyle.Italic;
		customButton.fixedWidth = 100;
		if (GUILayout.Button ("Add VR Player", customButton)) {
			var playerPrefabPath = "Assets/SteamVR/InteractionSystem/Core/Prefabs/Player.prefab";

			var file = new FileInfo(playerPrefabPath);

			string fullPath = file.FullName.Replace(@"\","/");
			string assetPath = "Assets" + fullPath.Replace(Application.dataPath, "");

			bool alreadyInScene = false;
			var allRootGos = UnityEngine.SceneManagement.SceneManager.GetActiveScene ().GetRootGameObjects ();
			foreach (var go in allRootGos) {
				bool isPrefabInstance = PrefabUtility.GetPrefabParent(go) != null && PrefabUtility.GetPrefabObject(go.transform) != null;
				if (isPrefabInstance) {
					Object parentObject = EditorUtility.GetPrefabParent(go);
					string path = AssetDatabase.GetAssetPath(parentObject); 

					if (assetPath.Equals (path)) {
						alreadyInScene = true;
						break;
					}
				}
			}

			if (alreadyInScene) {
				EditorUtility.DisplayDialog("Player prefab already exists.",
					"\"Player\" prefab already in the scene.\n\nTo reset it you can try deleting it first then pressing this button again to create a new VR player object.", "OK");
			} else {
				UnityEngine.Object prefab = AssetDatabase.LoadMainAssetAtPath(assetPath);

				GameObject clone  = PrefabUtility.InstantiatePrefab(prefab as GameObject) as GameObject;
			}

		}
		GUILayout.EndHorizontal();


		Rect r = (Rect)EditorGUILayout.BeginVertical(customLabel);

		customLabel = new GUIStyle ("Label");
		customLabel.alignment = TextAnchor.MiddleCenter;
		customLabel.fontSize = 14;
		customLabel.normal.textColor = Color.black;
		customLabel.fontStyle = FontStyle.Bold;

		GUILayout.Label("Custom Prop Builder", customLabel);

		customLabel = new GUIStyle ("Label");
		customLabel.alignment = TextAnchor.MiddleCenter;
		customLabel.fontSize = 11;
		customLabel.normal.textColor = new Color(0.5f,0.5f,0.5f);
		customLabel.fontStyle = FontStyle.Bold;

		GUILayout.Label(string.Format("Version: {0} (Lite)", Application.version), customLabel);
		EditorGUILayout.EndVertical();

        customLabel = new GUIStyle("Button");
        customLabel.alignment = TextAnchor.MiddleCenter;
        customLabel.fontSize = 10;
        customLabel.normal.textColor = new Color(0.2f, 0.2f, 0.2f);
        customLabel.fontStyle = FontStyle.Italic;
        customLabel.fixedWidth = 100;
        customLabel.normal.textColor = new Color(0.7f, 0.0f, 0.0f);

        //EditorGUILayout.LabelField("PATH WARNING!", customLabel, GUILayout.Width(100));

        if (!SystemInfo.operatingSystem.ToLower().Contains("windows 10"))
        {
            if (Application.dataPath.Length > 40)
            {
                if (GUILayout.Button("PATH WARNING!", customLabel))
                {
                    EditorUtility.DisplayDialog("Warning",
                         string.Format(
                             "Your OS is: \"{0}\" which may experience issues with long paths." +
                             "\n\nIt would be wise to move this Builder project as close to \"C:\\\" as possible." +
                             "\n\nCurrent application datapath:\n{1}",
                             SystemInfo.operatingSystem, Application.dataPath
                             ),
                         "Ok");

                    return;
                }
            }
        }

        customLabel = new GUIStyle ("Button");
		customLabel.alignment = TextAnchor.MiddleCenter;
		customLabel.fontSize = 14;
		customLabel.normal.textColor = new Color(0.0f,0.0f,1.0f);
		customLabel.fontStyle = FontStyle.Bold;

		EditorGUILayout.LabelField("Select the .blend file to process:");

		if (GUILayout.Button("Import Prop Model", customLabel)) {

			if (!string.IsNullOrEmpty (blenderAppPath) && !File.Exists (blenderAppPath)) {
				EditorUtility.DisplayDialog("Blender Application Is Not Set",
					"Please browse for and select the installed Blender application. Must be version 2.79.", "OK");
				return;
			}
            PlayerSettings.colorSpace = ColorSpace.Linear;

            AssetDatabase.RemoveUnusedAssetBundleNames ();

			var modelPath = EditorUtility.OpenFilePanel("Load model", modelPathLast, "blend,fbx");

			if (!string.IsNullOrEmpty (modelPath)) {
				modelPathLast = Path.GetDirectoryName(modelPath);
				if (Path.GetExtension (modelPath).Equals (".blend")) {
					if (!RunBatch (AnimPrepAssetPostprocessor.assetBundleVariant, modelPath, blenderAppPath)) {
						EditorUtility.DisplayDialog("No AssetCreator.exe Tool",
							"Please ensure the AssetCreator.exe tool in located in the AnimPrep directory.", "OK");
						return;
					}

					var baseName = Path.GetFileNameWithoutExtension (modelPath);

					modelPath = Path.Combine (
						Path.Combine (
							Path.GetDirectoryName (modelPath), baseName.ToLower() + string.Format("_{0}", AnimPrepAssetPostprocessor.assetBundleVariant.ToLower())
						),
						baseName + ".fbx"
					);
				}

				var uploadFolder = Path.GetDirectoryName (modelPath);

				//var userName = Path.GetFileName (SystemInfo.deviceName);

				var uploadFolderTop = new DirectoryInfo (uploadFolder).Name;

				var uploadName = System.Guid.NewGuid ().ToString ();// Path.GetFileName (uploadFolder);

				var processingPath = AnimPrepAssetPostprocessor.processingFolder;// Path.Combine(Application.dataPath, "MakeHumanModels");
				//processingPath = Path.Combine (processingPath, userName);
				processingPath = Path.Combine (processingPath, uploadName);

				System.IO.Directory.CreateDirectory (processingPath);


				DirectoryInfo dir = new DirectoryInfo (uploadFolder);
				FileInfo[] modelsInfo = dir.GetFiles ("*.fbx");

				if (modelsInfo.Length == 0) {
					Debug.LogError ("modelsInfo was empty. No .fbx file could be loaded.");
					return;
				}

				string uid = uploadName.Replace (AnimPrepAssetPostprocessor.templateSeperator.ToString(), "");// uploadFolderTop.Replace("$", "");

	
				//Copy all model files
				foreach (FileInfo f in modelsInfo) {
					var to = Path.Combine (
						processingPath,
						AnimPrepAssetPostprocessor.assetBundleVariant + 
						AnimPrepAssetPostprocessor.templateSeperator +//"$"
						uid + 
						AnimPrepAssetPostprocessor.templateSeperator +//"$"
						f.Name
					);
					File.Copy (f.FullName, to);
				}

				//Copy all .json files
				FileInfo[] jsonInfo = dir.GetFiles ("*.json");
				foreach (FileInfo f in jsonInfo) {
					var to = Path.Combine (processingPath, f.Name);
					File.Copy (f.FullName, to);
				}

				//Copy all images files
				var textures_path = Path.Combine(uploadFolder, "textures");
				if (Directory.Exists(textures_path))
				{
					string[] extensions = new[] { ".png", ".jpg", ".tiff", ".bmp" };
				
					DirectoryInfo dir_images = new DirectoryInfo (textures_path);

					FileInfo[] texturesInfo =
						dir_images.GetFiles()
							.Where(f => extensions.Contains(f.Extension.ToLower()))
							.ToArray();

					foreach (FileInfo f in texturesInfo)
					{
						var to = Path.Combine(processingPath, f.Name);
						FileUtil.CopyFileOrDirectory(f.FullName, to);
					}
				}

				AnimPrepAssetPostprocessor.AssetBundleUserJson userPrefs = new AnimPrepAssetPostprocessor.AssetBundleUserJson () {
					created = System.DateTime.UtcNow,
					//user = userName,
					//uploadFolder = uploadName,
					characterFolder = Path.GetDirectoryName(modelPath)
				};

				string json = JsonUtility.ToJson (userPrefs);
				var jsonPath = Path.Combine (AnimPrepAssetPostprocessor.assetBundlesFolder, uid + ".json");
				using (StreamWriter sr = new StreamWriter (jsonPath)) { // Create the file.
					sr.WriteLine (json);
				}

			
				AssetDatabase.Refresh ();

				BuildScript.BuildAssetBundles ();
		
				AssetDatabase.Refresh ();

			}
		}

		EditorGUILayout.Space();

		customLabel = new GUIStyle ("Button");
		customLabel.alignment = TextAnchor.MiddleCenter;
		customLabel.fontSize = 14;
		customLabel.normal.textColor = new Color(0.0f,0.5f,0.0f);
		customLabel.fontStyle = FontStyle.Bold;

		EditorGUILayout.LabelField("Save changes to assetbundles:");
		if (GUILayout.Button("Re-Build Assetbundles", customLabel)) {

			var allPaths = AssetDatabase.GetAllAssetPaths ();
			foreach (var assetPath in allPaths) {
				if (assetPath.StartsWith(AnimPrepAssetPostprocessor.prefabsFolder)) {
					//ensure the prefab is enabled before saving as assetbundle

					string modelFileName = Path.GetFileNameWithoutExtension( assetPath );
					GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject> (assetPath); //LOADING AN ASSET
					if (modelAsset == null) {
						continue;
					}

					if (!modelAsset.activeSelf) {
						GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
						model.SetActive (true);
						PrefabUtility.SaveAsPrefabAsset (model, assetPath);
						GameObject.DestroyImmediate (model);
					}
				}
			}


			var allShaderKeywordParams = GameObject.FindObjectsOfType<RendererShaderParams> ();

			foreach (var shaderKeywordParams in allShaderKeywordParams) {
				shaderKeywordParams.StoreParams();
			}

			var allRootGos = UnityEngine.SceneManagement.SceneManager.GetActiveScene ().GetRootGameObjects ();
			foreach (var go in allRootGos) {

				bool isPrefabInstance = PrefabUtility.GetPrefabParent(go) != null && PrefabUtility.GetPrefabObject(go.transform) != null;
				if (isPrefabInstance) {

					RendererShaderParams.StoreAllRenderers (go);

					PrefabUtility.ReplacePrefab(go, PrefabUtility.GetPrefabParent(go), ReplacePrefabOptions.ConnectToPrefab);
				}
			}

			AssetDatabase.Refresh ();
			BuildScript.BuildAssetBundles ();
			AssetDatabase.Refresh ();

			ShowAssetBundlesExplorer ();

		}

	

		EditorGUILayout.Space();

		customLabel = new GUIStyle ("Button");
		customLabel.alignment = TextAnchor.MiddleCenter;
		customLabel.fontSize = 12;
		//customLabel.normal.textColor = new Color(0.0f,0.5f,0.0f);
		customLabel.fontStyle = FontStyle.Italic;

		EditorGUILayout.LabelField("Add processed models to scene:");

		if (GUILayout.Button("Append Prefabs To Scene", customLabel))	{

			var info = new DirectoryInfo(AnimPrepAssetPostprocessor.prefabsFolder);
			var fileInfo = info.GetFiles("*.prefab", SearchOption.TopDirectoryOnly);

			for (int i = 0; i < fileInfo.Length; i++) {
				var file = fileInfo[i];

				if (Path.GetExtension (file.FullName).Equals (".meta")) {
					continue;
				}

				string fullPath = file.FullName.Replace(@"\","/");
				string assetPath = "Assets" + fullPath.Replace(Application.dataPath, "");

				bool alreadyInScene = false;
				var allRootGos = UnityEngine.SceneManagement.SceneManager.GetActiveScene ().GetRootGameObjects ();
				foreach (var go in allRootGos) {
					bool isPrefabInstance = PrefabUtility.GetPrefabParent(go) != null && PrefabUtility.GetPrefabObject(go.transform) != null;
					if (isPrefabInstance) {
						Object parentObject = EditorUtility.GetPrefabParent(go);
						string path = AssetDatabase.GetAssetPath(parentObject); 

						if (assetPath.Equals (path)) {
							alreadyInScene = true;
							break;
						}

					}
				}
				if (alreadyInScene) {
					continue;
				}

				UnityEngine.Object prefab = AssetDatabase.LoadMainAssetAtPath(assetPath);

				GameObject clone = PrefabUtility.InstantiatePrefab(prefab as GameObject) as GameObject;
			}
		}




		EditorGUILayout.Space();

		customLabel = new GUIStyle ("Button");
		customLabel.alignment = TextAnchor.MiddleCenter;
		customLabel.fontSize = 12;
		customLabel.normal.textColor = new Color(0.2f,0.2f,0.2f);
		customLabel.fontStyle = FontStyle.Italic;

		EditorGUILayout.LabelField("Show folder containing output files:");
		if (GUILayout.Button ("Open Assetbundles Folder", customLabel)) {
			ShowAssetBundlesExplorer ();
		}


		EditorGUILayout.Space();



		GUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Blender Application (v2.79):", GUILayout.MinWidth (0));

		customLabel = new GUIStyle ("Label");
		customLabel.alignment = TextAnchor.MiddleCenter;
		customLabel.fontStyle = FontStyle.BoldAndItalic;

		if (blenderAppExists) {
			customLabel.normal.textColor = new Color (0.0f, 0.5f, 0.0f);
			EditorGUILayout.LabelField ("File Exists", customLabel, GUILayout.Width (100));
		} else {
			customLabel.normal.textColor = new Color (0.5f, 0.0f, 0.0f);
			EditorGUILayout.LabelField ("File Missing!", customLabel, GUILayout.Width (100));

			customButton = new GUIStyle ("Button");
			customButton.alignment = TextAnchor.MiddleCenter;
			customButton.fontSize = 10;
			customButton.normal.textColor = new Color(0.2f,0.2f,0.2f);
			customButton.fontStyle = FontStyle.Italic;
			customButton.fixedWidth = 100;
			if (GUILayout.Button ("Default", customButton)) {
				blenderAppPath = blenderAppPathDefault;
			}

		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		customLabel = new GUIStyle ("Button");
		customLabel.alignment = TextAnchor.MiddleCenter;
		customLabel.fontSize = 10;
		customLabel.normal.textColor = new Color(0.2f,0.2f,0.2f);
		customLabel.fontStyle = FontStyle.Italic;
		customLabel.fixedWidth = 100;

		blenderAppPath = GUILayout.TextField(blenderAppPath, GUILayout.MinWidth (0));
		if (GUILayout.Button ("Browse", customLabel)) {
			string modelPath = EditorUtility.OpenFilePanel("Blender Application (v2.79)", Path.GetDirectoryName(blenderAppPath), "exe");
			if (!string.IsNullOrEmpty (modelPath)) {
				blenderAppPath = modelPath;
			}
		}
		GUILayout.EndHorizontal();
        EditorGUILayout.Space();

        customLabel = new GUIStyle("Label");
        customLabel.alignment = TextAnchor.MiddleCenter;
        customLabel.fontStyle = FontStyle.Italic;
        GUILayout.Label(string.Format("{0} - {1}", Application.unityVersion, SystemInfo.operatingSystem), customLabel);

        if (GUI.changed)
		{

			CheckBlenderAppExists ();
		}

		this.Repaint();
	}

	void CheckBlenderAppExists() {
		blenderAppExists = !string.IsNullOrEmpty (blenderAppPath) && File.Exists (blenderAppPath);
	}


	public static void ShowAssetBundlesExplorer()
	{
		ShowExplorer ( Application.dataPath + Path.Combine("/../", AnimPrepAssetPostprocessor.assetBundlesFolder));
	}


	public static void ShowExplorer(string itemPath)
	{
		itemPath = itemPath.Replace (@"/", @"\");//  @"\");   // explorer doesn't like front slashes
		System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
			FileName = itemPath,
			UseShellExecute = true,
			Verb = "open"
		});
	}

	public static bool RunBatch (string assetType, string modelPath, string blenderPath) {
		var path = "Assets\\AnimPrep\\AssetCreator.exe";
		if (!File.Exists (path)) {
			Debug.LogError ("The AssetCreator.exe tool was missing");
			return false;
		}

		try {
			System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
			myProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			myProcess.StartInfo.CreateNoWindow = true;
			myProcess.StartInfo.UseShellExecute = false;
			myProcess.StartInfo.FileName = "C:\\Windows\\system32\\cmd.exe";
			path = string.Format("{0} {1} \"{2}\" \"{3}\"", path, assetType, modelPath, blenderPath);
			myProcess.StartInfo.Arguments = "/c" + path;
			myProcess.EnableRaisingEvents = true;
			myProcess.Start();
			myProcess.WaitForExit();
			int ExitCode = myProcess.ExitCode;
			return true;
		} catch (System.Exception e){
			Debug.Log(e);        
		}
		return false;
	}

}


public class BuildScript
{
	public static void BuildAssetBundles ()
	{
		//TODO Assertion failed: AssetBundle index doesn't exist in the asset database.
		BuildPipeline.BuildAssetBundles(AnimPrepAssetPostprocessor.assetBundlesFolder, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
	}

	public static void BuildAssetBundles (AssetBundleBuild[] assetBundleBuilds)
	{
		BuildPipeline.BuildAssetBundles(AnimPrepAssetPostprocessor.assetBundlesFolder, assetBundleBuilds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
	}
}


#endif