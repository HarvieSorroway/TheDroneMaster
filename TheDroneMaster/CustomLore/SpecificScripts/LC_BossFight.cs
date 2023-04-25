using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheDroneMaster.CustomLore.SpecificScripts.Small3DObject;

namespace TheDroneMaster.CustomLore.SpecificScripts
{
    public class LC_BossFight : UpdatableAndDeletable
    {
        public LC_BossFight(Room room)
        { 
            base.room = room;
        }
    }

    public class Small3DObject : CosmeticSprite
    {
        public static Small3DObject instance;

        Mesh3D mesh = new Mesh3D();
        Mesh3DRenderer mesh3DRenderer;
        Vector2 startPos;
        public Small3DObject(Room room,Vector2 startPos)
        {
            instance= this;

            this.startPos = startPos;
            Mesh3D.TriangleFacet[] facets = new Mesh3D.TriangleFacet[]
            {
                new Mesh3D.TriangleFacet(0,1,2),
                new Mesh3D.TriangleFacet(0,2,3),
                new Mesh3D.TriangleFacet(0,1,3),

                new Mesh3D.TriangleFacet(1,2,3),
            };
            mesh.SetFacet(facets);

            mesh.SetVertice(0, Vector3.up * 10f);
            mesh.SetVertice(1, Vector3.down * 5f + Vector3.right * 15f);
            mesh.SetVertice(2, Vector3.down * 5f + Vector3.forward * 10f + Vector3.left * 5f);
            mesh.SetVertice(3, Vector3.down * 5f + Vector3.forward * -10f + Vector3.left * 5f);

            mesh3DRenderer = new Mesh3DRenderer(mesh, 0, Mesh3DRenderer.RenderMode.Wireframe);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[mesh3DRenderer.totalSprites];
            mesh3DRenderer.InitSprites(sLeaser, rCam);

            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if(newContatiner == null)
            {
                for(int i = 0;i < sLeaser.sprites.Length; i++)
                {
                    rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[i]);
                }
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            mesh3DRenderer.DrawSprites(sLeaser, rCam, timeStacker, camPos, startPos);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            mesh.rotation = new Vector3(180f * (Mathf.Sin(Time.time) + 1f), 180f * (Mathf.Cos(Time.time)) + 1f, mesh.rotation.z + 2f);

            mesh.Update();
        }

        public class Mesh3D
        {
            public List<Vector3> origVertices = new List<Vector3>();
            public List<Vector3> vertices = new List<Vector3>();
            public List<TriangleFacet> facets = new List<TriangleFacet>();

            //分别围绕x轴，y轴，z轴旋转
            public Vector3 rotation = Vector3.zero;
            public Mesh3D()
            {
            }

            public void SetFacet(TriangleFacet[] facets)
            {
                for(int i = 0;i < facets.Length; i++)
                {
                    int maxVertice = Mathf.Max(facets[i].a, facets[i].b, facets[i].c);

                    for(int k = 0;k < maxVertice - vertices.Count + 1;k++)
                    {
                        vertices.Add(Vector3.zero);
                        origVertices.Add(Vector3.zero);
                    }
                    this.facets.Add(facets[i]);
                }
            }

            public void SetVertice(int index,Vector3 pos)
            {
                vertices[index] = pos;
                origVertices[index] = pos;
            }

            public void Update()
            {
                for(int i = 0;i < vertices.Count;i++)
                {
                    Vector3 v = origVertices[i];
                    v = RotateRound(v, Vector3.up, rotation.y);
                    v = RotateRound(v, Vector3.forward, rotation.z);
                    v = RotateRound(v, Vector3.right ,rotation.x);

                    vertices[i] = v;
                }
            }

            public Vector2 GetVerticeIn2D(int index)
            {
                return new Vector2(vertices[index].x, vertices[index].y);
            }


            public Vector3 RotateRound(Vector3 position, Vector3 axis, float angle)
            {
                return Quaternion.AngleAxis(angle, axis) * position;
            }

            public struct TriangleFacet
            {
                public int a;
                public int b;
                public int c;

                public TriangleFacet(int a,int b,int c)
                {
                    this.a = a; this.b = b; this.c = c;
                }
            }
        }

        public class Mesh3DRenderer
        {
            public Mesh3D mesh;
            public int startIndex;

            public Color colorInFront;
            public Color colorInBack;

            public RenderMode renderMode;

            public List<LineRepresent> lineRepresents = new List<LineRepresent>();

            public int totalSprites;
            public Mesh3DRenderer(Mesh3D mesh,int startIndex, RenderMode renderMode) 
            {
                this.mesh = mesh;
                this.startIndex = startIndex;
                this.renderMode = renderMode;

                colorInFront = Color.green;
                colorInBack = Color.green * 0.5f + Color.black * 0.5f;

                SetUpRenderInfo();
            }

            public void SetUpRenderInfo()
            {
                Plugin.Log("SetUpRenderInfo : " + renderMode.ToString());
                switch (renderMode)
                {
                    case RenderMode.DotMatrix:
                        totalSprites = mesh.vertices.Count;
                        break;
                    case RenderMode.Wireframe:
                        foreach(var facet in mesh.facets)
                        {
                            var lineA = new LineRepresent(facet.a, facet.b);
                            var lineB = new LineRepresent(facet.b, facet.c);
                            var lineC = new LineRepresent(facet.a, facet.c);

                            if (!lineRepresents.Contains(lineA)) 
                                lineRepresents.Add(lineA);
                            if(!lineRepresents.Contains(lineB))
                                lineRepresents.Add(lineB);
                            if(!lineRepresents.Contains(lineC))
                                lineRepresents.Add(lineC);
                        }
                        totalSprites = lineRepresents.Count;
                        break;
                }
            }

            public void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                switch(renderMode)
                {
                    case RenderMode.DotMatrix:
                        for(int i = startIndex; i <startIndex + totalSprites; i++)
                        {
                            sLeaser.sprites[i] = new FSprite("pixel", true) { scale = 2f,color = Color.green };
                        }
                        break;
                    case RenderMode.Wireframe:
                        for(int i = 0;i < totalSprites; i++)
                        {
                            sLeaser.sprites[i + startIndex] = new CustomFSprite("Futile_White");
                        }
                        break;
                }
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos,Vector2 centerPos)
            {
                switch (renderMode)
                {
                    case RenderMode.DotMatrix:
                        for (int i = 0;i < mesh.vertices.Count; i++)
                        {
                            Vector2 pos = mesh.GetVerticeIn2D(i);
                            pos += centerPos - camPos;
                            Color newCol;

                            if (mesh.vertices[i].z > 0)
                                newCol = colorInFront;
                            else
                                newCol = colorInBack;

                            sLeaser.sprites[i + startIndex].SetPosition(pos);
                            sLeaser.sprites[i + startIndex].color = newCol;
                        }
                        break;
                    case RenderMode.Wireframe:
                        for(int i = 0;i < lineRepresents.Count; i++)
                        {
                            var line = lineRepresents[i];

                            Vector2 posA = mesh.GetVerticeIn2D(line.a) + centerPos - camPos;
                            Vector2 posB = mesh.GetVerticeIn2D(line.b) + centerPos - camPos;

                            Vector2 perpDir = Custom.PerpendicularVector(Custom.DirVec(posA, posB));

                            
                            Color newColA, newColB;
                            if (mesh.vertices[line.a].z > 0)
                                newColA = colorInFront;
                            else
                                newColA = colorInBack;

                            if (mesh.vertices[line.b].z > 0)
                                newColB = colorInFront;
                            else
                                newColB = colorInBack;

                            (sLeaser.sprites[i] as CustomFSprite).MoveVertice(0, posA + perpDir * 1f);
                            (sLeaser.sprites[i] as CustomFSprite).MoveVertice(1, posA - perpDir * 1f);
                            (sLeaser.sprites[i] as CustomFSprite).MoveVertice(2, posB - perpDir * 1f);
                            (sLeaser.sprites[i] as CustomFSprite).MoveVertice(3, posB + perpDir * 1f);

                            (sLeaser.sprites[i] as CustomFSprite).verticeColors[0] = newColA;
                            (sLeaser.sprites[i] as CustomFSprite).verticeColors[1] = newColA;
                            (sLeaser.sprites[i] as CustomFSprite).verticeColors[2] = newColB;
                            (sLeaser.sprites[i] as CustomFSprite).verticeColors[3] = newColB;
                        }

                        break;
                }
            }

            public enum RenderMode
            {
                DotMatrix,
                Wireframe,
                Facet
            }

            public struct LineRepresent
            {
                public int a;
                public int b;
                public LineRepresent(int a, int b)
                {
                    if(a < b)
                    {
                        this.a = a; this.b = b;
                    }
                    else
                    {
                        this.a = b; this.b = a;
                    }
                }

                public override bool Equals(object obj)
                {
                    LineRepresent represent = (LineRepresent)obj;
                    if (represent.a == this.a && represent.b == this.b)
                        return true;
                    if (represent.a == this.b && represent.b == this.a)
                        return true;
                    return false;
                }
            }
        }
    }
}
