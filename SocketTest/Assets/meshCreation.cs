using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class meshCreation : MonoBehaviour
{
    public Vector3[] newVertices;
    public Vector2[] newUV;
    public Vector3[] meshNormals;
    public int[] newTriangles;

    public bool createMesh = false;
    public GameObject objToClone;
    public dataReader reader;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void generateMesh() {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        Mesh meshToClone = objToClone.GetComponent<MeshFilter>().mesh;
        mesh.vertices = newVertices;
        //mesh.uv = meshToClone.uvs;
        for (int i = 0; i < newTriangles.Length / 6; i++) {
            newTriangles[i * 6 + 0] = i * 2;
            newTriangles[i * 6 + 1] = i * 2 + 1;
            newTriangles[i * 6 + 2] = i * 2 + 2;

            newTriangles[i * 6 + 3] = i * 2 + 2;
            newTriangles[i * 6 + 4] = i * 2 + 1;
            newTriangles[i * 6 + 5] = i * 2 + 3;
        }
        mesh.triangles = newTriangles;
        //mesh.normals = meshToClone.normals;
        Debug.Log(mesh.subMeshCount);
        /*newVertices = mesh.vertices;
        newUV = mesh.uv;
        newTriangles = mesh.triangles;*/
        meshNormals = mesh.normals;
    }

    // Update is called once per frame
    void Update()
    {
        if (createMesh)
        {
            createMesh = false;
            generateMesh();
        }
    }
}
