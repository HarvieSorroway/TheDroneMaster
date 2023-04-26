using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.CustomLore.SpecificScripts;
using UnityEngine;
using static Expedition.ExpeditionProgression;
using static System.Net.Mime.MediaTypeNames;
using static TheDroneMaster.Cool3DObject;

namespace TheDroneMaster
{
    public class Cool3DObject : CosmeticSprite
    {
        public static Cool3DObject instance;

        Mesh3D mesh = new Mesh3D();
        List<Mesh3DRenderer> mesh3DRenderers = new List<Mesh3DRenderer>();
        Vector2 startPos;

        int totalSprites = 0;

        public Cool3DObject(Room room, Vector2 startPos)
        {
            instance = this;

            this.startPos = startPos;
            Mesh3DAsset.TriangleFacet[] facets = new Mesh3DAsset.TriangleFacet[]
            {
                new Mesh3DAsset.TriangleFacet(0,1,2),
                new Mesh3DAsset.TriangleFacet(1,3,2),

                new Mesh3DAsset.TriangleFacet(4,5,6),
                new Mesh3DAsset.TriangleFacet(5,7,6),

                new Mesh3DAsset.TriangleFacet(8,9,10),
                new Mesh3DAsset.TriangleFacet(9,11,10),

                new Mesh3DAsset.TriangleFacet(12,13,14),
                new Mesh3DAsset.TriangleFacet(13,15,14),

                new Mesh3DAsset.TriangleFacet(16,17,18),
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


            mesh3DRenderers = mesh.CreateMesh3DRenderers<Mesh3DFrameRenderer>(ref totalSprites);

            foreach (var mesh3DRenderer in mesh3DRenderers)
            {
                mesh3DRenderer.SetVerticeColor(Color.green, true);
                mesh3DRenderer.SetVerticeColor(Color.green * 0.3f + Color.black * 0.8f, false);

                for (int i = 0; i < 4; i++)
                {
                    mesh3DRenderer.SetVerticeColor(i * 4, Color.blue, true);
                    mesh3DRenderer.SetVerticeColor(i * 4, Color.cyan * 0.3f + Color.black * 0.8f, false);
                }
            }
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
            mesh3DRenderers.ForEach(i => i.DrawSprites(sLeaser, rCam, timeStacker, camPos, startPos));
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            mesh3DRenderers.ForEach(i => i.Update());


            mesh.Update();
        }

        public class Mesh3D
        {
            public List<Vector3> animatedVertices = new List<Vector3>();
            public List<Vector3> vertices = new List<Vector3>();
            public List<Mesh3DAsset.TriangleFacet> facets = new List<Mesh3DAsset.TriangleFacet>();



            public Mesh3DAsset data;


            public Mesh3D()
            {
                data = new Mesh3DAsset();
                rootComponent = new ActorComponent(this);
                components.Add(rootComponent);
                components.Add(new RotatorComponent(rootComponent, Vector3.up * 0.1f));

            }

            public Mesh3D(Mesh3DAsset data)
            {
                this.data = data;
                vertices = animatedVertices = data.vertices;
            }

            public ActorComponent rootComponent;
            public List<RWComponent> components = new List<RWComponent>();

            //创建全部render
            public List<Mesh3DRenderer> CreateMesh3DRenderers<T>(ref int startIndex,params object[] objects) where T : Mesh3DRenderer
            {
                List<Mesh3DRenderer> list = new List<Mesh3DRenderer>();
                T tmp = null;  
                list.Add(tmp = (T)Activator.CreateInstance(typeof(T),startIndex, objects));
                startIndex += tmp.totalSprites;

                foreach (var com in components.FindAll(i => i is ActorComponent))
                    list.AddRange((com as ActorComponent).ownObject.CreateMesh3DRenderers<T>(ref startIndex));
                return list;
            }

            public void SetFacet(Mesh3DAsset.TriangleFacet[] facets)
            {
                for (int i = 0; i < facets.Length; i++)
                {
                    int maxVertice = Mathf.Max(facets[i].a, facets[i].b, facets[i].c);

                    while (animatedVertices.Count < maxVertice + 1)
                    {
                        animatedVertices.Add(Vector3.zero);
                        vertices.Add(Vector3.zero);
                        data.vertices.Add(Vector3.zero);
                    }

                    this.facets.Add(facets[i]);
                }
                Plugin.Log("Total Vertices : " + data.vertices.Count.ToString());
            }

            public void SetVertice(int index, Vector3 pos)
            {
                animatedVertices[index] = pos;
                data.vertices[index] = pos;
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
                    Vector3 v = animatedVertices[i];

                    Vector3.Scale(v, rootComponent.WorldScale);

                    v = RotateRound(v, Vector3.up, rootComponent.WorldRotation.x);
                    v = RotateRound(v, Vector3.forward, rootComponent.WorldRotation.y);
                    v = RotateRound(v, Vector3.right, rootComponent.WorldRotation.z);

                    v += rootComponent.WorldPosition;

                    vertices[i] = v;
                }
            }

            public Vector3 RotateRound(Vector3 position, Vector3 axis, float angle)
            {
                return Quaternion.AngleAxis(angle, axis) * (position);
            }

        }

        public class Mesh3DAsset
        {
            public List<Vector3> vertices = new List<Vector3>();
            public List<TriangleFacet> facets = new List<TriangleFacet>();

            public struct TriangleFacet
            {
                public int a;
                public int b;
                public int c;

                public TriangleFacet(int a, int b, int c)
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
            public int totalSprites;

            public Vector3[] vertices;
            public Vector2[] uvs;

            public Color[] verticeColorInFront;
            public Color[] verticeColorInBack;

            protected float maxZ;
            protected float minZ;

            public Mesh3DRenderer(Mesh3D mesh,ref int startIndex)
            {
                this.mesh = mesh;
                this.startIndex = startIndex;

                vertices = new Vector3[mesh.animatedVertices.Count];
                uvs = new Vector2[mesh.animatedVertices.Count];

                verticeColorInBack = new Color[mesh.animatedVertices.Count];
                verticeColorInFront = new Color[mesh.animatedVertices.Count];

                SetUpRenderInfo();
                startIndex += totalSprites;
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
                    Vector3 v = vertices[i] = mesh.vertices[i];

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

            public Mesh3DFrameRenderer(Mesh3D mesh,ref int startIndex) : base(mesh,ref startIndex)
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
                    if (mesh.animatedVertices[line.a].z > 0 && mesh.animatedVertices[line.b].z > 0)
                        newOverlay = Overlay.inFront;
                    else if (mesh.animatedVertices[line.a].z < 0 && mesh.animatedVertices[line.b].z < 0)
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
            public Mesh3DDotMatrixRenderer(Mesh3D mesh,ref int startIndex) : base(mesh,ref startIndex)
            {
            }

            public override void SetUpRenderInfo()
            {
                totalSprites = mesh.animatedVertices.Count;
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
            public Mesh3DFacetRenderer(Mesh3D mesh,ref int startIndex,string element, bool usingLight = false) : base(mesh, ref startIndex)
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

                public FacetRepresent(Mesh3DAsset.TriangleFacet copyFrom) : this(copyFrom.a,copyFrom.b, copyFrom.c)
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
