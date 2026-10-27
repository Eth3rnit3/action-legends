using UnityEngine;
using UnityEngine.InputSystem;

namespace Paname
{
    // Only the input adapter knows about hardware. Simulation consumes PlayerCommand.
    public struct PlayerCommand
    {
        public float move;
        public bool jump, fire, special, aimUp, down, grenade;
    }

    public sealed class LocalSlot
    {
        public bool joined;
        public int hero;
        public Gamepad pad;
        public bool Connected => pad != null && pad.added;
        public string DeviceName => Connected ? pad.displayName : "Clavier";
    }

    public static class LocalControls
    {
        public static PlayerCommand Read(LocalSlot slot, int index)
        {
            var cmd = new PlayerCommand();
            var k = Keyboard.current;
            if (k != null && index == 0)
            {
                cmd.move = (k.dKey.isPressed ? 1 : 0) - ((k.aKey.isPressed || k.qKey.isPressed) ? 1 : 0);
                cmd.jump = k.spaceKey.wasPressedThisFrame;
                cmd.fire = k.jKey.isPressed;
                cmd.special = k.kKey.wasPressedThisFrame;
                cmd.aimUp = k.wKey.isPressed || k.zKey.isPressed || k.upArrowKey.isPressed;
                cmd.down = k.sKey.isPressed || k.downArrowKey.isPressed;
                cmd.grenade = k.lKey.wasPressedThisFrame;
            }
            if (slot.Connected)
            {
                float x = slot.pad.leftStick.x.ReadValue() + slot.pad.dpad.x.ReadValue();
                if (Mathf.Abs(x) > .18f) cmd.move = Mathf.Clamp(x, -1, 1);
                cmd.aimUp |= slot.pad.leftStick.y.ReadValue() > .45f || slot.pad.dpad.up.isPressed;
                cmd.down |= slot.pad.leftStick.y.ReadValue() < -.45f || slot.pad.dpad.down.isPressed;
                cmd.grenade |= slot.pad.buttonEast.wasPressedThisFrame;
                cmd.jump |= slot.pad.buttonSouth.wasPressedThisFrame;
                cmd.fire |= slot.pad.buttonWest.isPressed || slot.pad.rightTrigger.isPressed;
                cmd.special |= slot.pad.buttonNorth.wasPressedThisFrame || slot.pad.rightShoulder.wasPressedThisFrame;
            }
            return cmd;
        }
    }
}
