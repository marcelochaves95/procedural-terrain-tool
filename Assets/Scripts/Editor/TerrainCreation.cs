﻿using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;

[CanEditMultipleObjects]
public class TerrainCreation : EditorWindow {

    public int maxHeight;
    public int xSize;
    public int ySize;
    private int gridResolution = 1024;
    private static int octavees = 8;
    public List<int> triangles = new List<int>();

    private static float noiseScale = 1.0f;
    
    public bool flat;

    public List<Vector3> vertex = new List<Vector3>();
    public List<Vector3> normals = new List<Vector3>();
    
    public List<Vector2> uvs = new List<Vector2>();

    private Color[] pix;
    public List<Color> colors = new List<Color>();
    
    private static Texture2D noiseTex;
    public Texture2D heightMap;

    public MeshFilter mesh;

	[MenuItem("Terrain/Create Terrain...")]
	static void Init () {
		EditorWindow.GetWindow<TerrainCreation>().Show();
	}

	public void OnGUI () {
        EditorGUILayout.LabelField("Texture");

		noiseScale = EditorGUILayout.FloatField("Noise Scale", noiseScale);

		octavees = EditorGUILayout.IntSlider("Number of Octavees", octavees, 0, 8);

        if (GUILayout.Button("Create New Texture")) {
            CreateNoiseTexture();
        }

        EditorGUILayout.LabelField("Terrain");
        
        xSize = EditorGUILayout.IntField("X Size: ", xSize);

        ySize = EditorGUILayout.IntField("Y Size: ", ySize);

        maxHeight = EditorGUILayout.IntSlider("Max Height: ", maxHeight, 1, 100);

        flat = EditorGUILayout.Toggle("Terrain Flat: ", flat);
        
        GUILayout.Label("Select Plane: ");
        mesh = (MeshFilter)EditorGUILayout.ObjectField(mesh, typeof(MeshFilter));
        GUILayout.Label("Select HeightMap: ");
        heightMap = (Texture2D)EditorGUILayout.ObjectField(heightMap, typeof(Texture2D));
        if (GUILayout.Button("Generate Terrain")) {
            GenerateProceduralTerrain();
        }

    }

	private void CreateNoiseTexture () {
		noiseTex = new Texture2D(gridResolution, gridResolution);
		pix = new Color[gridResolution * gridResolution];
		float xOri = Random.value * 100000.0f;
		float yOri = Random.value * 100000.0f;
		float y = 0.0f;
		while (y < noiseTex.height) {
			float x = 0.0f;
			while (x < noiseTex.width) {
				float sample = OctaveesNoise2D(xOri + x / noiseTex.width, yOri + y / noiseTex.height, octavees, 1.0f, 0.75f);
				pix[(int) y * noiseTex.width + (int) x] = new Color(sample, sample, sample);
				x++;
            }
            y++;
        }
        noiseTex.SetPixels(pix);
        noiseTex.Apply();
		byte[] bytes = noiseTex.EncodeToPNG ();
		Debug.Log("Creating Terrain Texture: " + Application.dataPath + "/TerrainTexture.png");
		File.WriteAllBytes(Application.dataPath + "/TerrainTexture.png", bytes);
		AssetDatabase.ImportAsset("Assets/TerrainTexture.png");
	}

	public float OctaveesNoise2D (float x, float y, int octNum, float frq, float amp) {
		float gain = 1.0f;
		float sum = 0.0f;
		for (int i = 0; i < octNum; i++) {
			sum +=  Mathf.PerlinNoise(x * gain / frq, y * gain / frq) * amp / gain;
			gain *= 2.0f;
		}
		return sum;
	}

    /// <summary>
    /// Método que reseta tudo
    /// </summary>
    public void ClearLists () {
        vertex.Clear();
        triangles.Clear();
        normals.Clear();
        colors.Clear();
        uvs.Clear();
    }

    /// <summary>
    /// Método para criar o terreno procedural
    /// </summary>
    public void GenerateProceduralTerrain () {
        ClearLists();
        mesh.sharedMesh = new Mesh();
        mesh.sharedMesh.name = "Procedural Grid";
        CreateVertex();

        if (flat) {
            CalculateNormalsFlat();
        } else {
            CalculateNormalsSmooth();
        }

        Debug.Log("Terrain Created");
    }

    /// <summary>
    /// Método para criar a malha
    /// </summary>
    private void CreateVertex () {
        float constant;
        for (int z = 0; z <= ySize - 1; z++) {
            for (int x = 0; x <= xSize - 1; x++) {
                Vector2 uv = new Vector2((float)x / (float)xSize, (float)z / (float)ySize);
                float height = heightMap.GetPixelBilinear(uv.x, uv.y).grayscale * maxHeight;
                vertex.Add(new Vector3(x, height, z));
                uvs.Add(uv);
                if (height <= maxHeight * 0.45) {
                    colors.Add(Color.green);
                } else if (height > maxHeight * 0.45 && height <= maxHeight * 0.55) {
                    constant = (height - (maxHeight * 0.45f)) * 10;
                    colors.Add(Color.Lerp(Color.green, Color.red, constant));
                } else if (height > maxHeight * 0.55 && height <= maxHeight * 0.75) {
                    colors.Add(Color.red);
                } else if (height > maxHeight * 0.75 && height < maxHeight * 0.85) {
                    constant = (height - (maxHeight * 0.75f)) * 10;
                    colors.Add(Color.Lerp(Color.red, Color.blue, constant));
                } else if (height > maxHeight * 0.85 && height <= maxHeight) {
                    colors.Add(Color.blue);
                }
            }
        }
        mesh.sharedMesh.vertices = vertex.ToArray();
        mesh.sharedMesh.colors = colors.ToArray();
        mesh.sharedMesh.uv = uvs.ToArray();
        for (int i = 0; i <= (xSize - 1) * (ySize - 1); i++) {
            if (i % xSize == 0) {
                triangles.Add(i);
                triangles.Add(i + xSize);
                triangles.Add(i + 1);
            } else if ((i + 1) % xSize == 0) {
                triangles.Add(i);
                triangles.Add(i + xSize - 1);
                triangles.Add(i + xSize);
            } else {
                triangles.Add(i);
                triangles.Add(i + xSize);
                triangles.Add(i + 1);

                triangles.Add(i);
                triangles.Add(i + xSize - 1);
                triangles.Add(i + xSize);
            }
        }
        mesh.sharedMesh.triangles = triangles.ToArray();
    }

    void CreateMeshRenato () {
        /*List<int> n;
        List<int> v;
        List<int> i;
        for (x = 0; x < max - 1; x++)
        {
            for (z = 0; z < max - 1; z++)
            {
                Vector3 v1 = new Vector3(x, 0, 2);
                Vector3 v1 = new Vector3(x, 0, 2 + 1);
                Vector3 v2 = new Vector3(x, 0, 2 + 1);
                Vector3 v3 = new Vector3(x, 0, 2 + 1);
                int index > v.lenght();

                v.Add(v1);
                v.Add(v2);
                v.Add(v3);
                i.Add(index + 1);
                i.Add(index + 2);
                i.Add(index + 3);
            }
        }*/
    }

    /// <summary>
    /// Método para calcular as normais smooth
    /// </summary>
    private void CalculateNormalsSmooth () {
        for (int i = 0; i < vertex.Count; i++) {
            Vector3 normalMed = new Vector3(0, 0, 0);
            int flag = 0;
            for (int j = 0; j < triangles.Count - 3; j += 3) {
                if (i != triangles[j] && i != triangles[j + 1] && i != triangles[j + 2]) {
                    continue;
                }
                Vector3 v1 = vertex[triangles[j + 1]] - vertex[triangles[j]];
                Vector3 v2 = vertex[triangles[j + 2]] - vertex[triangles[j]];
                Vector3 normal = Vector3.Cross(v1, v2);
                normalMed += normal;
                flag++;
            }
            normalMed = normalMed / flag;
            normalMed.Normalize();
            normals.Add(normalMed);
        }
        mesh.sharedMesh.normals = normals.ToArray();
        Debug.Log(normals.Count);
    }

    /// <summary>
    /// Método para calcular as normais flat
    /// </summary>
    private void CalculateNormalsFlat () {
        for (int i = 0; i < vertex.Count - 3; i += 3) {
            Vector3 normalMed = new Vector3(0, 0, 0);
            int flag = 0;
            for (int j = 0; j < triangles.Count - 3; j += 3) {
                if (i != triangles[j] && i != triangles[j + 1] && i != triangles[j + 2]) {
                    continue;
                }
                Vector3 v1 = vertex[triangles[j + 1]] - vertex[triangles[j]];
                Vector3 v2 = vertex[triangles[j + 2]] - vertex[triangles[j]];
                Vector3 normal = Vector3.Cross(v1, v2);
                normalMed += normal;
                flag++;
            }
            normalMed = normalMed/flag;
            normalMed.Normalize();
            normals.Add(normalMed);
            normals.Add(normalMed);
            normals.Add(normalMed);
            if (i >= vertex.Count - 5) {
                normals.Add(normalMed);
            }
        }
        mesh.sharedMesh.normals = normals.ToArray();
    }
}