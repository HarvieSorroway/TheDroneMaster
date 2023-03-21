﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using RWCustom;
using UnityEngine;

namespace TheDroneMaster
{
    public class DroneUI
    {
        public readonly float buttonHeight = 50f;
        public readonly float buttonGap = 15f;

        public WeakReference<LaserDrone> followDrone;
        public DroneHUD droneHUD;


        public List<Button3D> buttons = new List<Button3D>();
        public Button3D autoModeButton;
        public List<FNode> nodes = new List<FNode>();

        public DroneUI(DroneHUD part,LaserDrone drone)
        {
            this.droneHUD = part;
            this.followDrone = new WeakReference<LaserDrone>(drone);

            autoModeButton = new Button3D(40f, 20f, 5f, Color.red, "Auto", DroneCommandSystem.CommandType.Auto, DroneCursor.Mode.Free, this);

            buttons.Add(autoModeButton);
            buttons.Add(new Button3D(50f, 20f, 5f, Color.red, "Attack", DroneCommandSystem.CommandType.Attack,DroneCursor.Mode.SelectCreature, this));
            buttons.Add(new Button3D(40f, 20f, 5f, Color.red, "Stay", DroneCommandSystem.CommandType.Stay,DroneCursor.Mode.SelectPos, this));

            InitSprites();
        }

        public void InitSprites()
        {
            foreach(var button in buttons)
            {
                button.InitSprites(ref nodes, HUDPatch.currentCam);
            }

            foreach (var node in nodes)
            {
                droneHUD.hud.fContainers[0].AddChild(node);
            }

            if(followDrone.TryGetTarget(out var drone))
            {
                drone.AI.commandSystem.SyncUIButtons();
            }
        }

        public void DrawSprites()
        {
            foreach(var button in buttons)
            {
                button.DrawSprites();
            }
        }

        public void Update()
        {
            try
            {
                if (followDrone.TryGetTarget(out var drone))
                {
                    Vector2 basePos = drone.firstChunk.pos + buttonHeight * Vector2.down - HUDPatch.currentCam.pos;
                    foreach (var button in buttons)
                    {
                        button.centerPos = basePos;
                        button.sourcePos = drone.firstChunk.pos - HUDPatch.currentCam.pos;

                        bool inSameRoomWithPlayer = droneHUD.port.owner.TryGetTarget(out var player) && player.room == drone.room && !player.inShortcut && !drone.inShortcut;
                        button.alpha = droneHUD.alpha * (inSameRoomWithPlayer ? 1f : 0f);
                        button.Update();
                        basePos += (button.width + buttonGap) * Vector2.right;
                    }
                }
                else
                {
                    Destroy();
                }
            }
            catch
            {
                Destroy();
            }
        }

        public void Destroy()
        {
            foreach(var button in buttons)
            {
                button.Destroy();
            }
            buttons.Clear();


            foreach (var node in nodes)
            {
                node.isVisible = false;
                node.RemoveFromContainer();
            }

            droneHUD.droneUIs.Remove(this);
        }
    }
}
