using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Triangle {
  
    //Corners
    public Vertex v1;
    public Vertex v2;
    public Vertex v3;

    //If we are using the half edge mesh structure, we just need one half edge
    //public HalfEdge halfEdge;

    public Triangle(Vertex v1, Vertex v2, Vertex v3)
    {
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;
    }

    public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        this.v1 = new Vertex(v1);
        this.v2 = new Vertex(v2);
        this.v3 = new Vertex(v3);
    }

    /*public Triangle(HalfEdge halfEdge)
    {
        this.halfEdge = halfEdge;
    }*/

    //Change orientation of triangle from cw -> ccw or ccw -> cw
    public void ChangeOrientation()
    {
        Vertex temp = this.v1;

        this.v1 = this.v2;

        this.v2 = temp;
    }
}