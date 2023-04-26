using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheDroneMaster.CustomLore.SpecificScripts.Small3DObject;

namespace TheDroneMaster.CustomLore.SpecificScripts
{
    static class MeshLoader
    {
        static public Mesh3D LoadMeshFromPath(string path)
        {
            Mesh3D mesh = new Mesh3D();
            if (!File.Exists(path))
                return mesh;

            string[] lines = File.ReadAllLines(path);
            foreach(var line in lines) 
            {
                string[] chars = line.Split(' ');
                switch(chars[0])
                {
                    case "v":
                        mesh.origVertices.Add(new Vector3(float.Parse(chars[1]), float.Parse(chars[2]), float.Parse(chars[3])));
                        break;
                    case "vt":
                        //mesh.uvs.Add(new Vector2(float.Parse(chars[1]), float.Parse(chars[2])));
                        break;
                    case "vn":
                        //mesh.normals.Add(new Vector3(float.Parse(chars[1]), float.Parse(chars[2]), float.Parse(chars[3])));
                        break;
                    case "f":
                        int[] vec = new int[3];
                        for (int i = 1; i < 3; i++)//三角面
                        {
                            string[] face = chars[i].Split('/');
                            vec[i] = int.Parse(face[0]) - 1;

                            //int.Parse(face[1]) - 1]; 法线
                            //int.Parse(face[2]) - 1]; uv
                        }
                        mesh.facets.Add(new Mesh3D.TriangleFacet(vec));
                        break;
                }
            }
            return mesh;
        }
    }
}
