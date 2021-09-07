using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Device.I2c;

namespace iot.devices
{
    /// <summary>
    /// PCF8574 I2C Device class
    /// 
    /// based on https://github.com/xreef/PCF8574_library
    /// </summary>
    public class PCF8574 : IDisposable
    {
        #region Exceptions
        public class GeneralException : ApplicationException
        {
            public GeneralException() : base()
            {
            }

            public GeneralException(string msg) : base(msg)
            {
            }

        }

        public class NoPinsSetException : GeneralException
        {
            public NoPinsSetException() : base("No Pin are set for read and/or write")
            {

            }
        }

        public class InvalidPinNumberException : GeneralException
        {
            public InvalidPinNumberException() : base("Invalid Pin Number")
            {

            }
            public InvalidPinNumberException(int pinNumber) : base($"Invalid Pin Number - pinNumber = {pinNumber}")
            {

            }
        }
        #endregion

        private I2cDevice _i2c;
        private bool disposedValue;

        private byte _writeMode = 0b00000000;
        private byte _readMode = 0b00000000;
        private double _lastReadMillis = 0;

        private byte _writeByteBuffered = 0b00000000;


        public const int NumberOfPins = 8;

        /// <summary>
        /// I2C Device Address
        /// </summary>
        public int Address { get; private set; }


        public int I2CBus { get; private set; }

        /// <summary>
        /// Create PCF8574 object
        /// </summary>
        /// <param name="address">PCF8574 I2C device acess</param>
        /// <param name="bus">I2C Bus number</param>
        public PCF8574(int address, byte bus = 2)
        {
            Address = address;
            I2CBus = bus;
        }

        /// <summary>
        /// setup of PCF8574 configuration
        /// </summary>
        public void Begin()
        {
            _i2c = I2cDevice.Create(new I2cConnectionSettings(I2CBus, Address, I2cBusSpeed.StandardMode));

            if (_writeMode != 0 || _readMode != 0)
            {
                byte usedPins = (byte)(_writeMode | _readMode);
                usedPins = (byte)(~usedPins);

                Debug.WriteLine($"Set write Mode = 0x{usedPins:X}");

                var res = _i2c.WriteByte(usedPins);

                Debug.WriteLine($"I2C WriteByte Result = {res}");

                _lastReadMillis = GetCurrentMS();
            }
            else
            {
                Debug.WriteLine("No pins are set");

                throw new NoPinsSetException();
            }

        }

        /// <summary>
        /// Set Pin Mode of PCF8574
        /// </summary>
        /// <param name="pinNumber">Pin number to set (0-7)</param>
        /// <param name="mode">Pin Mode to set (Input or Output)</param>
        public void SetPinMode(int pinNumber, PinMode mode)
        {
            Debug.WriteLine($"Set Pin {pinNumber} as {mode}");
            Debug.WriteLine($"BEFORE - WriteMode = 0x{_writeMode:X} ReadMode = 0x{_readMode:X}");

            CheckForValidPinNumber(pinNumber);

            if (mode == PinMode.Output)
            {
                _writeMode |= BitPosition(pinNumber);
                _readMode &= (byte)(~BitPosition(pinNumber));

            }
            else if (mode == PinMode.Input)
            {
                _writeMode &= (byte)(~BitPosition(pinNumber));
                _readMode |= BitPosition(pinNumber);

            }
            else
            {
                Debug.WriteLine("Mode not supported by PCF8574");
            }

            Debug.WriteLine($"AFTER - WriteMode = 0x{_writeMode:X} ReadMode = 0x{_readMode:X}");

        }

        private static void CheckForValidPinNumber(int pinNumber)
        {
            if (pinNumber < 0 || pinNumber > 7)
            {
                throw new InvalidPinNumberException(pinNumber);
            }
        }

        /// <summary>
        /// Write bit value to a given pin
        /// </summary>
        /// <param name="pinNumber">Pin value to write value</param>
        /// <param name="value">value to set</param>
        public void Write(int pinNumber, PinValue value)
        {
            Debug.WriteLine($"Write Value {value} for Pin {pinNumber}");

            CheckForValidPinNumber(pinNumber);

            if (value == PinValue.High)
            {
                _writeByteBuffered |= BitPosition(pinNumber);
            }
            else
            {
                _writeByteBuffered &= (byte)(~BitPosition(pinNumber));
            }

            Debug.WriteLine($"Write Data 0x{_writeByteBuffered:X} for Pin {pinNumber} (Bit Positon 0x{BitPosition(pinNumber):X})");
            var res = _i2c.WriteByte(_writeByteBuffered);

            Debug.WriteLine($"I2C WriteByte Result = {res}");
        }

        public PinValue Read(int pinNumber)
        {
            PinValue value = PinValue.Low;

#if DEBUG_LOG_READ
            Debug.WriteLine($"Reading value from Pin {pinNumber}");
#endif

            CheckForValidPinNumber(pinNumber);

            _lastReadMillis = GetCurrentMS();

            var data = _i2c.ReadByte();

            if ((data & _readMode) != 0)
            {
#if DEBUG_LOG_READ
                Debug.WriteLine($"Input Data 0x{data:X}");
#endif
                value = (BitPosition(pinNumber) & data) != 0 ? PinValue.High : PinValue.Low;
            }

#if DEBUG_LOG_READ
            Debug.WriteLine($"Value = {value}");
#endif

            return value;
        }

        public PinValue[] ReadAll()
        {
            var value = new PinValue[NumberOfPins];

            var data = ReadAllAsByte();

            for (int i = 0; i < NumberOfPins; i++)
            {
                value[i] = (BitPosition(i) & data) != 0 ? PinValue.High : PinValue.Low;
#if DEBUG_LOG_READ
                Debug.WriteLine($"Value[{i}] = {value[i]}");
#endif
            }

            return value;
        }

        public byte ReadAllAsByte()
        {
            byte retVal = 0;


#if DEBUG_LOG_READ
            Debug.WriteLine($"Reading value from all pins");
#endif

            _lastReadMillis = GetCurrentMS();

            var data = _i2c.ReadByte();

            retVal = (byte)(data & _readMode);

#if DEBUG_LOG_READ
                Debug.WriteLine($"Input Data 0x{data:X}");                
#endif

            return retVal;
        }



        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _i2c.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get number with given bit set
        /// </summary>
        /// <param name="bit">given bit to set</param>
        /// <returns>number with given bit set to one</returns>
        private static byte BitPosition(int bit)
        {
            return (byte)(1 << bit);
        }

        /// <summary>
        /// Current Ticks Time in Milliseconds
        /// </summary>
        /// <returns>Current Ticks Time in Milliseconds</returns>
        private static double GetCurrentMS()
        {
            return TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalMilliseconds;
        }

    }
}
