using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ElementMesh {

    public string ID, Name;
    public Vector3[] vertices;
    public int[] triangles;
    public meshCreation meshCreator;
    public int index;

}
