using System;
using System.Diagnostics;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Sensors.Power;
using System.Threading.Tasks;
using System.Timers;
using Meadow.Foundation.FeatherWings;
using Meadow.Foundation.Graphics;
using Meadow.Hardware;
using Meadow.Units;

namespace InaWiFi
{
    public class MeadowApp : App<F7FeatherV2>
    {
        private Ina219 powerMonitor;
        private OLED128x32Wing oledWing;
        private MicroGraphics graphics;
        Stopwatch wifiStopwatch = new Stopwatch();

        public override Task Initialize()
        {
            Resolver.Log.Info("Initialize");
            wifiStopwatch.Start();

            var i2cBus = Device.CreateI2cBus(I2cBusSpeed.Fast);

            oledWing = new OLED128x32Wing(i2cBus, Device.Pins.D11, Device.Pins.D10, Device.Pins.D09);
            oledWing.Display.Clear(true);
            graphics = new MicroGraphics(oledWing.Display.PixelBuffer, true);

            graphics.SetCursorPosition(0,0);
            graphics.Write("Init");
            oledWing.Display.Show();

            var wifi = Device.NetworkAdapters.Primary<IWiFiNetworkAdapter>();
            wifi.NetworkConnected += (networkAdapter, networkConnectionEventArgs) => {
                Resolver.Log.Info("Connected");
                wifiStopwatch.Stop();
                graphics.ClearLine(0);
                graphics.SetCursorPosition(0, 0);
                graphics.Write($"Wifi: {networkAdapter.IpAddress}");
                graphics.ClearLine(1);
                graphics.SetCursorPosition(0, 1);
                graphics.Write($"Time: {wifiStopwatch.Elapsed:g}");
                oledWing.Display.Show();
            };

            powerMonitor = new Ina219(i2cBus);
            powerMonitor.Configure(Ina219.BusVoltageRange.Range_32V, 0.5.Amps(), Ina219.ADCModes.ADCMode_16xAvg_8512us);
            powerMonitor.StartUpdating(TimeSpan.FromSeconds(2));
            powerMonitor.VoltageUpdated += PowerMonitorOnVoltageUpdated;
            powerMonitor.CurrentUpdated += PowerMonitorOnCurrentUpdated;

            return base.Initialize();
        }

        private void PowerMonitorOnVoltageUpdated(object sender, IChangeResult<Voltage> e)
        {
            graphics.SetCursorPosition(0, 2);
            graphics.ClearLine(2);
            graphics.Write($"V: {e.New.Volts:F2} V");
            oledWing.Display.Show();
        }

        private void PowerMonitorOnCurrentUpdated(object sender, IChangeResult<Current> e)
        {
            graphics.SetCursorPosition(0, 3);
            graphics.ClearLine(3);
            graphics.Write($"I: {e.New.Milliamps:F3} mA");
            oledWing.Display.Show();
        }

        public override Task Run()
        {
            Resolver.Log.Info("Run");
            return base.Run();
        }

    }
}