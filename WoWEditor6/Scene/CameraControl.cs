﻿using System;
using System.Drawing;
using System.Windows.Forms;
using WoWEditor6.UI;

namespace WoWEditor6.Scene
{
    /// <summary>
    /// TODO: CameraControl should use some sort of key bindings and not hard coded keys
    /// </summary>
    class CameraControl
    {
        private readonly MainWindow mWindow;
        private Point mLastCursorPos;
        private DateTime mLastUpdate = DateTime.Now;

        public CameraControl(MainWindow window)
        {
            mWindow = window;
        }

        public void Update()
        {
            if (mWindow.Focused == false || WorldFrame.Instance.State != AppState.World)
            {
                mLastCursorPos = Cursor.Position;
                mLastUpdate = DateTime.Now;
                return;
            }

            var keyState = new byte[256];
            UnsafeNativeMethods.GetKeyboardState(keyState);

            var positionChanged = false;
            var updateTerrain = false;
            var cam = WorldFrame.Instance.ActiveCamera;
            var diff = (float) (DateTime.Now - mLastUpdate).TotalSeconds;

            if(IsDown(keyState, Keys.W))
            {
                positionChanged = true;
                updateTerrain = true;
                cam.MoveForward(diff * 30);
            }

            if(IsDown(keyState, Keys.S))
            {
                positionChanged = true;
                updateTerrain = true;
                cam.MoveForward(-diff * 30);
            }

            if(IsDown(keyState, Keys.D))
            {
                positionChanged = true;
                updateTerrain = true;
                cam.MoveRight(diff * 30);
            }

            if(IsDown(keyState, Keys.A))
            {
                positionChanged = true;
                updateTerrain = true;
                cam.MoveRight(-diff * 30);
            }

            if (IsDown(keyState, Keys.Q))
            {
                positionChanged = true;
                cam.MoveUp(diff * 30);
            }

            if (IsDown(keyState, Keys.E))
            {
                positionChanged = true;
                cam.MoveUp(-diff * 30);
            }

            if(IsDown(keyState, Keys.RButton))
            {
                var curPos = Cursor.Position;
                var dx = curPos.X - mLastCursorPos.X;
                var dy = curPos.Y - mLastCursorPos.Y;

                if (dx != 0)
                    cam.Yaw(dx * 0.2f);
                if (dy != 0)
                    cam.Pitch(dy * 0.2f);
            }

            if (positionChanged)
                WorldFrame.Instance.MapManager.UpdatePosition(cam.Position, updateTerrain);

            mLastUpdate = DateTime.Now;
            mLastCursorPos = Cursor.Position;
        }

        private static bool IsDown(byte[] keyState, Keys key)
        {
            return ((keyState[(int) key]) & 0x80) != 0;
        }
    }
}