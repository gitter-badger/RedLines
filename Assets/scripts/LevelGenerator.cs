﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic; // required for List<T>


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]

public class LevelGenerator : MonoBehaviour 
{



	public Material levelMaterial;
	public float levelLength;
	public float xDetail; public float xVariation;
	public float yVariation;
	public float minGapSize; public float maxGapSize;
	public bool levelDebug;
	
	
	private Player player;
	private static List<SectionData> data = new List<SectionData>();	// world positions for mesh data
	private static float minX; private static float maxX;
	private static float minY; private static float maxY;
	private static float maxMidChange;
	
	private GameObject topMesh;
	private GameObject botMesh;
	
	// alias the furthest and shortest distances in level data
	private static float head {
		get { return data[data.Count-1].Distance; }
	}
	private static float tail {
		get { return data[0].Distance; }
	}
	
	[System.Serializable]
	private class SectionData
	{
		private float dist = 0f;
		private float up = 0f;
		private float mid = 0f;
		private float down = 0f;
		
		private void Calculate()
		{
			up = Mathf.PerlinNoise(dist/50f, Random.value);
			Rescale(ref up, 1f, 0f, maxY/2f, minY/2f);
			
			mid = Mathf.PerlinNoise(dist/50f, Random.value);
			Rescale (ref mid, 1f, 0f, maxMidChange, 0f);
			
			down = Mathf.PerlinNoise(dist/50f, Random.value);
			Rescale(ref down, 1f, 0f, maxY/2f, minY/2f);
		}
		
		private void Rescale(ref float value, float oldMax, float oldMin, float newMax, float newMin)
		{
			float oldRange = oldMax - oldMin;
			float newRange = newMax - newMin;
			value = (((value - oldMin)*newRange)/oldRange)+newMin;
		}
		
		// constructors
		public SectionData(float x)
		{
			dist = x;
			Calculate();

		}
		
		// destructor
		~SectionData()
		{
			
		}
		

		
		public float Distance
		{
			get { return dist; }
			set { 
				dist = value; 
				Calculate();
			}
		}
		
		// return vertices
		public Vector3 Center {
			get { return new Vector3(dist, mid); }
		}
		
		public Vector3 Top {
			get { return new Vector3(dist, mid + up); }
		}
		
		public Vector3 Bottom {
			get { return new Vector3(dist, mid - down); }
		}
		
	}
	
	void OnLevelWasLoaded(int level)
	{
		data.Clear();
	}
	
	private void Start()
	{
		// initialise static level data parameters
		minX = xDetail - xVariation;
		maxX = xDetail + xVariation;
		minY = minGapSize;
		maxY = maxGapSize;
		maxMidChange = yVariation;
	
		// make child objects for containing level meshes
		topMesh = new GameObject("topMesh");
		topMesh.transform.parent = transform;
		topMesh.AddComponent<MeshFilter>();
		topMesh.AddComponent<MeshRenderer>();
		topMesh.GetComponent<MeshRenderer>().material = levelMaterial;
		
		botMesh = new GameObject("botMesh");
		botMesh.transform.parent = transform;
		botMesh.AddComponent<MeshFilter>();
		botMesh.AddComponent<MeshRenderer>();
		botMesh.GetComponent<MeshRenderer>().material = levelMaterial;
		
		// get reference to player script
		player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
		if (player == null)
			Debug.LogError("Could not find player!");
		
		
		// if these are negative then you may be stuck in an inf loop
		if (minX < 0f || maxX < 0f)
			Debug.LogError("MinX and MaxX must be positive values");
		
		
		// generate the first bits of data
		data.Add(new SectionData(transform.position.x));
		while (head < player.transform.position.x + levelLength)
		{
			GenerateDataAtHead();
		}
	}
	
	private void FixedUpdate()
	{
		bool updateMesh = false;
		// keep generating stuff ahead of the player
		if (head < player.transform.position.x + levelLength)
		{
			GenerateDataAtHead();
			updateMesh = true;
		}
		
		// forget old level data
		if (tail < player.transform.position.x - (levelLength/2f))
		{
			DestroyDataAtTail();
			updateMesh = true;
		}
		
		if (levelDebug) DebugDrawLevel();
		
		if (updateMesh) UpdateMesh();
	}
	
	private void GenerateDataAtHead()
	{
		data.Add(new SectionData(head + 10f));
	}
	
	private void DestroyDataAtTail()
	{
		data.RemoveAt(0);
	}
	
	private void DebugDrawLevel()
	{
		for(int i = 0; i < data.Count - 1; i++)
		{
			Debug.DrawLine(data[i].Top, data[i+1].Top);
			Debug.DrawLine(data[i].Bottom, data[i+1].Bottom);
			Debug.DrawLine (data[i].Center, data[i].Top );
			Debug.DrawLine (data[i].Center, data[i].Bottom );
		}
		Debug.DrawLine (data[data.Count-1 ].Center, data[data.Count-1 ].Top );
		Debug.DrawLine (data[data.Count-1 ].Center, data[data.Count-1 ].Bottom );
	}

	SectionData[] debugdata;
	private void UpdateMesh()
	{
		//debugdata = data.ToArray();
		Vector3[] verts = new Vector3[data.Count * 2];
		Vector2[] uvs = new Vector2[data.Count * 2];
		int[] triangles = new int[(data.Count * 2 * 6) - 12];
		
		// verts and uvs
		int v = 0;
		for (int i = 0; i < data.Count; i++)
		{
			verts[v] = data[i].Top; 
			uvs[v++] = new Vector2(v,0);
			verts[v] = data[i].Top + Vector3.up * 10f;
			uvs[v++] = new Vector2(v,1);
		}
		
		// mesh triangles
		int t = 0;
		for (v = 0; v < (data.Count*2) - 2 ; v+=2)
		{
			triangles[t++] = v;
			triangles[t++] = v + 1;
			triangles[t++] = v + 2;
			
			triangles[t++] = v + 3;
			triangles[t++] = v + 2;
			triangles[t++] = v + 1;
		}
		
		Mesh m = topMesh.GetComponent<MeshFilter>().mesh;
		m.Clear();
		m.vertices = verts;
		m.uv = uvs;
		m.triangles = triangles;
		m.RecalculateNormals();
		
		// DO IT ALL AGAIN FOR BOTTOM MESH WOO
		
		// verts and uvs
		v = 0;
		for (int i = 0; i < data.Count; i++)
		{
			verts[v] = data[i].Bottom; 
			uvs[v++] = new Vector2(v,0);
			verts[v] = data[i].Bottom - Vector3.up * 10f;
			uvs[v++] = new Vector2(v,1);
		}
		
		// mesh triangles
		t = 0;
		for (v = 0; v < (data.Count*2) - 2 ; v+=2)
		{
			triangles[t++] = v + 1;
			triangles[t++] = v;
			triangles[t++] = v + 2;
			
			triangles[t++] = v + 1;
			triangles[t++] = v + 2;
			triangles[t++] = v + 3;
		}
		
		m = botMesh.GetComponent<MeshFilter>().mesh;
		m.Clear();
		m.vertices = verts;
		m.uv = uvs;
		m.triangles = triangles;
		m.RecalculateNormals();
		
	}
}
