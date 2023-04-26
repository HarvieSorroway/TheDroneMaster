using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
using static TheDroneMaster.Cool3DObject;
using static TheDroneMaster.Cool3DObject.Mesh3D;

namespace TheDroneMaster
{
    public class Cool3DObject : CosmeticSprite
    {
        public static Cool3DObject instance;

        Mesh3D mesh = new Mesh3D();
        Mesh3DRenderer mesh3DRenderer1;
        Mesh3DRenderer mesh3DRenderer2;
        Mesh3DRenderer mesh3DRenderer3;

        Vector2 startPos;
        public Cool3DObject(Room room, Vector2 startPos)
        {
            instance = this;

            this.startPos = startPos;
            Mesh3D.TriangleFacet[] facets = new Mesh3D.TriangleFacet[]
            {
                new Mesh3D.TriangleFacet(0,1,2),
                new Mesh3D.TriangleFacet(1,3,2),

                new Mesh3D.TriangleFacet(4,5,6),
                new Mesh3D.TriangleFacet(5,7,6),

                new Mesh3D.TriangleFacet(8,9,10),
                new Mesh3D.TriangleFacet(9,11,10),

                new Mesh3D.TriangleFacet(12,13,14),
                new Mesh3D.TriangleFacet(13,15,14),

                new Mesh3D.TriangleFacet(16,17,18),
            };
            mesh.SetFacet(facets);

            mesh.SetVertice(0, Vector3.left * 150f + Vector3.up * 100f + Vector3.forward * 100f);
            mesh.SetVertice(1, Vector3.left * 150f + Vector3.up * -100f + Vector3.forward * 100f);
            mesh.SetVertice(2, Vector3.left * 150f + Vector3.up * 100f + Vector3.forward * -100f);
            mesh.SetVertice(3, Vector3.left * 150f + Vector3.up * -100f + Vector3.forward * -100f);

            mesh.SetVertice(4, Vector3.forward * -150f + Vector3.up * 100f + Vector3.left * 100f);
            mesh.SetVertice(5, Vector3.forward * -150f + Vector3.up * -100f + Vector3.left * 100f);
            mesh.SetVertice(6, Vector3.forward * -150f + Vector3.up * 100f + Vector3.left * -100f);
            mesh.SetVertice(7, Vector3.forward * -150f + Vector3.up * -100f + Vector3.left * -100f);

            mesh.SetVertice(8, Vector3.left * -150f + Vector3.up * 100f + Vector3.forward * -100f);
            mesh.SetVertice(9, Vector3.left * -150f + Vector3.up * -100f + Vector3.forward * -100f);
            mesh.SetVertice(10, Vector3.left * -150f + Vector3.up * 100f + Vector3.forward * 100f);
            mesh.SetVertice(11, Vector3.left * -150f + Vector3.up * -100f + Vector3.forward * 100f);

            mesh.SetVertice(12, Vector3.forward * 150f + Vector3.up * 100f + Vector3.left * 100f);
            mesh.SetVertice(13, Vector3.forward * 150f + Vector3.up * -100f + Vector3.left * 100f);
            mesh.SetVertice(14, Vector3.forward * 150f + Vector3.up * 100f + Vector3.left * -100f);
            mesh.SetVertice(15, Vector3.forward * 150f + Vector3.up * -100f + Vector3.left * -100f);

            mesh.SetVertice(16, Vector3.up * 5f);
            mesh.SetVertice(17, Vector3.right * 5f);
            mesh.SetVertice(18, Vector3.left * 5f);


            mesh3DRenderer1 = new Mesh3DFacetRenderer(mesh, 0, "AIimg1",true)
            {
                lightDir = new Vector3(-1f, -1f, -1f),
            };
            mesh3DRenderer1.SetVerticeColor(Color.gray, true);
            mesh3DRenderer1.SetVerticeColor(Color.gray * 0.3f + Color.black * 0.8f, false);
            (mesh3DRenderer1 as Mesh3DFacetRenderer).LoadImage();


            for (int i = 0; i < 8; i++)
            {
                float x = i / 7f;
                
                for(int k = 0; k < 2; k++)
                {
                    float y = k;
                    (mesh3DRenderer1 as Mesh3DFacetRenderer).SetUV(i * 2 + k, new Vector2(x, y));

                    Plugin.Log(string.Format("{0} : {1}",i *2 + k, new Vector2(x, y)));
                }
            }
            for(int i = 16;i < 19; i++)
            {
                (mesh3DRenderer1 as Mesh3DFacetRenderer).SetUV(i, Vector2.zero);
            }

            mesh3DRenderer2 = new Mesh3DDotMatrixRenderer(mesh, mesh3DRenderer1.totalSprites);
            mesh3DRenderer2.SetVerticeColor(Color.yellow, true);
            mesh3DRenderer2.SetVerticeColor(Color.yellow * 0.3f + Color.black * 0.8f, false);

            mesh3DRenderer3 = new Mesh3DFrameRenderer(mesh, mesh3DRenderer1.totalSprites + mesh3DRenderer2.totalSprites);
            mesh3DRenderer3.SetVerticeColor(Color.red, true);
            mesh3DRenderer3.SetVerticeColor(Color.red * 0.3f + Color.black * 0.8f, false);


            //for(int i = 0;i < 4;i++)
            //{
            //    mesh3DRenderer.SetVerticeColor(i * 4, Color.blue, true);
            //    mesh3DRenderer.SetVerticeColor(i * 4, Color.cyan * 0.3f + Color.black * 0.8f, false);
            //}
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[mesh3DRenderer1.totalSprites + mesh3DRenderer2.totalSprites + mesh3DRenderer3.totalSprites];
            mesh3DRenderer1.InitSprites(sLeaser, rCam);
            mesh3DRenderer2.InitSprites(sLeaser, rCam);
            mesh3DRenderer3.InitSprites(sLeaser, rCam);

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
            mesh3DRenderer1.DrawSprites(sLeaser, rCam, timeStacker, camPos, startPos);
            mesh3DRenderer2.DrawSprites(sLeaser, rCam, timeStacker, camPos, startPos);
            mesh3DRenderer3.DrawSprites(sLeaser, rCam, timeStacker, camPos, startPos);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            mesh.rotation = new Vector3(/*180f * (Mathf.Sin(Time.time * 0.5f) + 1f)*/0f, 180f * (Mathf.Cos(Time.time * 0.5f)) + 1f, /*mesh.rotation.z + 1f*/0f);
            mesh3DRenderer1.Update();
            mesh3DRenderer2.Update();
            mesh3DRenderer3.Update();
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
            public Vector2[] uvs;

            public Color[] verticeColorInFront;
            public Color[] verticeColorInBack;

            protected float maxZ;
            protected float minZ;

            public Mesh3DRenderer(Mesh3D mesh, int startIndex)
            {
                this.mesh = mesh;
                this.startIndex = startIndex;

                vertices = new Vector3[mesh.animatedVertice.Count];
                uvs = new Vector2[mesh.animatedVertice.Count];

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

            public void SetUV(int index,Vector2 uv)
            {
                uvs[index] = uv;
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
                    v = RotateRound(v, Vector3.right, mesh.rotation.x, Vector3.zero);
                    v = RotateRound(v, Vector3.up, mesh.rotation.y, Vector3.zero);

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
                                (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveToFront();
                                break;
                            case Overlay.inBack:
                                (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveToBack();
                                break;
                            case Overlay.inMid:
                                for (int k = startIndex; k < startIndex + totalSprites; k++)
                                {
                                    if (lineRepresents[k - startIndex].overlay == Overlay.inFront)
                                    {
                                        (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveBehindOtherNode((sLeaser.sprites[k]));
                                        break;
                                    }
                                }
                                break;
                        }
                        line.overlay = newOverlay;
                    }


                    (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveVertice(0, posA + perpDir * 1f);
                    (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveVertice(1, posA - perpDir * 1f);
                    (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveVertice(2, posB - perpDir * 1f);
                    (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveVertice(3, posB + perpDir * 1f);

                    (sLeaser.sprites[i + startIndex] as CustomFSprite).verticeColors[0] = newColA;
                    (sLeaser.sprites[i + startIndex] as CustomFSprite).verticeColors[1] = newColA;
                    (sLeaser.sprites[i + startIndex] as CustomFSprite).verticeColors[2] = newColB;
                    (sLeaser.sprites[i + startIndex] as CustomFSprite).verticeColors[3] = newColB;

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
                    sLeaser.sprites[i] = new FSprite("pixel", true) { scale = 5f, color = Color.green };
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

        public class Mesh3DFacetRenderer : Mesh3DRenderer
        {
            bool usingLight = false;

            string _element;
            bool shouldUpdateElement;
            public string Element
            {
                get => _element;
                set 
                {
                    shouldUpdateElement = _element != value;
                    _element = value;
                }
            }

            public List<FacetRepresent> facetRepresents;

            public Vector3 lightDir;
            public Mesh3DFacetRenderer(Mesh3D mesh, int startIndex,string element, bool usingLight = false) : base(mesh, startIndex)
            {
                _element = element;
                this.usingLight = usingLight;
            }

            public void LoadImage()
            {
                if (Futile.atlasManager.GetAtlasWithName(_element) == null)
                {
                    string str = AssetManager.ResolveFilePath("Illustrations" + Path.DirectorySeparatorChar.ToString() + _element + ".png");
                    Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + str, false, true);
                    Futile.atlasManager.LoadAtlasFromTexture(_element, texture, false);
                }
            }

            public override void SetUpRenderInfo()
            {
                totalSprites = mesh.facets.Count;
                facetRepresents = new FacetRepresent[totalSprites].ToList();
                for(int i = 0;i < facetRepresents.Count; i++)
                {
                    facetRepresents[i] = new FacetRepresent(mesh.facets[i]);
                }
            }

            public override void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                for(int i = 0; i < totalSprites; i++)
                {
                    facetRepresents[i].linkedSpriteIndex = i + startIndex;
                    sLeaser.sprites[i + startIndex] = new TriangleMesh(Element, new TriangleMesh.Triangle[1] { new TriangleMesh.Triangle(0, 1, 2)}, true, true);
                    
                    var facet = mesh.facets[i];
                    (sLeaser.sprites[i + startIndex] as TriangleMesh).UVvertices[0] = uvs[facet.a];
                    (sLeaser.sprites[i + startIndex] as TriangleMesh).UVvertices[1] = uvs[facet.b];
                    (sLeaser.sprites[i + startIndex] as TriangleMesh).UVvertices[2] = uvs[facet.c];
                } 
            }

            public override void Update()
            {
                base.Update();
                for(int i = 0;i < facetRepresents.Count; i++)
                {
                    facetRepresents[i].CaculateNormalAndSort(vertices);
                }
                facetRepresents.Sort((x, y) => x.sortZ.CompareTo(y.sortZ));
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 centerPos)
            {
                for(int i = 0;i <facetRepresents.Count - 1; i++)
                {
                    sLeaser.sprites[facetRepresents[i + 1].linkedSpriteIndex].MoveInFrontOfOtherNode(sLeaser.sprites[facetRepresents[i].linkedSpriteIndex]);
                }

                for(int i = 0; i < facetRepresents.Count; i++)
                {
                    var represent = facetRepresents[i];
                    bool culled = represent.normal.z < 0;
                    int index = represent.linkedSpriteIndex;

                    sLeaser.sprites[index].isVisible = true;
                    (sLeaser.sprites[index] as TriangleMesh).MoveVertice(0, GetVerticeIn2D(represent.a) + centerPos - camPos);
                    (sLeaser.sprites[index] as TriangleMesh).MoveVertice(1, GetVerticeIn2D(represent.b) + centerPos - camPos);
                    (sLeaser.sprites[index] as TriangleMesh).MoveVertice(2, GetVerticeIn2D(represent.c) + centerPos - camPos);

                    float light = Vector3.Dot(represent.normal, -lightDir);
                    if (culled || !usingLight)
                        light = 0f;

                    Color colA = Color.Lerp(GetLerpedColor(represent.a), Color.white, light);
                    Color colB = Color.Lerp(GetLerpedColor(represent.b), Color.white, light);
                    Color colC = Color.Lerp(GetLerpedColor(represent.c), Color.white, light);

                    (sLeaser.sprites[index] as TriangleMesh).verticeColors[0] = colA;
                    (sLeaser.sprites[index] as TriangleMesh).verticeColors[1] = colB;
                    (sLeaser.sprites[index] as TriangleMesh).verticeColors[2] = colC;
                }
            }

            public class FacetRepresent
            {
                public int a; public int b; public int c;
                public int linkedSpriteIndex;

                public float sortZ;
                public Vector3 normal;
                public FacetRepresent(int a, int b, int c)
                {
                    this.a = a; this.b = b; this.c = c;
                    sortZ = 0f;
                    normal = Vector3.zero;
                }

                public FacetRepresent(TriangleFacet copyFrom) : this(copyFrom.a,copyFrom.b, copyFrom.c)
                {
                }

                /// <summary>
                /// 法线方向默认从原点指向外部
                /// </summary>
                /// <param name="vertices"></param>
                public void CaculateNormalAndSort(Vector3[] vertices)
                {
                    Vector3 A = vertices[b] - vertices[a];
                    Vector3 B = vertices[c] - vertices[a];

                    normal = Vector3.Cross(A, B).normalized;

                    if (Vector3.Dot(vertices[a], normal) < 0f)
                        normal = -normal;

                    sortZ = (vertices[a].z + vertices[b].z + vertices[c].z) / 3f;
                }
            }
        }
    }
}
