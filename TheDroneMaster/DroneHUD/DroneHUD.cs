using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using UnityEngine;
using RWCustom;

namespace TheDroneMaster
{
    public class PlayerDroneHUD : HudPart
    {
        public static PlayerDroneHUD instance;
        public DronePort port;
        public DroneHUDInputManager inputManager;

        public static Color LaserColor = new Color(1f, 0.26f, 0.45f);

        public List<FNode> publicNodes = new List<FNode>();

        public List<DroneUI> droneUIs = new List<DroneUI>();
        public DroneCursor cursor;

        public CustomFSprite lightSprite;
        public ResummmonDroneButton resummmonDroneButton;

        public float alpha;
        public bool reval = false;
        public bool lastButtonPress = false;

        public PlayerDroneHUD(HUD.HUD hud, DronePort port) : base(hud)
        {
            Plugin.Log("Init DroneHUD");
            this.port = port;
            instance = this;

            inputManager = new DroneHUDInputManager(this);
            cursor = new DroneCursor(this, 30f, Color.red);
            resummmonDroneButton = new ResummmonDroneButton(200f, 20f, 5f, Color.red);


            InitSprites();
            LaserDrone.pauseMovement = false;
        }

        public void InitSprites()
        {
            for (int i = droneUIs.Count - 1; i >= 0; i--)
            {
                droneUIs[i].InitSprites();
            }
            cursor.InitSprites(ref publicNodes, HUDPatch.currentCam);
            resummmonDroneButton.InitSprites(ref publicNodes, HUDPatch.currentCam);

            Vector2 screenVec = new Vector2(Screen.width, Screen.height);

            resummmonDroneButton.sourcePos = screenVec / 2f;
            resummmonDroneButton.centerPos = new Vector2(screenVec.x - resummmonDroneButton.width / 2f - 20f, screenVec.y - resummmonDroneButton.height / 2f - 20f);

            lightSprite = new CustomFSprite("Futile_White") { shader = hud.rainWorld.Shaders["CustomHoloGrid"] };


            publicNodes.Add(lightSprite);

            for (int i = 0; i < publicNodes.Count; i++)
            {
                hud.fContainers[0].AddChild(publicNodes[i]);
            }

            Plugin.Log("Init Sprites HUD : " + publicNodes.Count.ToString());
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            for (int i = droneUIs.Count - 1; i >= 0; i--)
            {
                droneUIs[i].Destroy();
            }
            for (int i = publicNodes.Count - 1; i >= 0; i--)
            {
                publicNodes[i].isVisible = false;
                publicNodes[i].RemoveFromContainer();
            }
            inputManager.Destroy();

            alpha = 0f;
            Plugin.postEffect.Strength = 0f;
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            for (int i = droneUIs.Count - 1; i >= 0; i--)
            {
                droneUIs[i].DrawSprites();
            }
            cursor.DrawSprites();

            lightSprite.isVisible = true;
            if (lightSprite._renderLayer != null && lightSprite._renderLayer._material != null)
            {
                lightSprite._renderLayer._material.SetColor("_GridColor", LaserColor);
                lightSprite._renderLayer._material.SetFloat("_GridAlpha", alpha);
                lightSprite._renderLayer._material.SetFloat("_RollY", Time.time);
            }
            lightSprite.MoveVertice(0, Vector2.zero);
            lightSprite.MoveVertice(1, new Vector2(Screen.width, 0));
            lightSprite.MoveVertice(2, new Vector2(Screen.width, Screen.height));
            lightSprite.MoveVertice(3, new Vector2(0, Screen.height));

            resummmonDroneButton.alpha = alpha;
            resummmonDroneButton.DrawSprites();

            Vector2 vector3 = Vector2.zero;
            lightSprite.color = new Color(Mathf.InverseLerp(-1f, 1f, vector3.x), Mathf.InverseLerp(-1f, 1f, vector3.y), 1f, 1f);

        }

        public override void Update()
        {
            base.Update();

            Plugin.postEffect.Strength = alpha;

            //让玩家可以用特殊键或者自定义的按键来打开无人机操作界面
            bool buttonPress = (hud.owner as Player).input[0].spec || Input.GetKeyDown(Plugin.instance.config.OpenHUDKey.Value);

            if (buttonPress != lastButtonPress && buttonPress)
            {
                reval = !reval;
                LaserDrone.pauseMovement = reval;
            }
            lastButtonPress = buttonPress;
            alpha = Mathf.Lerp(alpha, reval ? 1f : 0f, 0.2f);

            for (int i = droneUIs.Count - 1; i >= 0; i--)
            {
                droneUIs[i].Update();
            }

            cursor.alpha = alpha;
            cursor.Update();
            if(!inputManager.UsingMouseInput&&cursor.mode==DroneCursor.Mode.Free&&reval)
            {
                // 找到最近的按钮
                BaseButton3D nearestButton = null;
                float minDist = float.MaxValue;
                
                // 先检查召回按钮的距离
                float recallDistX = Mathf.Abs(inputManager.CursorPos.x - resummmonDroneButton.centerPos.x);
                float recallDistY = Mathf.Abs(inputManager.CursorPos.y - resummmonDroneButton.centerPos.y);
                
                if(recallDistX < resummmonDroneButton.width/2f + 30f && recallDistY < resummmonDroneButton.height/2f + 30f)
                {
                    float recallDist = Vector2.Distance(inputManager.CursorPos, resummmonDroneButton.centerPos);
                    minDist = recallDist;
                    nearestButton = resummmonDroneButton;
                }
                
                // 然后检查其他按钮
                foreach(var button in droneUIs.SelectMany(ui => ui.buttons).Where(b => b.alpha > 0.0001f))
                {
                    float distX = Mathf.Abs(inputManager.CursorPos.x - button.centerPos.x);
                    float distY = Mathf.Abs(inputManager.CursorPos.y - button.centerPos.y);
                    
                    // 增大吸引范围
                    if(distX < button.width/2f + 30f && distY < button.height/2f + 30f)
                    {
                        float dist = Vector2.Distance(inputManager.CursorPos, button.centerPos);
                        if(dist < minDist)
                        {
                            minDist = dist;
                            nearestButton = button;
                        }
                    }
                }

                // 对最近的按钮施加吸引力
                if(nearestButton != null)
                {
                    float dist = Vector2.Distance(inputManager.CursorPos, nearestButton.centerPos);
                    float maxSize = Mathf.Max(nearestButton.width, nearestButton.height);
                    
                    // 计算吸引力，距离越近力越大，但在非常接近中心时减少吸引力
                    float normalizedDist = dist / maxSize;
                    float force;
                    
                    if(normalizedDist < 0.2f) // 非常接近中心时减少吸引力
                    {
                        // 在中心区域，吸引力随距离线性增加
                        force = Mathf.Lerp(0.5f, 2f, normalizedDist / 0.2f);
                    }
                    else
                    {
                        // 在外部区域，吸引力随距离线性减少
                        force = Mathf.Lerp(4f, 0f, (normalizedDist - 0.2f) / 0.8f);
                    }
                    
                    // 计算方向向量
                    Vector2 dir = (nearestButton.centerPos - inputManager.CursorPos).normalized;
                    
                    // 施加力
                    // 距离按钮中心越近速度越慢
                    float slowdown = Mathf.Lerp(0.5f, 1f, normalizedDist);
                    inputManager.vel *= slowdown;
                    inputManager.vel += dir * force;
                }
            }
            resummmonDroneButton.Update();
        }

        public void TryRequestHUDForDrone(LaserDrone newDrone)
        {
            for (int i = droneUIs.Count - 1; i >= 0; i--)
            {
                if (droneUIs[i].followDrone.TryGetTarget(out var drone) && drone == newDrone)
                {
                    return;
                }
            }
            droneUIs.Add(new DroneUI(this, newDrone));
        }
    }

    public class ResummmonDroneButton : BaseButton3D
    {
        public ResummmonDroneButton(float width, float height, float depth, Color color) : base(width, height, depth, color, "Call Back Drones")
        {

        }

        public override void Update()
        {
            base.Update();
        }

        public override void PressMe(bool pressWithoutCommand = false)
        {
            if (pressed) return;

            if (PlayerDroneHUD.instance.port != null && PlayerDroneHUD.instance.port.owner.TryGetTarget(out var player))
            {
                if (!player.inShortcut)
                {
                    PlayerDroneHUD.instance.port.ResummonDrones();
                    pressed = true;
                }
            }
        }
    }

    public class BaseButton3D
    {
        public DroneUI ui;
        public Vector2 centerPos = Vector2.zero;
        public Vector2 sourcePos = Vector2.zero;
        public Vector2 pannelPos = Vector2.zero;

        public float width;
        public float height;
        public float depth;
        public float lineWidth = 1f;
        public string message;

        public Color color;

        public float alpha;
        public float press;
        public bool pressed = false;
        public bool pressLock = false;
        protected bool isVisble => alpha > 0.0001f;
        bool lastIsVisible = true;

        public Vector2[] layer1Pos = new Vector2[4];
        Vector2[] layer2Pos = new Vector2[4];

        CustomFSprite[] layer1Sprites = new CustomFSprite[4];
        CustomFSprite[] layer2Sprites = new CustomFSprite[4];
        CustomFSprite[] layerConnectSprites = new CustomFSprite[4];

        FLabel pannel;

        public BaseButton3D(float width, float height, float depth, Color color, string message, DroneUI ui = null)
        {
            this.ui = ui;
            this.width = width;
            this.height = height;
            this.depth = depth;
            this.color = color;
            this.message = message;

            PlayerDroneHUD.instance.inputManager.pressKeyUp += OnPressKeyUp;
            PlayerDroneHUD.instance.inputManager.pressKeyDown += PressMe;
            Update();
        }

        public virtual void Update()
        {
            if (!isVisble) return;
            press = Mathf.Lerp(press, pressed ? 1f : 0f, 0.5f);


            layer1Pos[0] = centerPos + Vector2.left * width / 2f + Vector2.up * height / 2f;
            layer1Pos[1] = centerPos + Vector2.right * width / 2f + Vector2.up * height / 2f;
            layer1Pos[2] = centerPos + Vector2.right * width / 2f + Vector2.down * height / 2f;
            layer1Pos[3] = centerPos + Vector2.left * width / 2f + Vector2.down * height / 2f;

            Vector2 leftUp = (Vector2.up + Vector2.left) * depth * Mathf.Clamp((1 - press), 0.3f, 1f);
            for (int i = 0; i < 4; i++)
            {
                layer2Pos[i] = layer1Pos[i] + leftUp;

                layer1Pos[i] = Vector2.Lerp(layer1Pos[i], sourcePos, 1f - alpha);
                layer2Pos[i] = Vector2.Lerp(layer2Pos[i], sourcePos, 1f - alpha);
            }

            pannelPos = centerPos + leftUp;
        }

        public virtual void Destroy()
        {
            try
            {
                Plugin.Log("Destroying button: " + message);
                
                // 取消注册事件处理程序
                if(PlayerDroneHUD.instance != null && PlayerDroneHUD.instance.inputManager != null)
                {
                    PlayerDroneHUD.instance.inputManager.pressKeyUp -= OnPressKeyUp;
                    PlayerDroneHUD.instance.inputManager.pressKeyDown -= PressMe;
                }
                
                // 确保按钮不可见
                alpha = 0f;
                pressed = false;
                pressLock = false;
                
                // 清理资源
                if(layer1Sprites != null)
                {
                    foreach(var sprite in layer1Sprites)
                    {
                        if(sprite != null)
                        {
                            sprite.isVisible = false;
                            sprite.RemoveFromContainer();
                        }
                    }
                }
                
                if(layer2Sprites != null)
                {
                    foreach(var sprite in layer2Sprites)
                    {
                        if(sprite != null)
                        {
                            sprite.isVisible = false;
                            sprite.RemoveFromContainer();
                        }
                    }
                }
                
                if(layerConnectSprites != null)
                {
                    foreach(var sprite in layerConnectSprites)
                    {
                        if(sprite != null)
                        {
                            sprite.isVisible = false;
                            sprite.RemoveFromContainer();
                        }
                    }
                }
                
                if(pannel != null)
                {
                    pannel.isVisible = false;
                    pannel.RemoveFromContainer();
                }
                
                Plugin.Log("Button destroyed: " + message);
            }
            catch(Exception e)
            {
                Plugin.Log("Error destroying button: " + e.Message);
            }
        }

        public void InitSprites(ref List<FNode> nodes, RoomCamera rCam)
        {
            for (int i = 0; i < 4; i++)
            {
                layer1Sprites[i] = new CustomFSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["Hologram"] };
                layer2Sprites[i] = new CustomFSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["Hologram"] };
                layerConnectSprites[i] = new CustomFSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["Hologram"] };

                nodes.Add(layer1Sprites[i]);
                nodes.Add(layer2Sprites[i]);
                nodes.Add(layerConnectSprites[i]);
            }
            pannel = new FLabel(Custom.GetFont(), message) { scale = 1.1f };
            nodes.Add(pannel);
        }

        public virtual void OnPressKeyUp()
        {
            if (pressed) pressed = false;
        }

        public void DrawSprites()
        {
            if (lastIsVisible != isVisble)
            {
                for (int i = 0; i < 4; i++)
                {
                    layer1Sprites[i].isVisible = isVisble;
                    layer2Sprites[i].isVisible = isVisble;
                    layerConnectSprites[i].isVisible = isVisble;
                }
                pannel.isVisible = isVisble;
                lastIsVisible = isVisble;
            }
            if (!isVisble) return;

            for (int i = 0; i < 4; i++)
            {
                int firstPosIndex = i;
                int secondPosIndex = i + 1 > 3 ? 0 : i + 1;

                Vector2 layer1Pos_1 = layer1Pos[firstPosIndex];
                Vector2 layer1pos_2 = layer1Pos[secondPosIndex];
                Vector2 layer1Dir = (layer1pos_2 - layer1Pos_1);

                Vector2 layer2Pos_1 = layer2Pos[firstPosIndex];
                Vector2 layer2pos_2 = layer2Pos[secondPosIndex];
                Vector2 layer2Dir = (layer2pos_2 - layer2Pos_1);

                Vector2 layerConnectPos_1 = layer1Pos[firstPosIndex];
                Vector2 layerConnectPos_2 = layer2Pos[firstPosIndex];
                Vector2 layerConnectDir = layerConnectPos_2 - layerConnectPos_1;

                layer1Sprites[i].MoveVertice(0, layer1Pos_1 + Custom.PerpendicularVector(layer1Dir) * lineWidth);
                layer1Sprites[i].MoveVertice(1, layer1Pos_1 - Custom.PerpendicularVector(layer1Dir) * lineWidth);
                layer1Sprites[i].MoveVertice(2, layer1pos_2 + Custom.PerpendicularVector(layer1Dir) * lineWidth);
                layer1Sprites[i].MoveVertice(3, layer1pos_2 - Custom.PerpendicularVector(layer1Dir) * lineWidth);

                layer2Sprites[i].MoveVertice(0, layer2Pos_1 + Custom.PerpendicularVector(layer2Dir) * lineWidth);
                layer2Sprites[i].MoveVertice(1, layer2Pos_1 - Custom.PerpendicularVector(layer2Dir) * lineWidth);
                layer2Sprites[i].MoveVertice(2, layer2pos_2 + Custom.PerpendicularVector(layer2Dir) * lineWidth);
                layer2Sprites[i].MoveVertice(3, layer2pos_2 - Custom.PerpendicularVector(layer2Dir) * lineWidth);

                layerConnectSprites[i].MoveVertice(0, layerConnectPos_1 + Custom.PerpendicularVector(layerConnectDir) * lineWidth);
                layerConnectSprites[i].MoveVertice(1, layerConnectPos_1 - Custom.PerpendicularVector(layerConnectDir) * lineWidth);
                layerConnectSprites[i].MoveVertice(2, layerConnectPos_2 + Custom.PerpendicularVector(layerConnectDir) * lineWidth);
                layerConnectSprites[i].MoveVertice(3, layerConnectPos_2 - Custom.PerpendicularVector(layerConnectDir) * lineWidth);
            }
            pannel.SetPosition(pannelPos);

            for (int i = 0; i < 4; i++)
            {
                for (int k = 0; k < 4; k++)
                {
                    layer1Sprites[i].verticeColors[k] = color;
                    layer2Sprites[i].verticeColors[k] = color;
                    layerConnectSprites[i].verticeColors[k] = color;

                    layer1Sprites[i].verticeColors[k].a = alpha;
                    layer2Sprites[i].verticeColors[k].a = alpha;
                    layerConnectSprites[i].verticeColors[k].a = alpha;
                }
            }
            pannel.alpha = alpha;
        }

        public virtual void PressMe()
        {
            if (PlayerDroneHUD.instance.inputManager.IsPressMe(this)) PressMe(false);
        }

        public virtual void PressMe(bool pressWithoutCommand = false)
        {
        }
    }

    public class Button3D : BaseButton3D
    {
        public DroneCommandSystem.CommandType command;
        public DroneCursor.Mode mode;
        public bool pressWithoutCommand = false;

        public Button3D(float width, float height, float depth, Color color, string message, DroneCommandSystem.CommandType command, DroneCursor.Mode mode, DroneUI ui = null) : base(width, height, depth, color, message, ui)
        {
            this.command = command;
            this.mode = mode;
        }

        public override void PressMe(bool pressWithoutCommand = false)
        {
            if (PlayerDroneHUD.instance.inputManager.buttonPressLock) return;
            this.pressWithoutCommand = pressWithoutCommand;
            pressed = true;
        }
        public override void OnPressKeyUp()
        {
            if (pressed)
            {
                if (ui != null)
                {
                    foreach (var button in ui.buttons) button.pressed = false;

                    ui.droneHUD.cursor.ChangeMode(mode, mode == DroneCursor.Mode.Free ? null : this);
                }
                if (command == DroneCommandSystem.CommandType.Auto && !pressWithoutCommand)
                {
                    if (ui.followDrone.TryGetTarget(out var drone))
                    {
                        drone.AI.commandSystem.SendCommand(DroneCommandSystem.CommandType.Auto);
                    }
                }
            }
        }
    }

    public class DroneHUDInputManager
    {
        public float moveCursorSpeed = 20f;
        public Vector2 vel = Vector2.zero;
        public PlayerDroneHUD droneHUD;

        public Vector2 CursorPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        public bool UsingMouseInput = true;
        public bool GetPressKeyDown = false;

        public bool buttonPressLock = false;

        public PressKeyUp pressKeyUp;
        public PressKeyDown pressKeyDown;

        public static List<Player.InputPackage> inputPackages = new List<Player.InputPackage>();

        public DroneHUDInputManager(PlayerDroneHUD hud)
        {
            droneHUD = hud;
            UsingMouseInput = !Plugin.instance.config.UsingPlayerInput.Value;
            Plugin.frameUpdate += Update;
        }

        public void Update()
        {
            if (UsingMouseInput)
            {
                CursorPos = Input.mousePosition;
                GetPressKeyDown = Input.GetMouseButton(0);
                if (Input.GetMouseButtonDown(0) && pressKeyDown != null) pressKeyDown();
                if (Input.GetMouseButtonUp(0) && pressKeyUp != null) pressKeyUp();
            }
            else
            {
                // 计算有多少个输入包含非零移动值(x或y不为0)
                int movingPackages = inputPackages.Count(p => p.x != 0 || p.y != 0);
                
                moveCursorSpeed = Mathf.Lerp(0.1f, 8f, movingPackages / 40f);

                vel *= 0.68f;
                if (inputPackages.Count > 0)
                {
                    vel += inputPackages[0].analogueDir * moveCursorSpeed;
                }
                // for (int i = 0; i < inputPackages.Count; i++)
                // {
                //     if (inputPackages[i].x == 0 && inputPackages[i].y == 0) break;
                //     moveCursorSpeed += 0.5f;
                // }

                if (droneHUD.reval) // reval 是 reveal 的缩写,表示是否显示/激活无人机HUD界面
                {
                    CursorPos += vel;
                    if (CursorPos.y < 0 || CursorPos.y > Screen.height)
                    {
                        vel.y *= -1;
                    }
                    if (CursorPos.x < 0 || CursorPos.x > Screen.width)
                    {
                        vel.x *= -1;
                    }
                    CursorPos.x = Mathf.Clamp(CursorPos.x, 0, Screen.width);
                    CursorPos.y = Mathf.Clamp(CursorPos.y, 0, Screen.height);

                    // Clamp是一个数学函数,用于将一个数值限制在指定的最小值和最大值之间
                    // 这里用于确保光标位置不会超出屏幕范围(0到Screen.width/height)
                    // CursorPos.x = Mathf.Clamp(CursorPos.x + inputPackages[0].x * moveCursorSpeed, 0, Screen.width);
                    // CursorPos.y = Mathf.Clamp(CursorPos.y + inputPackages[0].y * moveCursorSpeed, 0, Screen.height);

                    GetPressKeyDown = inputPackages[0].thrw;

                    if (GetPressKeyDown && inputPackages.Count > 1 && GetPressKeyDown != inputPackages[1].thrw && pressKeyDown != null) pressKeyDown();
                    if (!GetPressKeyDown && inputPackages.Count > 1 && GetPressKeyDown != inputPackages[1].thrw && pressKeyUp != null) pressKeyUp();
                }
            }
        }

        public void Destroy()
        {
            Plugin.frameUpdate -= Update;
        }

        public bool IsPressMe(BaseButton3D button)
        {
            Plugin.Log("Button:" + button.message + button.centerPos.ToString());
            

            if (!GetPressKeyDown || buttonPressLock || droneHUD.cursor.mode != DroneCursor.Mode.Free) return false;
            if (Mathf.Abs(CursorPos.x - button.centerPos.x) < button.width / 2f && Mathf.Abs(CursorPos.y - button.centerPos.y) < button.height / 2f)
            {
                Plugin.Log("This Button can be press");
                return true;
            }
            return false;
        }

        public static void GetPlayerInput(Player.InputPackage package)
        {
            inputPackages.Insert(0, package);
            if (inputPackages.Count > 40) inputPackages.RemoveAt(inputPackages.Count - 1);
        }

        public delegate void PressKeyUp();
        public delegate void PressKeyDown();
    }
}
