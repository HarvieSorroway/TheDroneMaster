using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheDroneMaster.CustomLore.SpecificScripts.Simple3DObject.Mesh3D;
using static TheDroneMaster.PlayerPatchs;
using Random = UnityEngine.Random;

namespace TheDroneMaster.CustomLore.SpecificScripts
{
    public class Simple3DObject : CosmeticSprite
    {
        public static Simple3DObject instance;

        Mesh3D mesh = new Mesh3D();
        Mesh3DRenderer mesh3Drenderer;

        Player player;
        GlyphLabel label;

        int freshLabelCount = 0;


        public Simple3DObject(Room room, Player player)
        {
            instance = this;

            this.player = player;
            Mesh3D.TriangleFacet[] facets = new Mesh3D.TriangleFacet[]
            {
                new TriangleFacet(0,1,2),
                new TriangleFacet(0,2,3),
                new TriangleFacet(0,3,4),
                new TriangleFacet(0,4,1),

                new TriangleFacet(5,6,7),
                new TriangleFacet(5,7,8),
                new TriangleFacet(5,8,9),
                new TriangleFacet(5,9,6),

                new TriangleFacet(10,11,12),
                new TriangleFacet(10,12,13),
                new TriangleFacet(10,13,14),
                new TriangleFacet(10,14,11),

                new TriangleFacet(15,16,17),
                new TriangleFacet(15,17,18),
                new TriangleFacet(15,18,19),
                new TriangleFacet(15,19,16),
            };
            mesh.SetFacet(facets);

            mesh.SetVertice(0, Vector3.left * 10f);
            mesh.SetVertice(1, Vector3.left * 20f + Vector3.forward * 5f + Vector3.up * 5f);
            mesh.SetVertice(2, Vector3.left * 20f + Vector3.forward * 5f + Vector3.up * -5f);
            mesh.SetVertice(3, Vector3.left * 20f + Vector3.forward * -5f + Vector3.up * -5f);
            mesh.SetVertice(4, Vector3.left * 20f + Vector3.forward * -5f + Vector3.up * 5f);

            mesh.SetVertice(5, Vector3.forward * -10f);
            mesh.SetVertice(6, Vector3.forward * -20f + Vector3.left * 5f + Vector3.up * 5f);
            mesh.SetVertice(7, Vector3.forward * -20f + Vector3.left * 5f + Vector3.up * -5f);
            mesh.SetVertice(8, Vector3.forward * -20f + Vector3.left * -5f + Vector3.up * -5f);
            mesh.SetVertice(9, Vector3.forward * -20f + Vector3.left * -5f + Vector3.up * 5f);

            mesh.SetVertice(10, Vector3.right * 10f);
            mesh.SetVertice(11, Vector3.right * 20f + Vector3.forward * -5f + Vector3.up * 5f);
            mesh.SetVertice(12, Vector3.right * 20f + Vector3.forward * -5f + Vector3.up * -5f);
            mesh.SetVertice(13, Vector3.right * 20f + Vector3.forward * 5f + Vector3.up * -5f);
            mesh.SetVertice(14, Vector3.right * 20f + Vector3.forward * 5f + Vector3.up * 5f);

            mesh.SetVertice(15, Vector3.forward * 10f);
            mesh.SetVertice(16, Vector3.forward * 20f + Vector3.right * 5f + Vector3.up * 5f);
            mesh.SetVertice(17, Vector3.forward * 20f + Vector3.right * 5f + Vector3.up * -5f);
            mesh.SetVertice(18, Vector3.forward * 20f + Vector3.right * -5f + Vector3.up * -5f);
            mesh.SetVertice(19, Vector3.forward * 20f + Vector3.right * -5f + Vector3.up * 5f);

            mesh3Drenderer = new SpecialMesh3DFrameRenderer(mesh, 0);
            mesh3Drenderer.shader = "Hologram";

            mesh3Drenderer.SetVerticeColor(LaserDroneGraphics.defaulLaserColor, true);
            mesh3Drenderer.SetVerticeColor(LaserDroneGraphics.defaulLaserColor, false);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[mesh3Drenderer.totalSprites];
            mesh3Drenderer.InitSprites(sLeaser, rCam);

            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[i]);
                }
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            mesh3Drenderer.DrawSprites(sLeaser, rCam, timeStacker, camPos, player.DangerPos);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            mesh.rotation = new Vector3(90f * (Mathf.Sin(Time.time * 0.2f)) + 1f, mesh.rotation.y + 2f, 90f * (Mathf.Sin(Time.time * 0.2f)) + 1f);
            mesh3Drenderer.Update();

            if (freshLabelCount == 0)
            {
                if (label != null)
                    label.Destroy();
                label = new GlyphLabel(player.DangerPos + Vector2.up * 20f + Vector2.right * 40f, GlyphLabel.RandomString(Random.Range(3, 8), Random.Range(100, 10000), false));
                room.AddObject(label);
                freshLabelCount = Random.Range(15, 30);
            }
            else
                freshLabelCount--;
            label.setPos = player.DangerPos + Vector2.up * 20f + Vector2.right * 40f;
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
            public string shader;

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

            public void SetVerticeColor(Color color, bool inFront)
            {
                for (int i = 0; i < verticeColorInFront.Length; i++)
                {
                    SetVerticeColor(i, color, inFront);
                }
            }

            public void SetVerticeColor(int index, Color color, bool inFront)
            {
                if (inFront)
                    verticeColorInFront[index] = color;
                else
                    verticeColorInBack[index] = color;
            }

            public void SetUV(int index, Vector2 uv)
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

                    if (v.z < minZ)
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
            public float thickness;
            public Mesh3DFrameRenderer(Mesh3D mesh, int startIndex,float thickness = 1f) : base(mesh, startIndex)
            {
                this.thickness = thickness;
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
                    if(shader != string.Empty)
                    {
                        sLeaser.sprites[i + startIndex].shader = rCam.game.rainWorld.Shaders[shader];
                    }
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


                    (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveVertice(0, posA + perpDir * thickness / 2f);
                    (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveVertice(1, posA - perpDir * thickness / 2f);
                    (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveVertice(2, posB - perpDir * thickness / 2f);
                    (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveVertice(3, posB + perpDir * thickness / 2f);

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
            public Mesh3DFacetRenderer(Mesh3D mesh, int startIndex, string element, bool usingLight = false) : base(mesh, startIndex)
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
                for (int i = 0; i < facetRepresents.Count; i++)
                {
                    facetRepresents[i] = new FacetRepresent(mesh.facets[i]);
                }
            }

            public override void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                for (int i = 0; i < totalSprites; i++)
                {
                    facetRepresents[i].linkedSpriteIndex = i + startIndex;
                    sLeaser.sprites[i + startIndex] = new TriangleMesh(Element, new TriangleMesh.Triangle[1] { new TriangleMesh.Triangle(0, 1, 2) }, true, true);

                    var facet = mesh.facets[i];
                    (sLeaser.sprites[i + startIndex] as TriangleMesh).UVvertices[0] = uvs[facet.a];
                    (sLeaser.sprites[i + startIndex] as TriangleMesh).UVvertices[1] = uvs[facet.b];
                    (sLeaser.sprites[i + startIndex] as TriangleMesh).UVvertices[2] = uvs[facet.c];
                }
            }

            public override void Update()
            {
                base.Update();
                for (int i = 0; i < facetRepresents.Count; i++)
                {
                    facetRepresents[i].CaculateNormalAndSort(vertices);
                }
                facetRepresents.Sort((x, y) => x.sortZ.CompareTo(y.sortZ));
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 centerPos)
            {
                for (int i = 0; i < facetRepresents.Count - 1; i++)
                {
                    sLeaser.sprites[facetRepresents[i + 1].linkedSpriteIndex].MoveInFrontOfOtherNode(sLeaser.sprites[facetRepresents[i].linkedSpriteIndex]);
                }

                for (int i = 0; i < facetRepresents.Count; i++)
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

                public FacetRepresent(TriangleFacet copyFrom) : this(copyFrom.a, copyFrom.b, copyFrom.c)
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

        public class SpecialMesh3DFrameRenderer : Mesh3DFrameRenderer
        {
            public SpecialMesh3DFrameRenderer(Mesh3D mesh,int startIndex) : base(mesh, startIndex, 1f)
            {
            }

            public override void Update()
            {
                base.Update();
                for(int i = 0;i < vertices.Length;i++)
                {
                    vertices[i] += vertices[i].normalized * Mathf.Pow(Mathf.Sin(Time.time * 3f),2f) * 10f;
                }
            }
        }
    }
}
