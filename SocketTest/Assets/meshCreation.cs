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
    public GameObject objToClone;
    public dataReader reader;

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
            mesh.vertices = elementMeshArr[i].vertices;
            for (int n = 0; n < elementMeshArr[i].triangles.Length / 6; n++) {
                elementMeshArr[i].triangles[n * 6 + 0] = n * 2;
                elementMeshArr[i].triangles[n * 6 + 1] = n * 2 + 1;
                elementMeshArr[i].triangles[n * 6 + 2] = n * 2 + 2;

                elementMeshArr[i].triangles[n * 6 + 3] = n * 2;
                elementMeshArr[i].triangles[n * 6 + 4] = n * 2 + 2; // Changed
                elementMeshArr[i].triangles[n * 6 + 5] = n * 2 + 3;
            }
            mesh.triangles = elementMeshArr[i].triangles;
        }
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
        mesh.triangles = reader.triangles;
        for (int i = 0; i < reader.triangles.Length / 6; i++) {
            reader.triangles[i * 6 + 0] = i * 2;
            reader.triangles[i * 6 + 1] = i * 2 + 1;
            reader.triangles[i * 6 + 2] = i * 2 + 2;

            reader.triangles[i * 6 + 3] = i * 2 + 2;
            reader.triangles[i * 6 + 4] = i * 2 + 1;
            reader.triangles[i * 6 + 5] = i * 2 + 3;
            /*reader.triangles[i * 6 + 0] = i * 2;
            reader.triangles[i * 6 + 1] = i * 2 + 1;
            reader.triangles[i * 6 + 2] = i * 2 + 2;

            reader.triangles[i * 6 + 3] = i * 2 + 3;
            reader.triangles[i * 6 + 4] = i * 2 + 1;
            reader.triangles[i * 6 + 5] = i * 2;*/
            /*testedTris.Add(i);
            testedTris.Add(i+1);
            testedTris.Add(i+2);

            testedTris.Add(i);
            testedTris.Add(i+2);
            testedTris.Add(i+3);*/
            /*reader.triangles[i] = i;
            reader.triangles[i] = i + 1;
            reader.triangles[i] = i + 2;

            reader.triangles[i] = i;
            reader.triangles[i] = i + 2;
            reader.triangles[i] = i + 3;*/
        }
        mesh.triangles = reader.triangles;
        actualTris = mesh.triangles;

        //mesh.normals = meshToClone.normals;
        Debug.Log(mesh.subMeshCount);
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
    }
}
