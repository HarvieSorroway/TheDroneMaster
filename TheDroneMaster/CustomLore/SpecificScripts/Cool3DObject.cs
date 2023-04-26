using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster
{
    public class Cool3DObject : CosmeticSprite
    {
        public static Cool3DObject instance;

        Mesh3D mesh = new Mesh3D();
        Mesh3DRenderer mesh3DRenderer;
        Vector2 startPos;
        public Cool3DObject(Room room, Vector2 startPos)
        {
            instance = this;

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

                new Mesh3D.TriangleFacet(16,17,18),
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

            mesh.SetVertice(16, Vector3.up * 5f);
            mesh.SetVertice(17, Vector3.right * 5f);
            mesh.SetVertice(18, Vector3.left * 5f);


            mesh3DRenderer = new Mesh3DFrameRenderer(mesh, 0);
            mesh3DRenderer.SetVerticeColor(Color.green, true);
            mesh3DRenderer.SetVerticeColor(Color.green * 0.3f + Color.black * 0.8f, false);

            for(int i = 0;i < 4;i++)
            {
                mesh3DRenderer.SetVerticeColor(i * 4, Color.blue, true);
                mesh3DRenderer.SetVerticeColor(i * 4, Color.cyan * 0.3f + Color.black * 0.8f, false);
            }
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
            if (newContatiner == null)
            {
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[i]);
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
            mesh3DRenderer.Update();
            //for(int i = 0;i < 4; i++)
            //{
            //    Vector3 spread = Vector3.zero;
            //    switch (i)
            //    {
            //        case 0:
            //            spread = Vector3.left;
            //            break;
            //        case 1:
            //            spread = Vector3.forward; 
            //            break;
            //        case 2:
            //            spread = Vector3.right;
            //            break;
            //        case 3:
            //            spread = -Vector3.forward;
            //            break;
            //    }
            //    mesh.SetVertice(i * 4,  spread * (Mathf.Sin(Time.time) + 1f) * 15f);
            //}

            mesh.Update();
        }

        public class Mesh3D
        {
            public List<Vector3> origVertices = new List<Vector3>();
            public List<Vector3> animatedVertice = new List<Vector3>();
            public List<TriangleFacet> facets = new List<TriangleFacet>();

            //分别围绕x轴，y轴，z轴旋转
            public Vector3 rotation = Vector3.zero;

            public Mesh3D()
            {
            }

            public void SetFacet(TriangleFacet[] facets)
            {
                for (int i = 0; i < facets.Length; i++)
                {
                    int maxVertice = Mathf.Max(facets[i].a, facets[i].b, facets[i].c);

                    while (animatedVertice.Count < maxVertice + 1)
                    {
                        animatedVertice.Add(Vector3.zero);
                        origVertices.Add(Vector3.zero);
                    }

                    this.facets.Add(facets[i]);
                }
                Plugin.Log("Total Vertices : " + origVertices.Count.ToString());
            }

            public void SetVertice(int index, Vector3 pos)
            {
                animatedVertice[index] = pos;
                origVertices[index] = pos;
            }

            public void Update()
            {

            }

            public struct TriangleFacet
            {
                public int a;
                public int b;
                public int c;

                public TriangleFacet(int a, int b, int c)
                {
                    this.a = a; this.b = b; this.c = c;
                }
            }
        }

        public class Mesh3DRenderer
        {
            public Mesh3D mesh;

            public int startIndex;
            public int totalSprites;

            public Vector3[] vertices;

            public Color[] verticeColorInFront;
            public Color[] verticeColorInBack;

            protected float maxZ;
            protected float minZ;

            public Mesh3DRenderer(Mesh3D mesh, int startIndex)
            {
                this.mesh = mesh;
                this.startIndex = startIndex;

                vertices = new Vector3[mesh.animatedVertice.Count];

                verticeColorInBack = new Color[mesh.animatedVertice.Count];
                verticeColorInFront = new Color[mesh.animatedVertice.Count];

                SetUpRenderInfo();
            }

            public void SetVerticeColor(Color color,bool inFront)
            {
                for(int i = 0;i < verticeColorInFront.Length; i++)
                {
                    SetVerticeColor(i, color, inFront);
                }
            }

            public void SetVerticeColor(int index,Color color,bool inFront)
            {
                if (inFront)
                    verticeColorInFront[index] = color;
                else
                    verticeColorInBack[index] = color;
            }

            public virtual void SetUpRenderInfo()
            {
            }

            public virtual void Update()
            {
                minZ = float.MaxValue;
                maxZ = float.MinValue;

                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 v = mesh.animatedVertice[i];

                    v = RotateRound(v, Vector3.forward, mesh.rotation.z, Vector3.zero);
                    v = RotateRound(v, Vector3.up, mesh.rotation.y, Vector3.zero);
                    v = RotateRound(v, Vector3.right, mesh.rotation.x, Vector3.zero);

                    vertices[i] = v;

                    if(v.z < minZ)
                        minZ = v.z;
                    if (v.z > maxZ)
                        maxZ = v.z;
                }
            }

            public virtual void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
            }

            public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 centerPos)
            {
            }

            public Vector3 RotateRound(Vector3 position, Vector3 axis, float angle, Vector3 center)
            {
                return Quaternion.AngleAxis(angle, axis) * (position - center) + center;
            }

            public Vector2 GetVerticeIn2D(int index)
            {
                return new Vector2(vertices[index].x, vertices[index].y);
            }

            public Color GetLerpedColor(int index)
            {
                return Color.Lerp(verticeColorInBack[index], verticeColorInFront[index], Mathf.InverseLerp(minZ, maxZ, vertices[index].z));
            }

            public enum Overlay
            {
                inFront,
                inMid,
                inBack
            }
        }

        public class Mesh3DFrameRenderer : Mesh3DRenderer
        {
            public List<LineRepresent> lineRepresents = new List<LineRepresent>();

            public Mesh3DFrameRenderer(Mesh3D mesh, int startIndex) : base(mesh, startIndex)
            {
            }

            public override void SetUpRenderInfo()
            {
                foreach (var facet in mesh.facets)
                {
                    var lineA = new LineRepresent(facet.a, facet.b);
                    var lineB = new LineRepresent(facet.b, facet.c);
                    var lineC = new LineRepresent(facet.a, facet.c);

                    if (!lineRepresents.Contains(lineA))
                        lineRepresents.Add(lineA);
                    if (!lineRepresents.Contains(lineB))
                        lineRepresents.Add(lineB);
                    if (!lineRepresents.Contains(lineC))
                        lineRepresents.Add(lineC);
                }
                totalSprites = lineRepresents.Count;
            }

            public override void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                for (int i = 0; i < totalSprites; i++)
                {
                    sLeaser.sprites[i + startIndex] = new CustomFSprite("Futile_White");
                }
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 centerPos)
            {
                for (int i = 0; i < lineRepresents.Count; i++)
                {
                    var line = lineRepresents[i];

                    Vector2 posA = GetVerticeIn2D(line.a) + centerPos - camPos;
                    Vector2 posB = GetVerticeIn2D(line.b) + centerPos - camPos;

                    Vector2 perpDir = Custom.PerpendicularVector(Custom.DirVec(posA, posB));


                    Color newColA, newColB;
                    newColA = GetLerpedColor(line.a);
                    newColB = GetLerpedColor(line.b);

                    //计算覆盖关系
                    Overlay newOverlay;
                    if (mesh.animatedVertice[line.a].z > 0 && mesh.animatedVertice[line.b].z > 0)
                        newOverlay = Overlay.inFront;
                    else if (mesh.animatedVertice[line.a].z < 0 && mesh.animatedVertice[line.b].z < 0)
                        newOverlay = Overlay.inBack;
                    else
                        newOverlay = Overlay.inMid;

                    if (newOverlay != line.overlay)
                    {
                        switch (newOverlay)
                        {
                            case Overlay.inFront:
                                (sLeaser.sprites[i] as CustomFSprite).MoveToFront();
                                break;
                            case Overlay.inBack:
                                (sLeaser.sprites[i] as CustomFSprite).MoveToBack();
                                break;
                            case Overlay.inMid:
                                for (int k = startIndex; k < startIndex + totalSprites; k++)
                                {
                                    if (lineRepresents[k - startIndex].overlay == Overlay.inFront)
                                    {
                                        (sLeaser.sprites[i] as CustomFSprite).MoveBehindOtherNode((sLeaser.sprites[k]));
                                        break;
                                    }
                                }
                                break;
                        }
                        line.overlay = newOverlay;
                    }


                    (sLeaser.sprites[i] as CustomFSprite).MoveVertice(0, posA + perpDir * 1f);
                    (sLeaser.sprites[i] as CustomFSprite).MoveVertice(1, posA - perpDir * 1f);
                    (sLeaser.sprites[i] as CustomFSprite).MoveVertice(2, posB - perpDir * 1f);
                    (sLeaser.sprites[i] as CustomFSprite).MoveVertice(3, posB + perpDir * 1f);

                    (sLeaser.sprites[i] as CustomFSprite).verticeColors[0] = newColA;
                    (sLeaser.sprites[i] as CustomFSprite).verticeColors[1] = newColA;
                    (sLeaser.sprites[i] as CustomFSprite).verticeColors[2] = newColB;
                    (sLeaser.sprites[i] as CustomFSprite).verticeColors[3] = newColB;

                }
            }

            public struct LineRepresent
            {
                public int a;
                public int b;
                public Overlay overlay;

                public LineRepresent(int a, int b)
                {
                    if (a < b)
                    {
                        this.a = a; this.b = b;
                    }
                    else
                    {
                        this.a = b; this.b = a;
                    }
                    overlay = Overlay.inFront;
                }

                public override bool Equals(object obj)
                {
                    LineRepresent represent = (LineRepresent)obj;
                    if (represent.a == a && represent.b == b)
                        return true;
                    if (represent.a == b && represent.b == a)
                        return true;
                    return false;
                }

                
            }
        }

        public class Mesh3DDotMatrixRenderer : Mesh3DRenderer
        {
            public Mesh3DDotMatrixRenderer(Mesh3D mesh, int startIndex) : base(mesh, startIndex)
            {
            }

            public override void SetUpRenderInfo()
            {
                totalSprites = mesh.animatedVertice.Count;
            }

            public override void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                for (int i = startIndex; i < startIndex + totalSprites; i++)
                {
                    sLeaser.sprites[i] = new FSprite("pixel", true) { scale = 2f, color = Color.green };
                }
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 centerPos)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector2 pos = GetVerticeIn2D(i);
                    pos += centerPos - camPos;

                    sLeaser.sprites[i + startIndex].SetPosition(pos);
                    sLeaser.sprites[i + startIndex].color = GetLerpedColor(i);
                }
            }
        }
    }
}
