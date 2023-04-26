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
        List<Mesh3DRenderer> mesh3DRenderers;
        Vector2 startPos;
        int totalSprites =0;
        public Small3DObject(Room room,Vector2 startPos)
        {
            instance= this;

            this.startPos = startPos;
            Mesh3D.TriangleFacet[] facets = new Mesh3D.TriangleFacet[]
            {
                new Mesh3D.TriangleFacet(0,1,2),
                new Mesh3D.TriangleFacet(1,2,3),

                new Mesh3D.TriangleFacet(4,5,6),
                new Mesh3D.TriangleFacet(5,6,7),

                new Mesh3D.TriangleFacet(8,9,10),
                new Mesh3D.TriangleFacet(9,10,11),

                new Mesh3D.TriangleFacet(12,13,14),
                new Mesh3D.TriangleFacet(13,14,15),
            };
            mesh.SetFacet(facets);

            mesh.SetVertice(0, Vector3.left * 15f + Vector3.up * 5f + Vector3.forward * 10f);
            mesh.SetVertice(1, Vector3.left * 15f + Vector3.up * -5f + Vector3.forward * 10f);
            mesh.SetVertice(2, Vector3.left * 15f + Vector3.up * 5f + Vector3.forward * -10f);
            mesh.SetVertice(3, Vector3.left * 15f + Vector3.up * -5f + Vector3.forward * -10f);

            mesh.SetVertice(4, Vector3.forward * 15f + Vector3.up * 5f + Vector3.left * 10f);
            mesh.SetVertice(5, Vector3.forward * 15f + Vector3.up * -5f + Vector3.left * 10f);
            mesh.SetVertice(6, Vector3.forward * 15f + Vector3.up * 5f + Vector3.left * -10f);
            mesh.SetVertice(7, Vector3.forward * 15f + Vector3.up * -5f + Vector3.left * -10f);

            mesh.SetVertice(8, Vector3.left * -15f + Vector3.up * 5f + Vector3.forward * 10f);
            mesh.SetVertice(9, Vector3.left * -15f + Vector3.up * -5f + Vector3.forward * 10f);
            mesh.SetVertice(10, Vector3.left * -15f + Vector3.up * 5f + Vector3.forward * -10f);
            mesh.SetVertice(11, Vector3.left * -15f + Vector3.up * -5f + Vector3.forward * -10f);

            mesh.SetVertice(12, Vector3.forward * -15f + Vector3.up * 5f + Vector3.left * 10f);
            mesh.SetVertice(13, Vector3.forward * -15f + Vector3.up * -5f + Vector3.left * 10f);
            mesh.SetVertice(14, Vector3.forward * -15f + Vector3.up * 5f + Vector3.left * -10f);
            mesh.SetVertice(15, Vector3.forward * -15f + Vector3.up * -5f + Vector3.left * -10f);

            mesh3DRenderers = mesh.CreateMesh3DRenderers(ref totalSprites, Mesh3DRenderer.RenderMode.Wireframe);
            //mesh3DRenderer = new Mesh3DRenderer(mesh, 0, Mesh3DRenderer.RenderMode.Wireframe);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[totalSprites];
            
            mesh3DRenderers.ForEach(i => i.InitSprites(sLeaser, rCam));

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
            mesh3DRenderers.ForEach(i => i.DrawSprites(sLeaser, rCam, timeStacker, camPos, startPos));
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            mesh.Update();
        }

        public class Mesh3D
        {
            public List<Vector3> origVertices = new List<Vector3>();
            public List<Vector3> vertices = new List<Vector3>();
            public List<TriangleFacet> facets = new List<TriangleFacet>();

            public ActorComponent rootComponent;
            public List<Component> components = new List<Component>();

            public Mesh3DRenderer renderer; 


            public Mesh3D()
            {
                rootComponent = new ActorComponent(this);
                components.Add(rootComponent);
                components.Add(new RotatorComponent(rootComponent, Vector3.up*0.1f));

            }


            //创建全部render
            public List<Mesh3DRenderer> CreateMesh3DRenderers(ref int startIndex, Mesh3DRenderer.RenderMode mode)
            {
                List<Mesh3DRenderer> list = new List<Mesh3DRenderer>();
                list.Add(new Mesh3DRenderer(this, ref startIndex, mode));
                foreach(var com in components.FindAll(i=>i is ActorComponent))
                     list.AddRange((com as ActorComponent).ownObject.CreateMesh3DRenderers(ref startIndex, mode));
                return list;
            }

            public void SetFacet(TriangleFacet[] facets)
            {
                for(int i = 0;i < facets.Length; i++)
                {
                    int maxVertice = Mathf.Max(facets[i].a, facets[i].b, facets[i].c);

                    while(vertices.Count < maxVertice + 1)
                    {
                        vertices.Add(Vector3.zero);
                        origVertices.Add(Vector3.zero);
                    }

                    this.facets.Add(facets[i]);
                }
                Plugin.Log("Total Vertices : " + origVertices.Count.ToString());
            }

            public void SetVertice(int index,Vector3 pos)
            {
                vertices[index] = pos;
                origVertices[index] = pos;
            }

            public void Update()
            {
                components.ForEach(c => c.Update());
                PendingUpdate();
            }

            void PendingUpdate()
            {
                for (int i = 0; i < vertices.Count; i++)
                {
                    Vector3 v = origVertices[i];

                    Vector3.Scale(v, rootComponent.GetWorldScale());

                    v = RotateRound(v, Vector3.up, rootComponent.GetWorldRotation().x);
                    v = RotateRound(v, Vector3.forward, rootComponent.GetWorldRotation().y);
                    v = RotateRound(v, Vector3.right, rootComponent.GetWorldRotation().z);

                    v += rootComponent.GetWorldPosition();

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

                public TriangleFacet(int[] s)
                {
                    a = s[0];
                    b = s[1];
                    c = s[2];
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
            public Mesh3DRenderer(Mesh3D mesh,ref int startIndex, RenderMode renderMode) 
            {
                this.mesh = mesh;
                this.startIndex = startIndex;
                this.renderMode = renderMode;

                colorInFront = Color.green;
                colorInBack = Color.green * 0.5f + Color.black * 0.5f;

                SetUpRenderInfo();
                startIndex += totalSprites;
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
