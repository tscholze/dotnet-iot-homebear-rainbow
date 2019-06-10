using HomeBear.Rainbow.Utils;
using Microsoft.IoT.Lightning.Providers;
using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;

namespace HomeBear.Rainbow.Controller
{
    /// <summary>
    /// BMP280 controller.
    /// 
    /// Links:
    ///     - Pimoroni original code:
    ///         https://github.com/pimoroni/rainbow-hat/blob/master/library/rainbowhat/bmp280.py
    /// 
    ///     - Microsoft Sample code:
    ///        https://github.com/ms-iot/adafruitsample/blob/master/Lesson_203/FullSolution/BMP280.cs
    ///        
    ///     - Datasheet:
    ///         http://www.adafruit.com/datasheets/BST-BMP280-DS001-11.pdf
    /// </summary>
    class BMP280 : IDisposable
    {
        #region Private constants

        /// <summary>
        /// I2C adress of the BMP280.
        /// </summary>
        private static readonly byte BMP280_ADDRESS = 0x77;

        /// <summary>
        /// I2C signature of the BMP280.
        /// Used to verify the address.
        /// </summary>
        private static readonly byte BMP280_SIGNATURE = 0x58;

        /// <summary>
        /// Chip ID of the BMP280.
        /// </summary>
        private static readonly byte REGISTER_CHIPID = 0xD0;

        /// <summary>
        /// Control register of the BMP280.
        /// </summary>
        private static readonly byte REGISTER_CONTROL = 0xF4;

        /// <summary>
        /// I2C register for the first digit of the temperatur measurement.
        /// </summary>
        private static readonly byte REGISTER_DIGIT_TEMPERATUR_1 = 0x88;

        /// <summary>
        /// I2C register for the second digit of the temperatur measurement.
        /// </summary>
        private static readonly byte REGISTER_DIGIT_TEMPERATUR_2 = 0x8A;

        /// <summary>
        /// I2C register for the third digit of the temperatur measurement.
        /// </summary>
        private static readonly byte REGISTER_DIGIT_TEMPERATUR_3 = 0x8C;

        /// <summary>
        /// Gets the most significant bit of the temperatur measurement.
        /// </summary>
        private static readonly byte REGISTER_MSB_TEMPERATUR = 0xFA;

        /// <summary>
        /// Gets the least significant bit of the temperatur measurement.
        /// </summary>
        private static readonly byte REGISTER_LSB_TEMPERATUR = 0xFB;

        /// <summary>
        /// Gets the bits between msb and lsb of the temperatur measurement.
        /// </summary>
        private static readonly byte REGISTER_XLSB_TEMPERATUR = 0xFC;

        /// <summary>
        /// I2C register for the first digit of the pressur measurement.
        /// </summary>
        private static readonly byte REGISTER_DIGIT_PRESSURE_1 = 0x8E;

        /// <summary>
        /// I2C register for the second digit of the pressure measurement.
        /// </summary>
        private static readonly byte REGISTER_DIGIT_PRESSURE_2 = 0x90;

        /// <summary>
        /// I2C register for the third digit of the pressure measurement.
        /// </summary>
        private static readonly byte REGISTER_DIGIT_PRESSURE_3 = 0x92;

        /// <summary>
        /// I2C register for the fourth digit of the pressur measurement.
        /// </summary>
        private static readonly byte REGISTER_DIGIT_PRESSURE_4 = 0x94;

        /// <summary>
        /// I2C register for the fith digit of the pressur measurement.
        /// </summary>
        private static readonly byte REGISTER_DIGIT_PRESSURE_5 = 0x96;

        /// <summary>
        /// I2C register for the sixth digit of the pressur measurement.
        /// </summary>
        private static readonly byte REGISTER_DIGIT_PRESSURE_6 = 0x98;

        /// <summary>
        /// I2C register for the seventh digit of the pressur measurement.
        /// </summary>
        private static readonly byte REGISTER_DIGIT_PRESSURE_7 = 0x9A;

        /// <summary>
        /// I2C register for the eigths digit of the pressur measurement.
        /// </summary>
        private static readonly byte REGISTER_DIGIT_PRESSURE_8 = 0x9C;

        /// <summary>
        /// I2C register for the ninth digit of the pressur measurement.
        /// </summary>
        private static readonly byte REGISTER_DIGIT_PRESSURE_9 = 0x9E;

        /// <summary>
        /// Gets the most significant bit of the preasure measurement.
        /// </summary>
        private static readonly byte REGISTER_MSB_PRESSURE = 0xF7;

        /// <summary>
        /// Gets the least significant bit of the pressure measurement.
        /// </summary>
        private static readonly byte REGISTER_LSB_PRESSURE = 0XF8;

        /// <summary>
        /// Gets the bits between msb and lsb of the pressure measurement.
        /// </summary>
        private static readonly byte REGISTER_XLSB_PRESSURE = 0xF9;

        #endregion

        #region Private properties 

        /// <summary>
        /// System's default GPIO controller.
        /// </summary>
        private readonly GpioController gpioController = GpioController.GetDefault();

        /// <summary>
        /// Underlying BMP280 device.
        /// </summary>
        private I2cDevice bmp280;

        /// <summary>
        /// Sensor calibration information of the BMP280 device.
        /// </summary>
        private BMP280CalibrationInformation calibrationInformation;

        /// <summary>
        /// Determines if the BMP280 has been already initialized.
        /// </summary>
        private bool isInitialized = false;

        #endregion

        #region Public helpers

        /// <summary>
        /// Initializes the BMP280 async.
        /// 
        /// Caution:
        ///     This is required before accessing other
        ///     methods in this class.
        /// </summary>
        /// <param name="i2cController">Underlying I2C controller.</param>
        /// <returns>Task.</returns>
        public async Task InitializeAsync(I2cController i2cController)
        {
            Logger.Log(this, "InitializeAsync");

            // Setup device.
            bmp280 = i2cController.GetDevice(new I2cConnectionSettings(BMP280_ADDRESS) { BusSpeed = I2cBusSpeed.FastMode });

            // Ensure device has been found.
            if(bmp280 == null)
            {
                throw new OperationCanceledException("BPM280 device not found.");
            }

            // Setup device.
            byte[] writeBuffer = new byte[] { REGISTER_CHIPID };
            byte[] readBuffer = new byte[] { 0xFF };

            // Read signature.
            bmp280.WriteRead(writeBuffer, readBuffer);
            byte signature = readBuffer[0];

            // Ensure valid signature has been found.
            if(signature != BMP280_SIGNATURE)
            {
                Logger.Log(this, $"Found signature {signature.ToString()} does not match required signature: '{BMP280_SIGNATURE.ToString()}'");
                return;
            }

            // Set state as initialized.
            isInitialized = true;

            // Read calibration information from i2c device.
            calibrationInformation = await ReadCalibrationInformation();

            // Write /enable(?) control register
            await WriteControlRegister();
        }
        
        /// <summary>
        /// Reads temperatur from BMP280.
        /// </summary>
        /// <returns>Read temperature value.</returns>
        public double ReadTemperature(bool asFinite = false)
        {
            // Ensure BMP280 has been initialzed.
            if(!isInitialized)
            {
                Logger.Log(this, "BMP has not been initialized, yet. Call `InitializeAsync()` at very first operation.");
                return 0;
            }

            // Get byte values from I2C device.
            byte msb = ReadByte(REGISTER_MSB_TEMPERATUR);
            byte lsb = ReadByte(REGISTER_LSB_TEMPERATUR);
            byte xlsb = ReadByte(REGISTER_XLSB_TEMPERATUR);

            // Combine values into raw temperatur value.
            int rawValue = (msb << 12) + (lsb << 4) + (xlsb >> 4);

            // Transform it into a humanreadble value.
            // It uses the compensation formula in the BMP280 datasheet.
            double part1 = ((rawValue / 16384.0) - (calibrationInformation.Temperatur1/ 1024.0)) * calibrationInformation.Temperatur2;
            double part2 = (rawValue / 131072.0 - calibrationInformation.Temperatur1 / 8192.0) * (rawValue / 131072.0 - calibrationInformation.Temperatur2 / 8192.0) * calibrationInformation.Temperatur3;

            // Set global values.
            var finiteTemperature = part1 + part2;

            // Check which value representation is required.
            if (asFinite)
            {
                return finiteTemperature;
            }

            // Return combined / transformed value.
            return finiteTemperature / 5120.0;
        }

        public double ReadPressure()
        {
            // Ensure BMP280 has been initialzed.
            if (!isInitialized)
            {
                Logger.Log(this, "BMP has not been initialized, yet. Call `InitializeAsync()` at very first operation.");
                return 0;
            }

            // Current temperature is required for pressure meassurement.
            var temperature = ReadTemperature(true);

            // Get byte values from I2C device.
            byte msb = ReadByte(REGISTER_MSB_PRESSURE);
            byte lsb = ReadByte(REGISTER_LSB_PRESSURE);
            byte xlsb = ReadByte(REGISTER_XLSB_PRESSURE);

            // Combine values into raw temperatur value.
            int rawValue = (msb << 12) + (lsb << 4) + (xlsb >> 4);

            // Transform it into a humanreadble value.
            // It uses the compensation formula in the BMP280 datasheet.
            long part1 = Convert.ToInt64(temperature) - 128000;
            long part2 = part1 * part1 * calibrationInformation.Pressure6;
            part2 += ((part1 * calibrationInformation.Pressure5) << 17);
            part2 += (long)calibrationInformation.Pressure4 << 35;
            part1 = ((part1 * part1 * calibrationInformation.Pressure3) >> 8) + ((part1 * calibrationInformation.Pressure2) << 12);
            part1 = ((((long)1 << 47) + part1) * calibrationInformation.Pressure1) >> 33;

            // Ensure valid information.
            if (part1 == 0)
            {
                Logger.Log(this, "Pressure value would be invalid. Returning 0");
                return 0; 
            }

            // Perform calibration operations as per datasheet: 
            long pressure = 1048576 - rawValue;
            pressure = (((pressure << 31) - part2) * 3125) / part1;
            part1 = (calibrationInformation.Pressure9 * (pressure >> 13) * (pressure >> 13)) >> 25;
            part2 = (calibrationInformation.Pressure8 * pressure) >> 19;
            pressure = ((pressure + part1 + part2) >> 8) + ((long)calibrationInformation.Pressure7 << 4);

            // TRansform pressure to hPa
            pressure = pressure / 256 / 1000;
            return pressure;
        }

        #endregion

        #region IDisposeable

        /// <summary>
        /// Will dispose all related attributes.
        /// </summary>
        public void Dispose()
        {
            bmp280.Dispose();
            isInitialized = false;
        }

        #endregion

        #region Private Helpers 

        /// <summary>
        /// Reads calibration information from BMP280.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task<BMP280CalibrationInformation> ReadCalibrationInformation()
        {
            // Read information from I2C device.
            var information = new BMP280CalibrationInformation
            {
                // Read temperatur calibration information
                Temperatur1 = ReadUIntFromLittleEndian(REGISTER_DIGIT_TEMPERATUR_1),
                Temperatur2 = (short)ReadUIntFromLittleEndian(REGISTER_DIGIT_TEMPERATUR_2),
                Temperatur3 = (short)ReadUIntFromLittleEndian(REGISTER_DIGIT_TEMPERATUR_3),
                // Read pressure calibration information.
                Pressure1 = ReadUIntFromLittleEndian(REGISTER_DIGIT_PRESSURE_1),
                Pressure2 = (short)ReadUIntFromLittleEndian(REGISTER_DIGIT_PRESSURE_2),
                Pressure3 = (short)ReadUIntFromLittleEndian(REGISTER_DIGIT_PRESSURE_3),
                Pressure4 = (short)ReadUIntFromLittleEndian(REGISTER_DIGIT_PRESSURE_4),
                Pressure5 = (short)ReadUIntFromLittleEndian(REGISTER_DIGIT_PRESSURE_5),
                Pressure6 = (short)ReadUIntFromLittleEndian(REGISTER_DIGIT_PRESSURE_6),
                Pressure7 = (short)ReadUIntFromLittleEndian(REGISTER_DIGIT_PRESSURE_7),
                Pressure8 = (short)ReadUIntFromLittleEndian(REGISTER_DIGIT_PRESSURE_8),
                Pressure9 = (short)ReadUIntFromLittleEndian(REGISTER_DIGIT_PRESSURE_9)
            };

            // Ensure that every request has been read and processed.
            await Task.Delay(1);

            // Return the information.
            return information;
        }

        /// <summary>
        /// Reads a UInt in Little Endian format from given register.
        /// </summary>
        /// <param name="register">Register to read from.</param>
        /// <returns>Read UInt value.</returns>
        private ushort ReadUIntFromLittleEndian(byte register)
        {
            // Setup values and buffers to read from register.
            byte[] writeBuffer = new byte[] { register };
            byte[] readBuffer = new byte[] { 0x00, 0x00 };

            // Write / Read from register
            bmp280.WriteRead(writeBuffer, readBuffer);

            // Calculate resulting value.
            return (ushort)((readBuffer[1] << 8) + readBuffer[0]);
        }

        /// <summary>
        /// Reads a byte from given register.
        /// </summary>
        /// <param name="register">Register to read from.</param>
        /// <returns>Read byte value.</returns>
        private byte ReadByte(byte register)
        {
            // Setup values and buffers to read from register.
            byte[] writeBuffer = new byte[] { register };
            byte[] readBuffer = new byte[] { 0x00 };

            // Write / Read from register.
            bmp280.WriteRead(writeBuffer, readBuffer);

            // Get the first byte of the buffer.
            return readBuffer[0];
        }

        /// <summary>
        /// Writes into the control register.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task WriteControlRegister()
        {
            // Write into control register.
            byte[] writeBuffer = new byte[] { REGISTER_CONTROL, 0x3F };
            bmp280.Write(writeBuffer);

            // Ensure that every request has been read and processed.
            await Task.Delay(1);
        }

        #endregion
    }
}
