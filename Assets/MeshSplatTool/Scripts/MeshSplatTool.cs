using UnityEngine;
using UnityEditor;
using System.IO;

[RequireComponent(typeof(MeshCollider))]
[System.Serializable]
public class MeshSplatTool : MonoBehaviour 
{
		// ** SETUP **
	public int maskResolution = 512; // splatmap mask texture resolution
	public float flatRangeMin = 0.0f;
	public float flatRangeMax = 10.0f;
	public float mediumRangeMax = 50.0f;
	public float steepRangeMax = 90.0f;

	public bool paintMode = false; // is paint tool enabled
	public int paintLayer = 0;
	public Texture2D paintBrush; // texture paint brush
	public int brushSize = 4;
	public float brushStrengh = 1.0f;
	public bool sameStroke = false;

	public bool checkColliderObjects = false;
	public int objCollidersLayer = 0;

	public bool realTimePreviewEnabled = false;

	public float obstacleRadius = 10.0f;
	
	public float blurStrength = 3.0f;

	public GameObject distributePrefab;
	public int objCount = 10;



	// ** private varialbles **

	private float flatRangeMinOld = 0.0f;
	private float flatRangeMaxOld = 10.0f;
	private float mediumRangeMaxOld = 50.0f;
	private float steepRangeMaxOld = 90.0f;

	private int maskPreviewResolution = 128; // realtime preview res multiplier 
	private bool isDirty = false;
	
	private bool checkColliderObjectsOld = false;
	private int objCollidersLayerOld = 0;

	public bool saveFile = true;
	public bool doBlur = false;
	
	private Texture2D tex;
	private float[,] faceAngles;
	private Color[] pixels; // keep array of pixels
	private Color[] brushPixels; // array of brush pixels



	// *** main splatmask creation function ***
	public void createSplatMask(int texResolution)
	{
		// take current game object (must be better way..), maybe this.gameObject
		
		GameObject go = Selection.activeGameObject;
		
		// check: we have selection?
		// check: has collider?
		
		// create texture
		/*
			if (realTimePreviewEnabled)
			{
				maskResolution = maskPreviewResolution;
			}
			*/
		
		tex = new Texture2D(texResolution,texResolution,TextureFormat.ARGB32, true);
		tex.hideFlags = HideFlags.DontSave;
		
		Bounds area = Selection.activeGameObject.GetComponent<Renderer>().bounds;
		
		//		Debug.Log("bounds:"+area.min+" to "+area.max+" , size:"+area.size.x);
		
		faceAngles = new float[texResolution,texResolution];
		pixels = new Color[texResolution*texResolution];
		
		//		float pixelStep = area.size.x/maskResolution;
		float pixelStepX = area.size.x/texResolution;
		float pixelStepZ = area.size.z/texResolution;
		
		Mesh mesh = Selection.activeGameObject.GetComponent<MeshFilter>().sharedMesh;
		Vector3[] normals = mesh.normals;
		int[] triangles = mesh.triangles;
		
		RaycastHit hit;
		int xx=0;
		int yy=0;
		
		for (float z=area.min.z+pixelStepZ/2;z<area.max.z;z+=pixelStepZ)
		{
			xx=0;
			for (float x=area.min.x+pixelStepX/2;x<area.max.x;x+=pixelStepX)
			{
				float r = 0;
				float g = 0;
				float b = 0;
				float a = 0;
				
				//Debug.DrawLine(new Vector3(x,100,y), new Vector3(x,0,y), Color.red, 5);
				
				//if (Physics.Raycast(new Vector3(x,200,y), -Vector3.up, out hit, 9999.0F))
				Ray ray = new Ray(new Vector3(x,area.max.y + 1,z), -Vector3.up); // FIXME: 9999 could be taken from maxheight
				if (Selection.activeGameObject.GetComponent<Collider>().Raycast(ray, out hit, 19999.0F)) // FIXME: 19999 could be taken from maxheight
				{
					//					Debug.DrawRay(new Vector3(x,100,y), transform.TransformPoint(hit.point), Color.red, 5);
					//Debug.DrawLine(new Vector3(x,100,y), hit.point, Color.blue, 5);
					
					//Debug.DrawLine(new Vector3(x,100,y), new Vector3(x,0,y), Color.green, 5);
					
					Vector3 n0 = normals[triangles[hit.triangleIndex * 3 + 0]];
					Vector3 n1 = normals[triangles[hit.triangleIndex * 3 + 1]];
					Vector3 n2 = normals[triangles[hit.triangleIndex * 3 + 2]];
					Vector3 baryCenter = hit.barycentricCoordinate;
					Vector3 interpolatedNormal = n0 * baryCenter.x + n1 * baryCenter.y + n2 * baryCenter.z;
					interpolatedNormal = interpolatedNormal.normalized;
					Transform hitTransform = hit.collider.transform;
					interpolatedNormal = hitTransform.TransformDirection(interpolatedNormal);
					
					//Debug.DrawRay(hit.point, interpolatedNormal*5, Color.yellow, 5);
					
					//float distanceToGround = hit.distance;
					//float a = Vector3.Angle(hit.normal, transform.forward);
					float angle = Vector3.Angle(interpolatedNormal, Selection.activeGameObject.transform.forward);
					//InverseLerp 
					
					faceAngles[xx,yy] = angle;
					
					
					// get colors
					if (angle<=flatRangeMax)
					{
						r=1;
					}
					
					if (angle>flatRangeMax & angle<=mediumRangeMax)
					{
						g=1;
					}
					
					if (angle>mediumRangeMax)
					{
						b=1;
					}
					
					// a = road mask
					//float p = Mathf.PerlinNoise(xx*0.01f,yy*0.01f);
					//a = p>0.5f&&p<0.6f?a=1:a=0;
					
					// check for obstacles around hitpoint
					if (checkColliderObjects)
					{
						LayerMask layerMask = 1<<objCollidersLayer; //LayerMask.LayerToName(objCollidersLayer);
						Collider[] hitColliders = Physics.OverlapSphere(hit.point, obstacleRadius,layerMask);
						if (hitColliders.Length>0)
						{
							a=1;
						}
					}					
					
					pixels[xx+yy*texResolution] = new Color(r,g,b,a);
				}else{
					// set a default color anyway to suport holes in the surface and to avoid black seams
					pixels[xx+yy*texResolution] = new Color(1,0,0,0);
					
					//r=1;
					//b=1;
					//Debug.DrawLine(new Vector3(x,100,y), new Vector3(x,0,y), Color.yellow, 5);
				}
				// always increment, to support not rectangular surfaces
				xx++;
				
				//tex.SetPixel((int)x,(int)y,new Color(r,g,b,1));
				//Debug.Log((int)x+(int)y*(int)resolution);
				//Debug.Log("xx"+xx+" | "+xx+yy*(int)resolution);
				
			}
			yy++;
		}
		
		// ROAD , houses? mask
		
		tex.SetPixels(pixels);
		tex.Apply();
		
		// blurring
		if (doBlur)
		{
			// TODO: blur alpha?
			tex = Gaussian.FilterProcessImage(blurStrength,tex);
		}
		
		// apply to object?
		// check if has _mask?
		// if(m.HasProperty("_MainTex"))
		Selection.activeGameObject.GetComponent<Renderer>().sharedMaterial.SetTexture("_Mask", tex);
		//Selection.activeGameObject.renderer.material.SetTexture("_Mask", tex);
		
		// 0 - 90
		//Debug.Log("minmax:"+mina+"/"+maxa);
		
		// save texture
		if (saveFile)
		{
			isDirty = true;
			saveToDisk();
		}
		
		// assign texture?
		//		renderer.material.SetTexture("_Mask", tex);
		
		//DestroyImmediate(tex);
	}
	
	// *** helper functions ***
	
	// make realtime preview, if enabled
	public void realTimePreview()
	{
		// TODO: option for enable/disable
		// TODO: option for lower resolution live preview (64x64?)
		// TODO: disable colliders for live preview
		// TODO: need to press create texture at the end? (to get higher resolution..)
		
		// HACK: just compare with previous angle values..
		if (flatRangeMinOld!=flatRangeMin || flatRangeMaxOld !=flatRangeMax || mediumRangeMaxOld != mediumRangeMax || steepRangeMaxOld != steepRangeMax || checkColliderObjectsOld != checkColliderObjects || objCollidersLayerOld != objCollidersLayer)
		{
			createSplatMask(maskPreviewResolution);
		}
	}
	
	// TODO: take filename parameter ?
	public void saveToDisk()
	{


		
		// setdirty instead? http://answers.unity3d.com/questions/39515/how-do-i-detect-that-monobehaviour-is-modified-in.html
		
		//			Debug.Log ("savetodisk");
		
		
		if (!isDirty) return;
		//			Debug.Log ("saving..");
		
		// TODO: check if folder exists
		byte[] bytes = tex.EncodeToPNG();
		File.WriteAllBytes(Application.dataPath + "/SplatMask.png", bytes);
		
		AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);	// needed?

		isDirty = false;
	}

	public void distributeObjects() {
		// place around the terrain, TODO: check splatmap, keep distances..
		//for (int oc=0;oc<objCount;oc++)
		int maxLoop = 1000;
		Texture2D splatTex = Selection.activeGameObject.GetComponent<Renderer>().sharedMaterial.GetTexture ("_Mask") as Texture2D;
		splatTex.hideFlags = HideFlags.DontSave;
		
		GameObject tempGO = new GameObject("TempInstanceFolder");
		int counter = objCount;
		
		//Vector3[] tempVerts = Selection.activeGameObject.GetComponent<MeshFilter>().sharedMesh.vertices;
		
		while (counter>0 && maxLoop>0)
		{
			maxLoop--;
			RaycastHit hit;
			float minx = Selection.activeGameObject.GetComponent<Renderer>().bounds.min.x;
			float minz = Selection.activeGameObject.GetComponent<Renderer>().bounds.min.z;
			float maxx = Selection.activeGameObject.GetComponent<Renderer>().bounds.max.x;
			float maxz = Selection.activeGameObject.GetComponent<Renderer>().bounds.max.z;
			
			Ray ray = new Ray(new Vector3(Random.Range(minx,maxx),200,Random.Range(minz,maxz)), -Vector3.up);
			if (Selection.activeGameObject.GetComponent<Collider>().Raycast(ray, out hit, 9999.0F))
			{
				
				Vector2 pixelUV = hit.textureCoord;
				pixelUV.x *= splatTex.width;
				pixelUV.y *= splatTex.height;
				
				Color c = splatTex.GetPixel((int)pixelUV.x, (int)pixelUV.y);
				
				// filter by ground texture.. TODO: might not have any of this color..
				if (Mathf.Approximately(c.a,0.0f) && c.g>0)
				{
					GameObject clone = PrefabUtility.InstantiatePrefab(distributePrefab) as GameObject;
					clone.transform.position = hit.point; //,Quaternion.identity) as Transform;
					counter--;
					clone.transform.parent = tempGO.transform;
				}
			}
		}
		
		if (maxLoop<1) Debug.Log("MeshSplatTool error: Object distribution failed..max loop reached");
	}
	

	// new paint function, supports brush size, uses setpixels
	public void paint(Vector2 p) 
	{


		// TODO: do this only when brush is changed, TODO: if texture is not readable, ask to set it
		if (paintBrush==null)
		{
			// TODO: do painting in same function, depending on the brush type selection
			paint_orig(p);
			return;
		}else{
			brushPixels = paintBrush.GetPixels();
		}


		p.x *= tex.width;
		p.y *= tex.height;
		
		
		// get position where we paint
		int startX=(int)(p.x-(paintBrush.width*0.5f));
		int startY=(int)(p.y-(paintBrush.height*0.5f));
		
		if (startX<0) startX = 0;
		if (startX+paintBrush.width>=tex.width) startX = tex.width-paintBrush.width;
		
		if (startY<0) startY = 0;
		if (startY+paintBrush.height>=tex.height) startY = tex.height-paintBrush.height;
		
		Color paint = Color.black;
		if (paintLayer == 1)  paint = new Color(brushStrengh,0,0,0); // red channel
		if (paintLayer == 2)  paint = new Color(0,brushStrengh,0,0); // green
		if (paintLayer == 3)  paint = new Color(0,0,brushStrengh,0); // blue
		if (paintLayer == 4)  paint = new Color(0,0,0,brushStrengh); // alpha
		
		//Debug.Log ("paint:"+p+ "startXY:"+startX+","+startY+" pix:"+tex.GetPixel((int)p.x,(int)p.y));
		
		for(int i=0;i<paintBrush.height;i++)
		{
			for(int j=0;j<paintBrush.width;j++)
			{

				//pixels[(startY+i)*tex.width+startX+j] = texPix[  (startY+i)*texwidth+startX+j] * (1-splatPix[j*splatwidth+i].a) + splatPix[j*splatwidth+i]*splatPix[j*splatwidth+i].a;
				pixels[(startY+i)*tex.width+startX+j] = Color.Lerp( pixels[(startY+i)*tex.width+startX+j] , paint , brushPixels[j*paintBrush.width+i].a );


				/*
				// TODO: optimize, add brushSizeHalf var.., support for procedural brush shapes (circular, box, randomnoise?)
				int d=(int)Mathf.Sqrt((i-brushSize*0.5f)*(i-brushSize*0.5f)+(j-brushSize*0.5f)*(j-brushSize*0.5f));
				if(d<brushSize*0.5f)
				{
					pixels[(startY+i)*tex.width+startX+j] = paint; 
				}*/
			}
		}
		
		tex.SetPixels(pixels); 
		tex.Apply(); // could use (false) no need mipmaps ?
		isDirty = true;
	}

	//
	public void paint_orig(Vector2 p) 
	{
		
		p.x *= tex.width;
		p.y *= tex.height;
		
		
		// get position where we paint
		int startX=(int)(p.x-(brushSize*0.5f));
		int startY=(int)(p.y-(brushSize*0.5f));
		
		if (startX<0) startX = 0;
		if (startX+brushSize>=tex.width) startX = tex.width-brushSize;
		
		if (startY<0) startY = 0;
		if (startY+brushSize>=tex.height) startY = tex.height-brushSize;
		
		Color paint = Color.black;
		if (paintLayer == 1)  paint = new Color(brushStrengh,0,0,0); // red channel
		if (paintLayer == 2)  paint = new Color(0,brushStrengh,0,0); // green
		if (paintLayer == 3)  paint = new Color(0,0,brushStrengh,0); // blue
		if (paintLayer == 4)  paint = new Color(0,0,0,brushStrengh); // alpha
		
		//Debug.Log ("paint:"+p+ "startXY:"+startX+","+startY+" pix:"+tex.GetPixel((int)p.x,(int)p.y));
		
		for(int i=0;i<brushSize;i++)
		{
			for(int j=0;j<brushSize;j++)
			{
				// TODO: optimize, add brushSizeHalf var.., support for procedural brush shapes (circular, box, randomnoise?)
				int d=(int)Mathf.Sqrt((i-brushSize*0.5f)*(i-brushSize*0.5f)+(j-brushSize*0.5f)*(j-brushSize*0.5f));
				if(d<brushSize*0.5f)
				{
//					pixels[(startY+i)*tex.width+startX+j] = paint; 
					// *0.1f is to slow it down more
					pixels[(startY+i)*tex.width+startX+j] = Color.Lerp(pixels[(startY+i)*tex.width+startX+j],paint,brushStrengh*0.1f);
				}
			}
		}
		
		tex.SetPixels(pixels); 
		tex.Apply(); // could use (false) no need mipmaps ?
		isDirty = true;
	}
	

	// do gaussian blur
	public void blur() {
		// blur selected layer or all
		tex = Gaussian.FilterProcessImage(blurStrength, tex);
		pixels = tex.GetPixels(); // update pixels array also
		Selection.activeGameObject.GetComponent<Renderer>().sharedMaterial.SetTexture("_Mask", tex);
		
		isDirty = true;
		saveToDisk();
	}

	// load splatmask texture
	public void loadSplatmap() {
		if (tex == null) {
			tex = new Texture2D(maskResolution, maskResolution, TextureFormat.ARGB32, true);
			tex.hideFlags = HideFlags.DontSave;
			
			Texture2D mask = Selection.activeGameObject.GetComponent<Renderer>().sharedMaterial.GetTexture ("_Mask") as Texture2D;
			if (mask == null) {
				Debug.LogWarning("Can't locate the Splatmap texture from current shader. Are you using a different shader?");
			} else {
				string path = AssetDatabase.GetAssetPath(mask);
				path = "file://" + Application.dataPath + path.Substring(6);
				Debug.Log(Application.dataPath + path.Substring(6));
				WWW www = new WWW(path);
				while(!www.isDone);
				www.LoadImageIntoTexture(tex);
			}
		}
	}

	// save texture to undo, before each paint stroke (but not each invidual paint event)
	public void registerTextureUndo()
	{
		if (!sameStroke)
		{
			//Debug.Log ("registering undo for texture");
			sameStroke=true; // we have registered the start of the paint
			// FIXME: this is too slow for even 512x512 texture
			//Undo.RecordObject(tex,"SplatMask Paint");
		}
	}

}
