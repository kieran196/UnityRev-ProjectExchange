using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class meshCreation : MonoBehaviour
{
    [SerializeField]
    public List<ElementMesh> elementMeshArr = new List<ElementMesh>();

    public Vector2[] newUV;
    public Vector3[] meshNormals;

    public bool createMesh = false;
    public bool createAllMeshes = false;
    public bool createMeshFromReader = false;
    public GameObject objToClone;
    public dataReader reader;

    public Vector3[] clonedVerts;
    public int[] clonedTris;
    public int[] actualTris;
    public List<int> testedTris = new List<int>();
    public int index = 0;
    // Start is called before the first frame update
    void Start()
    {
        /*elementMeshArr = new ElementMesh[30]; // Just for testing purposes..
        for (int i=0; i<elementMeshArr.Length; i++) {
            elementMeshArr[i] = new ElementMesh();
        }*/
    }
    
    void generateAllMeshes() {
        for (int i=0; i<elementMeshArr.Count; i++) {
            GameObject obj = new GameObject();
            obj.AddComponent<MeshRenderer>();
            Mesh mesh = obj.AddComponent<MeshFilter>().mesh;
            obj.name = elementMeshArr[i].ID;
            mesh.vertices = elementMeshArr[i].vertices;
            System.Array.Reverse(elementMeshArr[i].triangles);
            mesh.triangles = elementMeshArr[i].triangles;
            /*for (int n = 0; n < elementMeshArr[i].triangles.Length / 6; n++) {
                elementMeshArr[i].triangles[n * 6 + 0] = n * 2;
                elementMeshArr[i].triangles[n * 6 + 1] = n * 2 + 1;
                elementMeshArr[i].triangles[n * 6 + 2] = n * 2 + 2;

                elementMeshArr[i].triangles[n * 6 + 3] = n * 2;
                elementMeshArr[i].triangles[n * 6 + 4] = n * 2 + 2; // Changed
                elementMeshArr[i].triangles[n * 6 + 5] = n * 2 + 3;
            }
            mesh.triangles = elementMeshArr[i].triangles;*/
        }
    }
    public Vector3[] actualVerts;
    void generateMeshFromReaderData()
    {
        clonedTris = objToClone.GetComponent<MeshFilter>().mesh.triangles;
        clonedVerts = objToClone.GetComponent<MeshFilter>().mesh.vertices;
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        Mesh meshToClone = objToClone.GetComponent<MeshFilter>().mesh;
        mesh.vertices = reader.vertices;
        mesh.vertices = clonedVerts;
        //System.Array.Reverse(reader.triangles);
        mesh.triangles = reader.triangles;
        actualVerts = mesh.vertices;
        actualTris = mesh.triangles;
    }

    void generateMesh() {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        Mesh meshToClone = objToClone.GetComponent<MeshFilter>().mesh;
        clonedTris = meshToClone.triangles;
        /*mesh.vertices = elementMeshArr[index].vertices;
        //mesh.uv = meshToClone.uvs;
        for (int i = 0; i < elementMeshArr[index].triangles.Length / 6; i++) {
            elementMeshArr[index].triangles[i * 6 + 0] = i * 2;
            elementMeshArr[index].triangles[i * 6 + 1] = i * 2 + 1;
            elementMeshArr[index].triangles[i * 6 + 2] = i * 2 + 2;

            elementMeshArr[index].triangles[i * 6 + 3] = i * 2;
            elementMeshArr[index].triangles[i * 6 + 4] = i * 2 + 2; // Changed
            elementMeshArr[index].triangles[i * 6 + 5] = i * 2 + 3;
        }
        mesh.triangles = elementMeshArr[index].triangles;*/
        mesh.vertices = reader.vertices;
        //mesh.triangles = new int[204];
        //for (int i = 0; i < reader.triangles.Length / 6; i++) {
        int[] tris = new int[mesh.vertices.Length * 3];
        for (int i = 0; i < (mesh.vertices.Length * 3) / 6; i++) {
            tris[i * 6 + 0] = i * 2;
            tris[i * 6 + 1] = i * 2 + 1;
            tris[i * 6 + 2] = i * 2 + 2;

            tris[i * 6 + 3] = i * 2 + 2;
            tris[i * 6 + 4] = i * 2 + 1;
            tris[i * 6 + 5] = i * 2 + 3;
        }
        mesh.triangles = tris;
        actualTris = mesh.triangles;

        //mesh.normals = meshToClone.normals;
        //Debug.Log(mesh.subMeshCount);
        /*newVertices = mesh.vertices;
        newUV = mesh.uv;
        newTriangles = mesh.triangles;*/
        //meshNormals = mesh.normals;
    }

    // Update is called once per frame
    void Update()
    {
        if (createAllMeshes) {
            createAllMeshes = false;
            generateAllMeshes();

        }
        if (createMesh)
        {
            createMesh = false;
            generateMesh();
        }
        if (createMeshFromReader) {
            createMeshFromReader = false;
            generateMeshFromReaderData();
        }
    }
}
