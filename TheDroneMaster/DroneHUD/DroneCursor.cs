using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using UnityEngine;

namespace TheDroneMaster
{
    public class DroneCursor
    {
        public readonly float distanceThreshold = 55f;
        public readonly float lineWidth = 1f;

        public PlayerDroneHUD hud;

        public CustomFSprite[] sprites = new CustomFSprite[4];
        public CustomFSprite connectionLine;
        public float staticWidth;
        public Color color;

        public Mode mode;

        public Vector2 centerPos = Vector2.zero;
        public Vector2[] vertexPos = new Vector2[4];

        public Button3D currentConnectButton;
        public Creature focusCreature;
        public WorldCoordinate currentMouseCoord;

        public float alpha;
        public bool isVisible => alpha > 0.0001f;
        public bool lastIsVisible = true;
        public float dynamicWidth
        {
            get
            {
                switch (mode)
                {
                    case Mode.SelectCreature:
                        return focusCreature == null ? staticWidth : staticWidth * 1.5f;
                    case Mode.SelectPos:
                        return staticWidth * 0.7f;
                    case Mode.Free:
                    default:
                        return staticWidth;
                }
            }
        }


        public DroneCursor(PlayerDroneHUD hud, float width,Color color)
        {
            this.hud = hud;
            this.staticWidth = width;
            this.color = color;

            hud.inputManager.pressKeyDown += OnPressKeyUp;
        }

        public void InitSprites(ref List<FNode> nodes,RoomCamera rCam)
        {
            connectionLine = new CustomFSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["Hologram"] };
            nodes.Add(connectionLine);

            for (int i = 0;i < 4; i++)
            {
                sprites[i] = new CustomFSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["Hologram"] };
                nodes.Add(sprites[i]);

                for(int k = 0;k < 4; k++)
                {
                    sprites[i].verticeColors[k] = color;
                    connectionLine.verticeColors[k] = color;
                }
            }
        }

        public void DrawSprites()
        {
            if (!isVisible) return;

            for(int i = 0;i < 4; i++)
            {
                int firstPosIndex = i;
                int secondPosIndex = i + 1 > 3 ? 0 : i + 1;

                Vector2 pos1 = vertexPos[firstPosIndex];
                Vector2 pos2 = vertexPos[secondPosIndex];
                Vector2 dir = (pos2 - pos1);

                sprites[i].MoveVertice(0, pos1 + Custom.PerpendicularVector(dir) * lineWidth);
                sprites[i].MoveVertice(1, pos1 - Custom.PerpendicularVector(dir) * lineWidth);
                sprites[i].MoveVertice(2, pos2 + Custom.PerpendicularVector(dir) * lineWidth);
                sprites[i].MoveVertice(3, pos2 - Custom.PerpendicularVector(dir) * lineWidth);

                for (int k = 0; k < 4; k++)
                {
                    sprites[i].verticeColors[k].a = alpha;
                }
            }

            if(currentConnectButton != null)
            {
                Vector2 anchorPos1 = currentConnectButton.layer1Pos[0];
                Vector2 anchorPos2 = vertexPos[0];
                Vector2 dir = anchorPos2 - anchorPos1;

                for(int i = 0;i < 4; i++)
                {
                    connectionLine.MoveVertice(0, anchorPos1 + Custom.PerpendicularVector(dir) * lineWidth);
                    connectionLine.MoveVertice(1, anchorPos1 - Custom.PerpendicularVector(dir) * lineWidth);
                    connectionLine.MoveVertice(2, anchorPos2 + Custom.PerpendicularVector(dir) * lineWidth);
                    connectionLine.MoveVertice(3, anchorPos2 - Custom.PerpendicularVector(dir) * lineWidth);

                    connectionLine.verticeColors[i].a = alpha;
                }
            }
        }

        public void Update()
        {
            if(lastIsVisible != isVisible)
            {
                for(int i = 0;i < 4; i++)
                {
                    sprites[i].isVisible = isVisible;
                }
                connectionLine.isVisible = isVisible;
                lastIsVisible = isVisible;
            }
            if (!isVisible) return;
            connectionLine.isVisible = currentConnectButton != null;


            Vector2 mousePos = hud.inputManager.CursorPos;
            Vector2 camPos = HUDPatch.currentCam.pos;
            Room camRoom = HUDPatch.currentCam.room;
            switch (mode)
            {
                case Mode.SelectCreature:
                    UpdateFocusCreature(mousePos,camPos,camRoom);
                    if (focusCreature != null) centerPos = focusCreature.DangerPos - camPos;
                    else centerPos = hud.inputManager.CursorPos;
                    break;
                case Mode.SelectPos:
                    var currentMouseTile = camRoom.GetTilePosition(mousePos + camPos);
                    var coord = camRoom.GetWorldCoordinate(mousePos + camPos);
                    if (camRoom.aimap.getAItile(coord.Tile).acc != AItile.Accessibility.Solid) currentMouseCoord = coord;
                    centerPos = camRoom.MiddleOfTile(currentMouseTile) - camPos;
                    break;
                case Mode.Free:
                default:
                    centerPos = mousePos;
                    break;
            }

            vertexPos[0] = centerPos + (Vector2.left + Vector2.up) * dynamicWidth / 2f;
            vertexPos[1] = centerPos + (Vector2.right + Vector2.up) * dynamicWidth / 2f;
            vertexPos[2] = centerPos + (Vector2.right + Vector2.down) * dynamicWidth / 2f;
            vertexPos[3] = centerPos + (Vector2.left + Vector2.down) * dynamicWidth / 2f;
        }

        public void UpdateFocusCreature(Vector2 mousePos,Vector2 camPos,Room room)
        {
            Creature targetCreature = null;
            float minDist = float.MaxValue;

            //查找房间内最贴近鼠标的生物
            foreach (var updateObj in room.updateList)
            {
                if (!(updateObj is Creature) || updateObj is LaserDrone || updateObj is Player) continue;

                Creature current = updateObj as Creature;

                foreach (var bodychunk in current.bodyChunks)
                {
                    float currentDist = Manhatton(bodychunk.pos, mousePos + camPos);

                    if (currentDist < distanceThreshold && currentDist < minDist + bodychunk.rad)
                    {
                        targetCreature = current;
                        minDist = currentDist;

                        break;
                    }
                }
            }

            focusCreature = targetCreature;
        }

        public void ChangeMode(Mode newMode,Button3D fromButton)
        {
            if (newMode == mode) return;

            mode = newMode;
            currentConnectButton = fromButton;

            if (newMode != Mode.Free) hud.inputManager.buttonPressLock = true;
        }

        public void Destroy()
        {
            currentConnectButton = null;

            connectionLine.isVisible = false;
            connectionLine.RemoveFromContainer();

            hud.inputManager.pressKeyUp -= OnPressKeyUp;
        }

        public void OnPressKeyUp()
        {
            LaserDrone drone = null;
            bool getDrone = false;
            if (currentConnectButton != null)
            {
                currentConnectButton.pressLock = false;
                currentConnectButton.pressed = false;
                getDrone = currentConnectButton.ui.followDrone.TryGetTarget(out drone);
            }


            switch (mode)
            {
                case Mode.SelectCreature:
                    if (getDrone)
                    {
                        if (focusCreature != null) drone.AI.commandSystem.SendCommand(currentConnectButton.command, focusCreature.abstractCreature);
                        else currentConnectButton.ui.autoModeButton.PressMe();
                    }
                    break;
                case Mode.SelectPos:
                    if (getDrone) drone.AI.commandSystem.SendCommand(currentConnectButton.command, null, currentMouseCoord);
                    break;
                case Mode.Free:
                default:
                    break;
            }
            ChangeMode(Mode.Free, null);
            hud.inputManager.buttonPressLock = false;
        }

        public float Manhatton(Vector2 a,Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        public enum Mode
        {
            Free,
            SelectCreature,
            SelectPos
        }
    }
}
