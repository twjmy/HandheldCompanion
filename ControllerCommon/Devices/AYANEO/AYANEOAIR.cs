using ControllerCommon.Inputs;
using System.Collections.Generic;
using System.Numerics;
using WindowsInput.Events;

namespace ControllerCommon.Devices
{
    public class AYANEOAIR : IDevice
    {
        public AYANEOAIR() : base()
        {
            // device specific settings
            this.ProductIllustration = "device_aya_air";
            this.ProductModel = "AYANEOAir";

            // https://www.amd.com/en/products/apu/amd-ryzen-5-5560u
            this.nTDP = new double[] { 12, 12, 15 };
            this.cTDP = new double[] { 3, 15 };
            this.GfxClock = new double[] { 100, 1600 };

            this.AngularVelocityAxisSwap = new()
            {
                { 'X', 'X' },
                { 'Y', 'Z' },
                { 'Z', 'Y' },
            };

            this.AccelerationAxis = new Vector3(-1.0f, 1.0f, -1.0f);
            this.AccelerationAxisSwap = new()
            {
                { 'X', 'X' },
                { 'Y', 'Z' },
                { 'Z', 'Y' },
            };

            // device specific capacities
            this.Capacities = DeviceCapacities.FanControl;

            this.ECDetails = new ECDetails()
            {
                AddressControl = 0x44A,
                AddressDuty = 0x44B,
                AddressRegistry = 0x4E,
                AddressData = 0x4F,
                ValueMin = 0,
                ValueMax = 100
            };

            OEMChords.Add(new DeviceChord("Custom Key Top Right",
                new List<KeyCode>() { KeyCode.RControlKey, KeyCode.LWin, KeyCode.F10 },
                new List<KeyCode>() { KeyCode.F10, KeyCode.LWin, KeyCode.RControlKey },
                false, ButtonFlags.OEM3
                ));

            OEMChords.Add(new DeviceChord("Custom Key Top Left",
                new List<KeyCode>() { KeyCode.RControlKey, KeyCode.LWin, KeyCode.F11 },
                new List<KeyCode>() { KeyCode.F11, KeyCode.LWin, KeyCode.RControlKey },
                false, ButtonFlags.OEM4
                ));

            OEMChords.Add(new DeviceChord("Custom Key Big",
                new List<KeyCode>() { KeyCode.RControlKey, KeyCode.LWin, KeyCode.F12 },
                new List<KeyCode>() { KeyCode.F12, KeyCode.LWin, KeyCode.RControlKey },
                false, ButtonFlags.OEM1
                ));

            OEMChords.Add(new DeviceChord("Custom Key Small",
                new List<KeyCode>() { KeyCode.LWin, KeyCode.D },
                new List<KeyCode>() { KeyCode.LWin, KeyCode.D },
                false, ButtonFlags.OEM2
                ));
        }
    }
}
