using UnityEditor;
using UnityEngine;

namespace MeshSplatMapTool
{
	[CustomEditor (typeof(MeshSplatTool))]
	public class MeshSplatEditor : Editor
	{
		// general
		private const string appName = "MeshSplatMapTool";

		// keep track of previous paintmode
		private bool oldPaintMode = false;

		private string[]  layerNames= new [] {"User","Red (override)","Green (override)","Blue (override)","Alpha (override)"};

		// collider layermask
//		bool checkColliderObjects = false;
//		int objCollidersLayer = 0;

		void OnEnable () 
		{
			//Debug.Log ("OnEnable()");
			MeshSplatTool mst = target as MeshSplatTool;
			mst.loadSplatmap();
		}

		// inspector gui events
		public override void OnInspectorGUI ()
		{
			MeshSplatTool mst = target as MeshSplatTool;

			// Settings
			GUILayout.Space(11);
			GUILayout.Label("Settings", EditorStyles.boldLabel);


			// mask texture resolution
			//GUILayout.Space(11);
			GUILayout.BeginHorizontal ("", GUIStyle.none);
			mst.maskResolution = mst.maskResolution = Mathf.Clamp(EditorGUILayout.IntField("Texture Resolution:", mst.maskResolution),0,4096);
			
				if(GUILayout.Button (new GUIContent ("PoT", "Nearest Power of Two"), GUILayout.Width(40))) 
				{
					mst.maskResolution = Mathf.ClosestPowerOfTwo(mst.maskResolution);
				}
			EditorGUILayout.EndHorizontal ();
				
			GUILayout.Space(11);
			// range editor drag bars flat-mid-steep
			GUILayout.Label("Terrain Slopes", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal ("", GUIStyle.none);
				mst.flatRangeMin = Mathf.Clamp( EditorGUILayout.FloatField("", mst.flatRangeMin, GUILayout.Width(40)),0,0);
				mst.flatRangeMax = Mathf.Clamp( EditorGUILayout.FloatField("", mst.flatRangeMax,GUILayout.Width(40)),0,mst.mediumRangeMax);
				mst.mediumRangeMax = Mathf.Clamp( EditorGUILayout.FloatField("", mst.mediumRangeMax,GUILayout.Width(40)),mst.flatRangeMax,mst.steepRangeMax);
				mst.steepRangeMax = Mathf.Clamp(EditorGUILayout.FloatField("", mst.steepRangeMax,GUILayout.Width(40)),90,90);
			EditorGUILayout.EndHorizontal ();	
			EditorGUILayout.MinMaxSlider(ref mst.flatRangeMin, ref mst.flatRangeMax, 0, 90);
			EditorGUILayout.MinMaxSlider(ref mst.flatRangeMax, ref mst.mediumRangeMax, 0, 90);
			EditorGUILayout.MinMaxSlider(ref mst.mediumRangeMax, ref mst.steepRangeMax, 0, 90);

			// ** realtime preview options **
			mst.realTimePreviewEnabled = EditorGUILayout.BeginToggleGroup("RealTime Preview", mst.realTimePreviewEnabled);
			EditorGUILayout.EndToggleGroup();

			//GUILayout.Label("Colliders", EditorStyles.boldLabel);
			mst.checkColliderObjects = EditorGUILayout.BeginToggleGroup("Check object colliders", mst.checkColliderObjects);
				mst.obstacleRadius = EditorGUILayout.FloatField("Check radius:",mst.obstacleRadius);
				mst.objCollidersLayer = EditorGUILayout.LayerField("Object layer:",mst.objCollidersLayer);
			EditorGUILayout.EndToggleGroup();


			// *** create splatmap texture button ***
			GUILayout.Space(11);
			GUILayout.BeginHorizontal ("", GUIStyle.none);
			if(GUILayout.Button (new GUIContent ("Create SplatMask Texture", "Create SplatMask Texture"), GUILayout.Height(40))) 
			{
				mst.realTimePreviewEnabled = false;
				mst.createSplatMask(mst.maskResolution);
			}
			EditorGUILayout.EndHorizontal ();
			GUILayout.Space(11);


			// *** tools ***
			GUILayout.Label("Tools", EditorStyles.boldLabel);


			// *** gaussion blur button ***
			GUILayout.BeginHorizontal ("", GUIStyle.none);
			mst.blurStrength = Mathf.Clamp(EditorGUILayout.FloatField("Blur strength: ", mst.blurStrength),0,10);
			if(GUILayout.Button (new GUIContent ("Blur", "Gaussion blur splatmask texture"))) 
			{
				mst.blur();
			}
			EditorGUILayout.EndHorizontal ();
			
			
			// *** paint mode ***
			GUILayout.Space(11);
			mst.paintMode = EditorGUILayout.BeginToggleGroup("SplatMask Painting Mode", mst.paintMode);

				// if paintmode was changed
				if (mst.paintMode != oldPaintMode)
				{
					if (mst.paintMode)
					{
						oldPaintMode = mst.paintMode;
						EditorUtility.SetSelectedWireframeHidden(Selection.activeGameObject.GetComponent<Renderer>(), true);
					}else{ 
						oldPaintMode = mst.paintMode;
						EditorUtility.SetSelectedWireframeHidden(Selection.activeGameObject.GetComponent<Renderer>(), false);
						// save texture, if it was edited
						//mst.saveToDisk(); // HACK: save texture here, instead of each mouseup..
					}
				}

				// TODO: paint brush (circular, or texture)
				mst.paintBrush = EditorGUILayout.ObjectField("Brush Texture:", mst.paintBrush, typeof(Texture2D), true) as Texture2D;

				mst.brushSize = Mathf.Clamp(EditorGUILayout.IntField("Brush size:",mst.brushSize),1,512);
				mst.brushStrengh = Mathf.Clamp(EditorGUILayout.FloatField("Brush strengh:",mst.brushStrengh),0.0f,1.0f);
			//mst.objCollidersLayer = EditorGUILayout.LayerField("Object layer:",mst.objCollidersLayer);

				GUILayout.BeginHorizontal (GUILayout.ExpandWidth(false));
				EditorGUILayout.LabelField( new GUIContent ("Layer to paint: ","Painting layer") );
				mst.paintLayer = EditorGUILayout.Popup(mst.paintLayer, layerNames);
				EditorGUILayout.EndHorizontal ();


			EditorGUILayout.EndToggleGroup();





			GUILayout.Space(11);

			// object distribution, TODO: remove from here..separate distribution script..
			GUILayout.Label("Object2Distribute", EditorStyles.boldLabel);
			mst.distributePrefab = EditorGUILayout.ObjectField(mst.distributePrefab, typeof(GameObject), true) as GameObject;
			mst.objCount = Mathf.Clamp( EditorGUILayout.IntField("Amount", mst.objCount),0,255);

			// distribute objects
			if(GUILayout.Button (new GUIContent ("Randomly Place Items", ""), GUILayout.Height(20))) 
			{
				mst.distributeObjects();
			} 


			if (mst.realTimePreviewEnabled)
			{
				// FIXME: this listens any gui changes.. only need to check those related to angles..
				if (GUI.changed) mst.realTimePreview();
			}

		} // onInspectorGUI

		// main loop
		public void OnSceneGUI()
		{
			MeshSplatTool mst = target as MeshSplatTool;

			// below this, its only about painting, so we can early exit
			if (!mst.paintMode) return;

			// get current event
			Event e = Event.current;

			// check keypress
			if (Event.current.type == EventType.keyDown)
			{
				// escape stops painting mode
				if (Event.current.keyCode == (KeyCode.Escape))
				{
					mst.sameStroke = false; // we end this stroke
					mst.paintMode = false;
					oldPaintMode = mst.paintMode;
					EditorUtility.SetSelectedWireframeHidden(Selection.activeGameObject.GetComponent<Renderer>(), false);
				}
			}

			// this stops objects getting selected
			if (Event.current.type == EventType.layout)  HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));

			//Event e = Event.current;
			//int ID = GUIUtility.GetControlID(hash, FocusType.Passive);
			//EventType type = current.GetTypeForControl(ID);

			RaycastHit hit;
			Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
			bool raycastResult = Selection.activeGameObject.GetComponent<Collider>().Raycast (ray, out hit, 9999f);
			if (raycastResult)
			{
				Handles.DrawWireDisc(hit.point,hit.normal, mst.brushSize*0.5f); // TODO: support for other shapes than sphere..?
				HandleUtility.Repaint();
			}

			// mouse pressed down, or dragged & pressed
			if(((e.type == EventType.MouseDown) || (e.type == EventType.MouseDrag)) && (e.button == 0))
			{
				if (Selection.activeGameObject==null) 
				{
					//Debug.Log (mst.go);
					Debug.Log (appName+" error: Cannot paint now, click Create Texture again..");
					return;
				}

				// we hit terrain
				if (raycastResult)
				{
					mst.registerTextureUndo();
					Vector2 pixelUV = hit.textureCoord;
					mst.paint(pixelUV);
				}

				//else if ( e.type == EventType.Layout )
				//{
				   // http://answers.unity3d.com/questions/303248/how-to-paint-objects-in-the-editor.html
				   //HandleUtility.AddDefaultControl( GUIUtility.GetControlID( GetHashCode(), FocusType.Passive ) );
			//	}
				
			} // mousedown

			// mouse released, after painting
			if (e.type == EventType.MouseUp)
			{
				mst.sameStroke = false; // we end this stroke
				mst.saveToDisk();
				e.Use();
			}

		} //OnSceneGUI
			
	} // class

} // namespace