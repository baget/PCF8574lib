using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;
using nanoFramework.Hardware.Esp32;
using iot.devices;

namespace RollingLights
{
    public class Program
    {
        
        /// <summary>
        /// PCF8574 I2C Address on the board
        /// </summary>
        const int I2C_DEVICE_ADDR = 0x21;

        /// <summary>
        /// PCF8574 Pins operations
        /// </summary>
        public static class MyBoardsPCFPins
        {
            public const int S1_BUTTON = 1;
            public const int S2_BUTTON = 2;
            public const int S3_BUTTON = 3;
            public const int YELLOW_LED = 6;
            public const int RED_LED = 7;
        }

        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");

            ConfigEsp32Ttgo();

            var pCF8574 = new PCF8574(I2C_DEVICE_ADDR, 2);

            pCF8574.SetPinMode(MyBoardsPCFPins.YELLOW_LED, PinMode.Input);
            pCF8574.SetPinMode(MyBoardsPCFPins.RED_LED, PinMode.Output);
            pCF8574.Begin();

            RollingLights(pCF8574);

        }


        private static void RollingLights(PCF8574 pCF8574)
        {
            var delayValue = TimeSpan.FromSeconds(1);
            while (true)
            {
                pCF8574.Write(MyBoardsPCFPins.YELLOW_LED, PinValue.High);
                pCF8574.Write(MyBoardsPCFPins.RED_LED, PinValue.Low);
                Thread.Sleep(delayValue);

                pCF8574.Write(MyBoardsPCFPins.YELLOW_LED, PinValue.Low);
                pCF8574.Write(MyBoardsPCFPins.RED_LED, PinValue.High);
                Thread.Sleep(delayValue);
            }
        }

        private static void ConfigEsp32Ttgo()
        {
            // Map I2C Bus #2 to SCL=IO22, SDA=IO21 in ESP32 TTGO
            Configuration.SetPinFunction(Gpio.IO21, DeviceFunction.I2C2_DATA);
            Configuration.SetPinFunction(Gpio.IO22, DeviceFunction.I2C2_CLOCK);
        }
    }
}
