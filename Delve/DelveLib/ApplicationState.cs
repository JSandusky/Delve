using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DelveLib
{
    public struct MouseButtonEventArgs
    {
        public bool Left;
        public bool Right;
        public bool Middle;
        public bool Handled;
    }

    public class ApplicationState
    {
        public KeyboardState Keyboard = new KeyboardState();
        public MouseState Mouse = new MouseState();
        public List<GamePadState> GamePads = new List<GamePadState>();

        public event EventHandler<Keys> KeyPressed;
        public event EventHandler<Keys> KeyReleased;
        public event EventHandler<Vector2> MouseMoved;
        public event EventHandler<MouseButtonEventArgs> MouseButton;
        public event EventHandler<int> MouseWheel;

        public void Update()
        {
            var oldKeyboard = Keyboard;
            var oldMouse = Mouse;

            Keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();

            bool anyNew = false;
            bool anyLost = false;

            var oldKeys = oldKeyboard.GetPressedKeys();
            var newKeys = Keyboard.GetPressedKeys();
            if (KeyReleased != null)
            {
                for (int i = 0; i < oldKeys.Length; ++i)
                    if (!newKeys.Contains(oldKeys[i]))
                        KeyReleased(this, oldKeys[i]);
            }
            if (KeyPressed != null)
            {
                for (int i = 0; i < newKeys.Length; ++i)
                    if (!oldKeys.Contains(newKeys[i]))
                        KeyPressed(this, newKeys[i]);
            }

            Mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
            if (oldMouse.ScrollWheelValue != Mouse.ScrollWheelValue && MouseWheel != null)
                MouseWheel(this, Mouse.ScrollWheelValue - oldMouse.ScrollWheelValue);
            if (oldMouse.Position != Mouse.Position && MouseMoved != null)
                MouseMoved(this, (Mouse.Position - oldMouse.Position).ToVector2());

            bool leftChanged = oldMouse.LeftButton != Mouse.LeftButton;
            bool rightChanged = oldMouse.RightButton != Mouse.RightButton;
            bool middleChanged = oldMouse.MiddleButton != Mouse.MiddleButton;
            if ((leftChanged || rightChanged || middleChanged) && MouseButton != null)
                MouseButton(this, new MouseButtonEventArgs { Left = leftChanged, Right = rightChanged, Middle = middleChanged, Handled = false });

            while (GamePads.Count < GamePad.MaximumGamePadCount)
                GamePads.Add(new GamePadState());
            for (int i = 0; i < GamePad.MaximumGamePadCount; ++ i)
                GamePads[i] = GamePad.GetState(i, GamePadDeadZone.Circular);
        }
    }
}
