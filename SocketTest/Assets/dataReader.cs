using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class dataReader : MonoBehaviour {

    public string trianglesFile = "triangless.txt";
    public string verticesFile = "verticess.txt";
    public string uvsFile = "uvs.txt";

    public int[] triangles;
    public Vector3[] vertices;
    public Vector2[] uvs;

    public int getLines(StreamReader reader)
    {
        int count = 0;
        while (reader.ReadLine() != null)
        {
            count++;
        }
        return count;
    }

    public void readTriData() {
        using (var reader = new StreamReader(trianglesFile))
        {
            int lineCount = getLines(new StreamReader(trianglesFile));
            triangles = new int[lineCount];
            int count = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                triangles[count] = int.Parse(line.ToString());
                count++;
            }
        }
    }

    public void readVertData()
    {
        using (var reader = new StreamReader(verticesFile))
        {
            int lineCount = getLines(new StreamReader(verticesFile));
            vertices = new Vector3[lineCount];
            int count = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                string[] verts = line.Split(',');
                vertices[count].x = float.Parse(verts[2]);
                vertices[count].y = float.Parse(verts[0]);
                vertices[count].z = float.Parse(verts[1]);
                count++;
            }
        }
    }

    public void readUVData()
    {
        using (var reader = new StreamReader(uvsFile))
        {
            int lineCount = getLines(new StreamReader(uvsFile));
            uvs = new Vector2[lineCount];
            int count = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                string[] verts = line.Split(',');
                uvs[count].x = float.Parse(verts[0]);
                uvs[count].y = float.Parse(verts[1]);
                count++;
            }
        }
    }

    private void Start()
    {
        readTriData();
        readVertData();
        readUVData();
    }
}
