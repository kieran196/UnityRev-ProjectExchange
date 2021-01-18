using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevSocketing
{
    public class meshSender {

        public static Class1 classInstance;

        public string getVertices(IList<XYZ> vertices)
        {
            string verts = "";
            foreach (XYZ vertice in vertices) {
                //Debug.WriteLine(vertice);
                float x = (float)Math.Round(vertice.X, 2);
                float y = (float)Math.Round(vertice.Y, 2);
                float z = (float)Math.Round(vertice.Z, 2);
                //StringBuilder sb = new StringBuilder(vertice.ToString()).Replace("(", "").Replace(")", "");
                verts += x + "," + y + "," + z + "#";
                //verts = sb.ToString().Trim() + "#";
            }
            return verts;
        }

        public void getUVs(Face face, IList<XYZ> vertices)
        {
            foreach (XYZ vertice in vertices) {
                UV pt = face.Project(vertice).UVPoint;
                Debug.WriteLine(pt);
            }
        }

        public string getTris(Face face, Mesh mesh) {
            string triangleStr = "";
            for (int i = 0; i < mesh.NumTriangles; i++){
                MeshTriangle tri = mesh.get_Triangle(i);
                uint tri1 = tri.get_Index(0);
                uint tri2 = tri.get_Index(1);
                uint tri3 = tri.get_Index(2);
                triangleStr += " " +tri1 + " " + tri2 + " " + tri3;
                //Debug.WriteLine("-Triangles-");
                //Debug.WriteLine(tri1);
                //Debug.WriteLine(tri2);
                //Debug.WriteLine(tri3);
                /*XYZ vert1 = tri.get_Vertex(0);
                XYZ vert2 = tri.get_Vertex(1);
                XYZ vert3 = tri.get_Vertex(2);
                UV UVpt1 = face.Project(vert1).UVPoint;
                UV UVpt2 = face.Project(vert2).UVPoint;
                UV UVpt3 = face.Project(vert3).UVPoint;
                Debug.WriteLine(vert1);
                Debug.WriteLine(vert2);
                Debug.WriteLine(vert3);*/
            }
            return triangleStr;
        }

        public void sendMeshData(Element e) {
            Options geometryOptions = new Options();
            GeometryElement geoEle = e.get_Geometry(geometryOptions);
            string tris = "";
            string vertices = "";
            foreach (GeometryObject geoObject in geoEle)
            {
                GeometryInstance geoInstance = geoObject as GeometryInstance;
                if (geoInstance != null)
                {
                    GeometryElement instanceGeometryElement = geoInstance.GetSymbolGeometry();
                    foreach (GeometryObject instObj in instanceGeometryElement)
                    {
                        Solid solid = instObj as Solid;
                        if (solid == null) return;
                        foreach (Face face in solid.Faces)
                        {
                            Mesh mesh = face.Triangulate();
                            //getVertices(mesh.Vertices);
                            //getUVs(face, mesh.Vertices);
                            tris += getTris(face, mesh);
                            vertices += getVertices(mesh.Vertices);
                        }
                    }
                } else {
                    Debug.WriteLine("Element:" + e.Name + " does not have a GeometryInstance");
                    Solid solid = geoObject as Solid;
                    if (solid != null) {
                        foreach (Face face in solid.Faces) {
                            Mesh mesh = face.Triangulate();
                            //getVertices(mesh.Vertices);
                            //getUVs(face, mesh.Vertices);
                            tris += getTris(face, mesh);
                            vertices += getVertices(mesh.Vertices);
                        }
                    } else {
                        Debug.WriteLine("Solid = null");
                    }
                }
            }
            classInstance.sendMeshData(e.Id.ToString(), tris, "tris");
            System.Threading.Thread.Sleep(100);
            classInstance.sendMeshData(e.Id.ToString(), vertices, "verts");
            System.Threading.Thread.Sleep(100);
        }

    }
}
