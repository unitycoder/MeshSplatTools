using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace MeshGrassTest
{
	public class MeshGrassMaker : EditorWindow 
	{

		private const string appName = "MeshGrassMaker";
		
		private GameObject meshTerrain;
		private Material grassMaterial;
		private Vector3 OffsetY = new Vector3(0,-0.005f,0);

		//private Vector2 atlasSize = new Vector2(2,1); // x rows, y columns
		private int atlasTilesX = 2; // start from 0 
		private int atlasTilesY = 2; // start from 0 

		private float[] grassPercentage = {50,30,15,5};
//		private float[] grassPercentage = {50,80,95,100};

		private Color vertexBottomColor = new Color(0,0,0,0);
		private Color vertexTopColor = new Color(1,1,1,1);

		private float densityMultiplier = 4.5f;
		private float posNoiseScale = 3.0f;

		private bool twoPlanes = true;
		private int debugLimit = 2; // stop making grass after #st patch

		private GameObject tree1;

		// create menu item and window
		[MenuItem ("Tools/"+appName)]
		static void Init () 
		{
			MeshGrassMaker window = (MeshGrassMaker)EditorWindow.GetWindow (typeof (MeshGrassMaker));
			window.title = appName;
			window.minSize = new Vector2(340,350);
			window.maxSize = new Vector2(341,351);
		}

		// main loop
		void OnGUI () 
		{
			// title
			GUILayout.Label ("Test", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			// TODO: grass options and settings (sizes, colors, textures, or just prefabs?)
			
			GUILayout.Label ("Mesh Terrain GameObject");
			meshTerrain = (GameObject)EditorGUILayout.ObjectField(meshTerrain,typeof(GameObject),true);
			
			GUILayout.Label ("Grass Material");
			grassMaterial = (Material)EditorGUILayout.ObjectField(grassMaterial,typeof(Material), false);

			GUILayout.Label ("optional: Tree prefab1");
			tree1 = (GameObject)EditorGUILayout.ObjectField(tree1,typeof(GameObject),true);


			GUILayout.Label ("Grass Vertex Colors");
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label ("Top___");
			vertexTopColor = EditorGUILayout.ColorField(vertexTopColor);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label ("Bottom");
			vertexBottomColor = EditorGUILayout.ColorField(vertexBottomColor);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label ("Density multiplier");
			densityMultiplier = Mathf.Clamp(EditorGUILayout.FloatField(densityMultiplier),0.1f,4.5f);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label ("Positional Noise Scale");
			posNoiseScale = Mathf.Clamp(EditorGUILayout.FloatField(posNoiseScale),0.1f,10f);
			EditorGUILayout.EndHorizontal();

			twoPlanes = GUILayout.Toggle(twoPlanes,"Two planes");
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label ("Debug: Max grass patches");
			debugLimit = Mathf.Clamp(EditorGUILayout.IntField(debugLimit),1,999);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label ("Texture Atlas X tiles");
			atlasTilesX = Mathf.Clamp(EditorGUILayout.IntField(atlasTilesX),1,32);
			GUILayout.Label ("Texture Atlas Y tiles");
			atlasTilesY = Mathf.Clamp(EditorGUILayout.IntField(atlasTilesY),1,32);
			EditorGUILayout.EndHorizontal();


			if(GUILayout.Button (new GUIContent ("Make Grass", ""),GUILayout.Height(30))) 
			{
				// TODO: check if terrain & material are assigned
				MakeGrass();
			}

		} // ongui









		// main grass generator
		void MakeGrass()
		{

			// TODO: build quads/meshes and place(drop) to terrain (maybe big triangle would be enough also)
			// TODO: use vertex colors (get bottom vertex tint from terrain texture)
			// TODO v2: distribution, based on terrain angles/slopes/sun reach?, where/how grass would be..

			List<Vector3> newVerts = new List<Vector3>();
			List<Vector2> newUV = new List<Vector2>();
			List<int> newTris = new List<int>();
			List<Color> newCols = new List<Color>();



	

//			int maxQuads=64;
	

			//for (int i=0;i<maxQuads;i++)
			int i=0;
//			int iter=0;
//			int maxIter = 99999; // just in case
//			int maxArea = 100;
			
			float noiseScale = 0.12f;
			//float noiseScale2 = 0.61f;
			//float noiseScale3 = 0.53f;
			
			float grassScale = 1.5f;
			
			// TODO: use terrain splatmask as reference
			// TODO: later: use terrain slopes as reference.. or special grass mask texture..RGBA = grass types
			
			//tmpTex = new Texture2D(texResolution,texResolution,TextureFormat.ARGB32, true);
			//tmpTex.hideFlags = HideFlags.DontSave;
			
			
			Texture2D maskTex = (Texture2D)meshTerrain.GetComponent<Renderer>().sharedMaterial.GetTexture("_Mask");
			maskTex.hideFlags = HideFlags.DontSave;
			
			Texture2D tex1 = (Texture2D)meshTerrain.GetComponent<Renderer>().sharedMaterial.GetTexture("_MainTex1");
			tex1.hideFlags = HideFlags.DontSave;
			
			Texture2D tex2 = (Texture2D)meshTerrain.GetComponent<Renderer>().sharedMaterial.GetTexture("_MainTex2");
			tex2.hideFlags = HideFlags.DontSave;
			
			Texture2D tex3 = (Texture2D)meshTerrain.GetComponent<Renderer>().sharedMaterial.GetTexture("_MainTex3");
			tex3.hideFlags = HideFlags.DontSave;
			
			Texture2D tex4 = (Texture2D)meshTerrain.GetComponent<Renderer>().sharedMaterial.GetTexture("_MainTex4");
			tex4.hideFlags = HideFlags.DontSave;

			// raycasting stuff		
			Bounds area = meshTerrain.GetComponent<Renderer>().bounds;
			float pixelStepX = area.size.x/maskTex.width;
			float pixelStepZ = area.size.z/maskTex.height; // or square?
			RaycastHit hit;
			//int xx=0;
			//int yy=0;
			
			//Debug.Log(area.min.z+pixelStepZ/2);
			//Debug.Log(area.max.z);
			
			// TODO: adjust grass placement resolution
			pixelStepX*=(5-densityMultiplier);
			pixelStepZ*=(5-densityMultiplier);
			
			
			float p1=0;
			float p2=0;
			float p3=0;
			float p4=0;
			

			float noiseScale2=0.37f;
			float noiseScale3=0.264f;

			// texture tiling value
			int tilingValue = 111;

			int treeCount=0;

			int vertIndex=0;
			int meshCount=0;
//			int stepSize = 16; // if stepSize = 1, then its 512x512
			//yy=0;
			for (float z=area.min.z+pixelStepZ/2;z<area.max.z;z+=pixelStepZ)
			{
				//xx=0;
				for (float x=area.min.x+pixelStepX/2;x<area.max.x;x+=pixelStepX)
				{
					
					//Debug.Log(z);
					//float r = 0;
					//float g = 0;
					//float b = 0;
					//float a = 0;
					
					//Debug.DrawLine(new Vector3(x,100,z), new Vector3(x,0,z), Color.red, 5, false);
					
					

					//if (Physics.Raycast(new Vector3(x,200,y), -Vector3.up, out hit, 9999.0F))


					// HACK: add noise to raycast position..
					float noiseX = Mathf.PerlinNoise(x*noiseScale2,z*noiseScale2)*posNoiseScale;
					float noiseZ = Mathf.PerlinNoise(x*noiseScale3,z*noiseScale3)*posNoiseScale;

					Ray ray = new Ray(new Vector3(x+noiseX,area.max.y + 1,z+noiseZ), -Vector3.up); 
					if (meshTerrain.GetComponent<Collider>().Raycast(ray, out hit, 19999.0F)) // FIXME: 19999 could be taken from maxheight
					{					

						Vector2 pixelUV = hit.textureCoord;


						pixelUV.x *= maskTex.width;
						pixelUV.y *= maskTex.height;
				        //tex.SetPixel(pixelUV.x, pixelUV.y, Color.black);


						Color maskC = maskTex.GetPixel((int)pixelUV.x, (int)pixelUV.y);


						// TEST: trees
						if (tree1 != null)
						{
							if (maskC.g>0.5f)
							{
								
								if (Random.value>0.997f && treeCount<250)
								{
									treeCount++;
									
									Instantiate(tree1,hit.point,Quaternion.Euler(new Vector3(0,Random.Range(0,360),0)));
									
								}
							}
						}


						
						// TODO: threshold adjustments..?
						// TODO: better distribution.. could get "grass center" positions, and spread (floodfill?) from there..
						//if (maskC.g>0.75f)
						if (Random.value>1-maskC.g)
						{
							Vector2 pixelUV1 = hit.textureCoord*tilingValue;
							pixelUV1.x *= tex1.width;
							pixelUV1.y *= tex1.height;
							
							Vector2 pixelUV2 = hit.textureCoord*tilingValue;
							pixelUV2.x *= tex2.width;
							pixelUV2.y *= tex2.height;

							Vector2 pixelUV3 = hit.textureCoord*tilingValue;
							pixelUV3.x *= tex3.width;
							pixelUV3.y *= tex3.height;
							
							Vector2 pixelUV4 = hit.textureCoord*tilingValue;
							pixelUV4.x *= tex4.width;
							pixelUV4.y *= tex4.height;

							// get pixel color
							Color c1 = tex1.GetPixel((int)pixelUV1.x, (int)pixelUV1.y);
							Color c2 = tex2.GetPixel((int)pixelUV2.x, (int)pixelUV2.y);
							Color c3 = tex3.GetPixel((int)pixelUV3.x, (int)pixelUV3.y);
							Color c4 = tex4.GetPixel((int)pixelUV4.x, (int)pixelUV4.y);
							Color c = c1 * maskC.r + c2 * maskC.g + c3 * maskC.b;

							// TODO: color multiplier option
							c = Color.Lerp(c,c4,maskC.a)*3;
							//c = c2*4;

							// calculate height scaling
							float grassHeightScale = 0.5f+Mathf.PerlinNoise(x*noiseScale,z*noiseScale); // 0.5-1.5f
							grassHeightScale*= 0.5f+maskC.g;

							//float noise = Mathf.PerlinNoise(x*noiseScale,z*noiseScale);
							float noise = 0.5f;
							
							Vector3 pos = hit.point;

							// pos
							//Debug.DrawLine(pos, pos+new Vector3(0,2,0), Color.green, 5, true);

							// normal
							//Debug.DrawLine(pos, pos+hit.normal, Color.cyan, 5, true);
							//Debug.DrawRay(pos, (pos.normalized)-hit.normal, Color.yellow, 5, true);

							Vector3 fixPos1 = Vector3.Cross(pos.normalized,hit.normal);
							//Vector3 fixPos = GetPerpendicular(pos.normalized);

							//Debug.DrawLine(pos, pos+fixPos1, Color.yellow, 5, true);
							
							// get surface angle dir
							float angle = Vector3.Angle(Vector3.up,hit.normal);
							float d = (0.5f/Mathf.Cos(angle*Mathf.Deg2Rad)); ///Mathf.Acos(angle*Mathf.Deg2Rad);

							
							//Debug.DrawLine(pos, pos-fixPos1, Color.blue, 5, true);
							//Debug.DrawRay(pos, fixPos1*d, Color.yellow, 5, true);
							//Debug.DrawRay(pos, -fixPos1*d, Color.blue, 5, true);


							// TODO: put these planes in helper function..then can build as many planes as want 1, 2x2, 3x3..
							// plane #2
							//							newVerts.Add (pos + new Vector3(0,0,-0.5f)*grassScale);
							//							newVerts.Add (pos + new Vector3(0,0.5f,-0.5f)*grassScale);
							//							newVerts.Add (pos + new Vector3(0,0.5f,0.5f)*grassScale);
							//							newVerts.Add (pos + new Vector3(0,0,0.5f)*grassScale);
							
//							newVerts.Add (pos + new Vector3(0,0,-0.5f));
//							newVerts.Add (pos + new Vector3(0,0.5f,-0.5f));
//							newVerts.Add (pos + new Vector3(0,0.5f,0.5f));
//							newVerts.Add (pos + new Vector3(0,0,0.5f));

//							newVerts.Add (pos+fixPos1*d+OffsetY); 
//							newVerts.Add (pos + new Vector3(0,1f*grassHeightScale,0.5f)); // top 1
//							newVerts.Add (pos + new Vector3(0,1f*grassHeightScale,-0.5f)); // top 2
//							newVerts.Add (pos-fixPos1*d+OffsetY);


							newVerts.Add (pos+fixPos1*(d*grassHeightScale)+OffsetY); 
							newVerts.Add (pos+fixPos1*(d*grassHeightScale) + new Vector3(0,1f*grassHeightScale,0)); // top 1
							newVerts.Add (pos-fixPos1*(d*grassHeightScale) + new Vector3(0,1f*grassHeightScale,0)); // top 2
							newVerts.Add (pos-fixPos1*(d*grassHeightScale)+OffsetY);








							/*
							newUV.Add(new Vector2(0,0));
							newUV.Add(new Vector2(0,1));
							newUV.Add(new Vector2(1,1));
							newUV.Add(new Vector2(1,0));
							*/

							//int tileIndex = 0; // atlas "tile" index

							//atlasTilesX = 1;
							//atlasTilesY = 1;

							float typeNoiseScale = 0.05f;

							// TODO: adjust percentages for each grass type

							// random
							int tileIndexX = (int)(Random.value*atlasTilesX); // atlas "tile" index
							int tileIndexY = (int)(Random.value*atlasTilesY); // atlas "tile" index

							// perlin?
							//tileIndexX = (int)Mathf.PerlinNoise(x*typeNoiseScale,z*typeNoiseScale)*(atlasTilesX+1);
							//tileIndexY = (int)Mathf.PerlinNoise(x*typeNoiseScale,z*typeNoiseScale)*(atlasTilesY+1);

							// mask col
							/*
							float maskVal = maskC.g+(Random.Range(-0.35f,0.35f));

							// mask
							if (maskVal>0.9f)
							{
								tileIndexX=0; // grass 1
								tileIndexY=1;
							}else{
								if (maskVal>0.65f)
								{
									tileIndexX=1; // grass 2
									tileIndexY=1;
								}else{

									if (maskVal>0.45f)
									{
										tileIndexX=0; // grass 3
										tileIndexY=0;
									}else{
										tileIndexX=1; // grass 4
										tileIndexY=0;
									}

								}
							}
							*/


							// percentage probability
							tileIndexX=0;
							tileIndexY=1;


							float rrr = Random.value;


							if (rrr>=0 && rrr<0.75f)
							{
									tileIndexX=0;
									tileIndexY=1;
							}
							if (rrr>0.75f && rrr<0.83f)
							{
								tileIndexX=1;
								tileIndexY=1;
							}
							if (rrr>=0.83f && rrr<0.90f)
							{
								tileIndexX=1;
								tileIndexY=0;
							}
							if (rrr>=0.90f)
							{
								tileIndexX=0;
								tileIndexY=0;
							}

							/*
							
							float rnd = Random.Range(0,100);
							
							
							if (rnd>grassPercentage[0]) // 50
							{
								tileIndexX=0;
								tileIndexY=0;
								p1++;
							}else{
	
								
								if (rnd<grassPercentage[3]) // 5
								{
									tileIndexX=1;
									tileIndexY=1;
									p4++;
									
								}else{
									
									if (rnd<grassPercentage[2]) // 15
									{
										tileIndexX=0;
										tileIndexY=1;
										p3++;
										
									}else{
								
										if (rnd<grassPercentage[1]) // 30
										{
											tileIndexX=1;
											tileIndexY=0;
											p2++;
										}
									}
								}
							}*/

							////grassPercentage[0,
							/*
							grassPercentage[0] = 0.5f;
							grassPercentage[1] = 0.5f;
							grassPercentage[2] = 0.0f;
							grassPercentage[3] = 0.0f;

							int tileIndexX = (int)(Random.value*atlasTilesX); // atlas "tile" index
							int tileIndexY = (int)(Random.value*atlasTilesY); // atlas "tile" index

							for (int ii=0;ii<atlasTilesX*atlasTilesY;ii++)
							{
								if (grassPercentage[ii]<Random.value)
								{
									tileIndexX=ii;
									if (tileIndexX>atlasTilesX)
									{
										tileIndexY+=(int)(tileIndexX/atlasTilesX);
										tileIndexX-=atlasTilesX;
									}
								}
							}*/
							/*
							int[] randomTile = new int[] { 0, 1, 2, 3 };
							int choosenOne = chooseWithChance(4,2,1,1);
							Debug.Log(randomTile[choosenOne]);*/




							//return;



							//							int tileIndexX = 0;

							//if (Mathf.PerlinNoise(x*typeNoiseScale,z*typeNoiseScale)>0.5f) tileIndexX=1;


							//int tileIndexY = 0; // atlas "tile" index

							/*
							newUV.Add(new Vector2(0,0));
							newUV.Add(new Vector2(0,1));
							newUV.Add(new Vector2(1,1));
							newUV.Add(new Vector2(1,0));
							*/
							/*
							int index=1;
							
							int uIndex = index % atlasTilesX;
							int vIndex = index / atlasTilesY;

							newUV.Add(new Vector2(uIndex,vIndex)); // 0,0
							newUV.Add(new Vector2(uIndex,1/atlasTilesY)); // 
							newUV.Add(new Vector2(uIndex+1/atlasTilesX,1/atlasTilesY));
							newUV.Add(new Vector2(uIndex+1/atlasTilesX,vIndex));

							Debug.Log ("uIndex"+uIndex);
							Debug.Log ("vIndex:"+vIndex);
*/
							newUV.Add( new Vector2(0.0f+tileIndexX*(1.0f/atlasTilesX),0.0f+tileIndexY*(1.0f/atlasTilesY)) );
							newUV.Add( new Vector2(0.0f+tileIndexX*(1.0f/atlasTilesX),(1.0f+tileIndexY)*(1.0f/atlasTilesY)) );
							newUV.Add( new Vector2((1.0f+tileIndexX)*(1.0f/atlasTilesX),(1.0f+tileIndexY)*(1.0f/atlasTilesY)) );
							newUV.Add( new Vector2((1.0f+tileIndexX)*(1.0f/atlasTilesX),0.0f+tileIndexY*(1.0f/atlasTilesY)) );

							/*
							Debug.Log ( new Vector2(0.0f+tileIndexX*(1.0f/atlasTilesX),0.0f+tileIndexY*(1.0f/atlasTilesY)) );
							Debug.Log ( new Vector2(0.0f+tileIndexX*(1.0f/atlasTilesX),(1.0f+tileIndexY)*(1.0f/atlasTilesY)) );
							Debug.Log ( new Vector2((1.0f+tileIndexX)*(1.0f/atlasTilesX),(1.0f+tileIndexY)*(1.0f/atlasTilesY)) );
							Debug.Log ( new Vector2((1.0f+tileIndexX)*(1.0f/atlasTilesX),0.0f+tileIndexY*(1.0f/atlasTilesY)) );

							tileIndexX = 1; // atlas "tile" index
							tileIndexY = 0; // atlas "tile" index

							Debug.Log ( new Vector2(0.0f+tileIndexX*(1.0f/atlasTilesX),0.0f+tileIndexY*(1.0f/atlasTilesY)) );
							Debug.Log ( new Vector2(0.0f+tileIndexX*(1.0f/atlasTilesX),(1.0f+tileIndexY)*(1.0f/atlasTilesY)) );
							Debug.Log ( new Vector2((1.0f+tileIndexX)*(1.0f/atlasTilesX),(1.0f+tileIndexY)*(1.0f/atlasTilesY)) );
							Debug.Log ( new Vector2((1.0f+tileIndexX)*(1.0f/atlasTilesX),0.0f+tileIndexY*(1.0f/atlasTilesY)) );
*/

							//return;

							// atlasTilesX

		

							// Vector2 offset = new Vector2 (uIndex * _size.x, 1.0f - _size.y - vIndex * _size.y);
							/*
							newUV.Add(new Vector2(uIndex,vIndex));
							newUV.Add(new Vector2(uIndex,1/atlasTilesY));
							newUV.Add(new Vector2(uIndex+1/atlasTilesX,1/atlasTilesY));
							newUV.Add(new Vector2(uIndex+1/atlasTilesX,vIndex));
*/

							// index=0
							// 0,0 - 0,1/atlasTilesX - 1/atlasTilesX,1/atlasTilesY - 1/atlasTilesX,0

							// TileIndexX=1, TileIndexY=0
							// 0+tileIndexX/atlasTilesX,0+tileIndexY/atlasTilesY
							// 0+tileIndexX/atlasTilesX,1/atlasTilesX - 1/atlasTilesX,1/atlasTilesY - 1/atlasTilesX,0+tileIndexY/atlasTilesY



							//newTris.Add(vertIndex*4+4);
							//							newTris.Add(vertIndex*4+1+4);
							//							newTris.Add(vertIndex*4+2+4);
							//							newTris.Add(vertIndex*4+3+4);
							newTris.Add(vertIndex);
							vertIndex++;
							newTris.Add(vertIndex);
							vertIndex++;
							newTris.Add(vertIndex);
							vertIndex++;
							newTris.Add(vertIndex);
							vertIndex++;
							
							// TODO: vertex colors ? (only alpha should be used for wind speed?)
							newCols.Add(new Color(c.r,c.g,c.b,vertexBottomColor.a)); // bottom
							newCols.Add(vertexTopColor);
							newCols.Add(vertexTopColor);
//							newCols.Add(new Color(0,0,0,0));
//							newCols.Add(new Color(0,0,0,0));
							newCols.Add(new Color(c.r,c.g,c.b,vertexBottomColor.a)); // bottom

//							newCols.Add(new Color(c.r,c.g,c.b,1));
//							newCols.Add(new Color(c.r,c.g,c.b,1));
//							newCols.Add(new Color(c.r,c.g,c.b,1));
//							newCols.Add(new Color(c.r,c.g,c.b,1));


							// 90degree rotate
							fixPos1 = Vector3.Cross(fixPos1,hit.normal);
							
							
							// TODO: move vertex points into end of those "rays"..
							
							//Debug.Log("d:"+d);
							//Debug.DrawLine(pos+fixPos1*d, pos+fixPos1*d+Vector3.up , Color.magenta, 5, true);
							
//							Debug.DrawRay(pos, fixPos1*d, Color.red, 5, true);
//							Debug.DrawRay(pos, -fixPos1*d, Color.cyan, 5, true);

							//Debug.DrawLine(pos, pos+fixPos1, Color.red, 5, true);
							
							//Debug.DrawLine(pos, pos+fixPos1, Color.red, 5, true);
							//Debug.DrawLine(pos, pos-fixPos1, Color.cyan, 5, true);

							//Debug.DrawLine(pos, pos+ new Vector3(-0.5f,0,0)*grassScale, Color.red, 5, false);
							//Debug.DrawLine(pos, pos+ new Vector3(-0.5f,(0.1f+Mathf.Exp(noise)*noise),0)*grassScale, Color.green, 5, false);
							//Debug.DrawLine(pos, pos+ new Vector3(0.5f,(0.1f+Mathf.Exp(noise)*noise),0)*grassScale, Color.blue, 5, false);
							//Debug.DrawLine(pos, pos+ new Vector3(0.5f,0,0)*grassScale, Color.yellow, 5, false);

							if (twoPlanes)
							{
								// plane #2
	//							newVerts.Add (pos + new Vector3(-0.5f,0,0)*grassScale);
	//							newVerts.Add (pos + new Vector3(-0.5f,0.5f,0)*grassScale);
	//							newVerts.Add (pos + new Vector3(0.5f,0.5f,0)*grassScale);
	//							newVerts.Add (pos + new Vector3(0.5f,0,0)*grassScale);

	//							newVerts.Add (pos + new Vector3(-0.5f,0,0)); 
	//							newVerts.Add (pos + new Vector3(-0.5f,1f,0)); // top 1
	//							newVerts.Add (pos + new Vector3(0.5f,1f,0)); // top 2
	//							newVerts.Add (pos + new Vector3(0.5f,0,0));
								
	//							newVerts.Add (pos+fixPos1*d+OffsetY); 
	//							newVerts.Add (pos + new Vector3(-0.5f,1f*grassHeightScale,0)); // top 1
	//							newVerts.Add (pos + new Vector3(0.5f,1f*grassHeightScale,0)); // top 2
	//							newVerts.Add (pos-fixPos1*d+OffsetY);
								
								newVerts.Add (pos+fixPos1*(d*grassHeightScale)+OffsetY);
								newVerts.Add (pos+fixPos1*(d*grassHeightScale) + new Vector3(0,1f*grassHeightScale,0)); // top 1
								newVerts.Add (pos-fixPos1*(d*grassHeightScale) + new Vector3(0,1f*grassHeightScale,0)); // top 2
								newVerts.Add (pos-fixPos1*(d*grassHeightScale)+OffsetY);
								
	//							newUV.Add(new Vector2(0,0));
	//							newUV.Add(new Vector2(0,1));
	//							newUV.Add(new Vector2(1,1));
	//							newUV.Add(new Vector2(1,0));



								newUV.Add( new Vector2(0.0f+tileIndexX*(1.0f/atlasTilesX),0.0f+tileIndexY*(1.0f/atlasTilesY)) );
								newUV.Add( new Vector2(0.0f+tileIndexX*(1.0f/atlasTilesX),(1.0f+tileIndexY)*(1.0f/atlasTilesY)) );
								newUV.Add( new Vector2((1.0f+tileIndexX)*(1.0f/atlasTilesX),(1.0f+tileIndexY)*(1.0f/atlasTilesY)) );
								newUV.Add( new Vector2((1.0f+tileIndexX)*(1.0f/atlasTilesX),0.0f+tileIndexY*(1.0f/atlasTilesY)) );

	//							newTris.Add(vertIndex*4);
	//							newTris.Add(vertIndex*4+1);
	//							newTris.Add(vertIndex*4+2);
	//							newTris.Add(vertIndex*4+3);
								newTris.Add(vertIndex);
								vertIndex++;
								newTris.Add(vertIndex);
								vertIndex++;
								newTris.Add(vertIndex);
								vertIndex++;
								newTris.Add(vertIndex);
								vertIndex++;
								
								// TODO: vertex colors ? (only alpha should be used for wind speed?)
								// TODO: get vertex adjustment color from texture, // FIXME: shader needs to use/show vertex colors and c needs to be taken from that actual texture color (not splatmask color)
								newCols.Add(new Color(c.r,c.g,c.b,vertexBottomColor.a)); // bottom
								newCols.Add(vertexTopColor);
								newCols.Add(vertexTopColor);
	//							newCols.Add(new Color(0,0,0,0));
	//							newCols.Add(new Color(0,0,0,0));
								newCols.Add(new Color(c.r,c.g,c.b,vertexBottomColor.a)); // bottom

	//							newCols.Add(new Color(c.r,c.g,c.b,1));
	//							newCols.Add(new Color(c.r,c.g,c.b,1));
	//							newCols.Add(new Color(c.r,c.g,c.b,1));
	//							newCols.Add(new Color(c.r,c.g,c.b,1));

							}



							

							// build mesh now?
//							if (newVerts.Count>=50000)
							if (newVerts.Count>=32000) // per mesh
							{

								//Debug.DrawLine(pos, pos+new Vector3(0,1111,0), Color.yellow, 5, true);

								vertIndex=0;
								meshCount++;

								Mesh mesh = new Mesh();
//								mesh.Clear(); //?

								GameObject go = new GameObject();
								go.name = "GrassTest"+meshCount.ToString();
								// build grass root object
								go.AddComponent<MeshFilter>();
								go.AddComponent<MeshRenderer>();
								go.GetComponent<MeshRenderer>().material = grassMaterial;
								go.GetComponent<MeshFilter>().mesh = mesh;


								//go.GetComponent<MeshRenderer>().material.SetColor("_WavingTint",c); 

								mesh.vertices = newVerts.ToArray();
								mesh.uv = newUV.ToArray();
								mesh.colors = newCols.ToArray();
								mesh.SetIndices(newTris.ToArray(),MeshTopology.Quads,0);
								;
								mesh.RecalculateNormals();
								calculateMeshTangents(mesh);

								newVerts.Clear();
								newCols.Clear();
								newUV.Clear();
								newTris.Clear();
								
//								float total = p1+p2+p3+p4;
								
//								Debug.Log("p1:"+p1/total*100);
//								Debug.Log("p2:"+p2/total*100);
//								Debug.Log("p3:"+p3/total*100);
//								Debug.Log("p4:"+p4/total*100);


								
								// TESTING: early exit to make it faster

								if (meshCount>=debugLimit)	return;

							}


	
						}else{ // no grass here

							//Debug.DrawLine(hit.point, hit.point+new Vector3(0,100,0), Color.red, 5, false);


						}
						
					} // if raycast hits
					

					
				} // for x
			} // for y

			// build mesh now?
			if (newVerts.Count>0)
			{
				vertIndex=0;
				
				GameObject go = new GameObject();
				go.name = "GrassTestLast";
				Mesh mesh = new Mesh();
				mesh.Clear();

				// build grass root object
				go.AddComponent<MeshFilter>();
				go.AddComponent<MeshRenderer>();
				go.GetComponent<MeshRenderer>().material = grassMaterial;
				go.GetComponent<MeshFilter>().mesh = mesh;
				
				mesh.vertices = newVerts.ToArray();
				mesh.uv = newUV.ToArray();
				
				mesh.colors = newCols.ToArray();
				
				//mesh.triangles = newTris.ToArray();
				mesh.SetIndices(newTris.ToArray(),MeshTopology.Quads,0);
				
				mesh.RecalculateNormals();
				
				calculateMeshTangents(mesh);
				
				newVerts.Clear();
				newCols.Clear();
				newUV.Clear();
				newTris.Clear();
				
			}

			
			/*
			
			// TEST, perlin noise
			
			for (int y=0;y<maxArea;y++)
			{
				for (int x=0;x<maxArea;x++)
				{
					
					float noise = Mathf.PerlinNoise(x*noiseScale,y*noiseScale);
					float noiseX = Mathf.PerlinNoise(x*noiseScale2,y*noiseScale2)*2;
					float noiseY = Mathf.PerlinNoise(x*noiseScale3,y*noiseScale3)*2;
					
					if (noise>0.5f)
					{
						i++;
						// TODO: calculate maximum amount..
						
						// build quad pieces in world space?
						// TODO: get mesh terrain position, raycast?
		
						// TESTpos
						//Vector3 pos = Random.insideUnitSphere*20;
						//pos.y=0;
						// TODO: should have different scale here, spacing value..
						Vector3 pos = new Vector3(x*grassScale+noiseX-1f,0,y*grassScale+noiseY-1f);
						
						// TODO: random rotations
						
						// TODO: amount of planes, scale, noise?
						
						// TODO: try adjusting grass height from noise
						
						
						//Debug.Log(noise);
						
						// plane #1
						newVerts.Add (pos + new Vector3(-0.5f,0,0)*grassScale);
						newVerts.Add (pos + new Vector3(-0.5f,(0.1f+Mathf.Exp(noise)*noise),0)*grassScale);
						newVerts.Add (pos + new Vector3(0.5f,(0.1f+Mathf.Exp(noise)*noise),0)*grassScale);
						newVerts.Add (pos + new Vector3(0.5f,0,0)*grassScale);
		
						newUV.Add(new Vector2(0,0));
						newUV.Add(new Vector2(0,1));
						newUV.Add(new Vector2(1,1));
						newUV.Add(new Vector2(1,0));
		
						newTris.Add(i*4);
						newTris.Add(i*4+1);
						newTris.Add(i*4+2);
						newTris.Add(i*4+3);
						
						// TODO: vertex colors ? (only alpha should be used for wind speed?)
						newCols.Add(new Color(1,1,1,0));
						newCols.Add(new Color(1,1,1,1));
						newCols.Add(new Color(1,1,1,1));
						newCols.Add(new Color(1,1,1,0));
						
						// plane #2
						newVerts.Add (pos + new Vector3(0,0,-0.5f)*grassScale);
						newVerts.Add (pos + new Vector3(0,(0.1f+Mathf.Exp(noise)*noise),-0.5f)*grassScale);
						newVerts.Add (pos + new Vector3(0,(0.1f+Mathf.Exp(noise)*noise),0.5f)*grassScale);
						newVerts.Add (pos + new Vector3(0,0,0.5f)*grassScale);
		
						newUV.Add(new Vector2(0,0));
						newUV.Add(new Vector2(0,1));
						newUV.Add(new Vector2(1,1));
						newUV.Add(new Vector2(1,0));
		
						newTris.Add(i*4+4);
						newTris.Add(i*4+1+4);
						newTris.Add(i*4+2+4);
						newTris.Add(i*4+3+4);
						
						// TODO: vertex colors ? (only alpha should be used for wind speed?)
						newCols.Add(new Color(1,1,1,0));
						newCols.Add(new Color(1,1,1,1));
						newCols.Add(new Color(1,1,1,1));
						newCols.Add(new Color(1,1,1,0));
		
					}
					
				} // for
			} // for

			*/

			//Debug.Log (newVerts.Count);


			
			// TODO: use recalculate tangents script (or assign by hand)

		}

	// helpers

	


	// http://programmers.stackexchange.com/a/203616
	public int chooseWithChance(params int[] args)
	{
		/*
		* This method takes number of chances and randomly chooses
		* one of them considering their chance to be choosen.    
		* e.g. 
		*   chooseWithChance(1,99) will most probably (%99) return 1 since 99 is the second parameter.
		*   chooseWithChance(99,1) will most probably (%99) return 0 since 99 is the first parameter.     
		*/
		int argCount = args.Length;
		int sumOfChances = 0;
		
		for (int i = 0; i < argCount; i++) {
			sumOfChances += args[i];
		}
		
		int random = (int)Random.value*sumOfChances;
		
		while ((random -=args[argCount-1])> 0)
		{
			argCount--;
			sumOfChances -= args[argCount -1];
		}
		
		return argCount-1;
	}	


	Vector3 GetPerpendicular(Vector3 v)
	{
		//return new Vector3(v.z, 0, -v.x); //z
		return new Vector3(-v.y, 0, -v.x);
	}




	// http://answers.unity3d.com/questions/7789/calculating-tangents-vector4.html
	public static void calculateMeshTangents(Mesh mesh)
    {
			
		// TODO: fix leaking object?
			
			
			
	    //speed up math by copying the mesh arrays
	    int[] triangles = mesh.triangles;
	    Vector3[] vertices = mesh.vertices;
	    Vector2[] uv = mesh.uv;
	    Vector3[] normals = mesh.normals;
	     
	    //variable definitions
	    int triangleCount = triangles.Length;
	    int vertexCount = vertices.Length;
	     
	    Vector3[] tan1 = new Vector3[vertexCount];
	    Vector3[] tan2 = new Vector3[vertexCount];
	     
	    Vector4[] tangents = new Vector4[vertexCount];
	     
	    for (long a = 0; a < triangleCount; a += 3)
	    {
	    long i1 = triangles[a + 0];
	    long i2 = triangles[a + 1];
	    long i3 = triangles[a + 2];
	     
	    Vector3 v1 = vertices[i1];
	    Vector3 v2 = vertices[i2];
	    Vector3 v3 = vertices[i3];
	     
	    Vector2 w1 = uv[i1];
	    Vector2 w2 = uv[i2];
	    Vector2 w3 = uv[i3];
	     
	    float x1 = v2.x - v1.x;
	    float x2 = v3.x - v1.x;
	    float y1 = v2.y - v1.y;
	    float y2 = v3.y - v1.y;
	    float z1 = v2.z - v1.z;
	    float z2 = v3.z - v1.z;
	     
	    float s1 = w2.x - w1.x;
	    float s2 = w3.x - w1.x;
	    float t1 = w2.y - w1.y;
	    float t2 = w3.y - w1.y;
	     
	    float r = 1.0f / (s1 * t2 - s2 * t1);
	     
	    Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
	    Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);
	     
	    tan1[i1] += sdir;
	    tan1[i2] += sdir;
	    tan1[i3] += sdir;
	     
	    tan2[i1] += tdir;
	    tan2[i2] += tdir;
	    tan2[i3] += tdir;
	    }
	     
	     
	    for (long a = 0; a < vertexCount; ++a)
	    {
	    Vector3 n = normals[a];
	    Vector3 t = tan1[a];
	     
	    //Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
	    //tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
	    Vector3.OrthoNormalize(ref n, ref t);
	    tangents[a].x = t.x;
	    tangents[a].y = t.y;
	    tangents[a].z = t.z;
	     
	    tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
	    }
	     
	    mesh.tangents = tangents;
    } // tangentHelper
		
		
	


	} // class
	
	
	
	
	
	
} // namespace