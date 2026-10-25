using UnityEngine;
using UnityEngine.InputSystem;

namespace Paname
{
    // Only the input adapter knows about hardware. Simulation consumes PlayerCommand.
    public struct PlayerCommand
    {
        public float move;
        public bool jump, fire, special;
    }

    public sealed class LocalSlot
    {
        public bool joined;
        public int hero;
        public bool secondKeyboard;
        public Gamepad pad;
        public bool Connected => pad != null && pad.added;
        public string DeviceName => Connected ? pad.displayName : secondKeyboard ? "Clavier 2" : "Clavier";
    }

    public static class LocalControls
    {
        public static PlayerCommand Read(LocalSlot slot, int index)
        {
            var cmd = new PlayerCommand();
            var k = Keyboard.current;
            if (k != null && (index == 0 || slot.secondKeyboard))
            {
                if (slot.secondKeyboard)
                {
                    cmd.move = (k.rightArrowKey.isPressed ? 1 : 0) - (k.leftArrowKey.isPressed ? 1 : 0);
                    cmd.jump = k.rightShiftKey.wasPressedThisFrame;
                    cmd.fire = k.rightCtrlKey.isPressed || k.enterKey.isPressed;
                    cmd.special = k.downArrowKey.wasPressedThisFrame;
                }
                else
                {
                    cmd.move = (k.dKey.isPressed ? 1 : 0) - ((k.aKey.isPressed || k.qKey.isPressed) ? 1 : 0);
                    cmd.jump = k.spaceKey.wasPressedThisFrame;
                    cmd.fire = k.jKey.isPressed;
                    cmd.special = k.kKey.wasPressedThisFrame;
                }
            }
            if (slot.Connected)
            {
                float x = slot.pad.leftStick.x.ReadValue() + slot.pad.dpad.x.ReadValue();
                if (Mathf.Abs(x) > .18f) cmd.move = Mathf.Clamp(x, -1, 1);
                cmd.jump |= slot.pad.buttonSouth.wasPressedThisFrame;
                cmd.fire |= slot.pad.buttonWest.isPressed || slot.pad.rightTrigger.isPressed;
                cmd.special |= slot.pad.buttonNorth.wasPressedThisFrame || slot.pad.rightShoulder.wasPressedThisFrame;
            }
            return cmd;
        }
    }
}
