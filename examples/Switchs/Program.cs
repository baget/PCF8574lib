using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;
using nanoFramework.Hardware.Esp32;
using iot.devices;

namespace Switchs
{
    public class Program
    {
        public static class MyBoardsPCFPins
        {
            public const int S1_BUTTON = 1;
            public const int S2_BUTTON = 2;
            public const int S3_BUTTON = 3;
            public const int YELLOW_LED = 6;
            public const int RED_LED = 7;
        }

        const int I2C_DEVICE_ADDR = 0x21;
        public static void Main()
        {
            ConfigEsp32Ttgo();

            var pCF8574 = new PCF8574(I2C_DEVICE_ADDR, 2);
            pCF8574.SetPinMode(MyBoardsPCFPins.S1_BUTTON, PinMode.Input);
            pCF8574.SetPinMode(MyBoardsPCFPins.S2_BUTTON, PinMode.Input);
            pCF8574.SetPinMode(MyBoardsPCFPins.S3_BUTTON, PinMode.Input);
            pCF8574.SetPinMode(MyBoardsPCFPins.YELLOW_LED, PinMode.Input);
            pCF8574.SetPinMode(MyBoardsPCFPins.RED_LED, PinMode.Output);

            pCF8574.Begin();

            PinValue[] swVal = null;
            var delayValue = TimeSpan.FromMilliseconds(50);
            while (true)
            {
                bool keyPress = false;
                while (!keyPress)
                {
                    swVal = pCF8574.ReadAll();

                    for (int i = MyBoardsPCFPins.S1_BUTTON; i != MyBoardsPCFPins.S3_BUTTON; i++)
                    {
                        if (swVal[i] == PinValue.High)
                        {
                            Debug.WriteLine($"S{i} was pressed");
                            keyPress = true;

                            // Debounce
                            while (pCF8574.Read(i) == PinValue.High)
                            {
                                Thread.Sleep(delayValue);
                            }
                        }
                    }

                    Thread.Sleep(delayValue);
                }

                if (swVal[MyBoardsPCFPins.S1_BUTTON] == PinValue.High)
                {
                    pCF8574.Write(MyBoardsPCFPins.YELLOW_LED, PinValue.High);
                    pCF8574.Write(MyBoardsPCFPins.RED_LED, PinValue.Low);
                }
                else if (swVal[MyBoardsPCFPins.S2_BUTTON] == PinValue.High)
                {
                    pCF8574.Write(MyBoardsPCFPins.YELLOW_LED, PinValue.High);
                    pCF8574.Write(MyBoardsPCFPins.RED_LED, PinValue.High);
                }
                else if (swVal[MyBoardsPCFPins.S3_BUTTON] == PinValue.High)
                {
                    pCF8574.Write(MyBoardsPCFPins.YELLOW_LED, PinValue.Low);
                    pCF8574.Write(MyBoardsPCFPins.RED_LED, PinValue.High);
                }



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
