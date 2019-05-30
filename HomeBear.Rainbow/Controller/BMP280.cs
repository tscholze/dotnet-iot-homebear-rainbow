using HomeBear.Rainbow.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;

namespace HomeBear.Rainbow.Controller
{
    // https://github.com/pimoroni/rainbow-hat/blob/master/library/rainbowhat/bmp280.py
    // https://github.com/ms-iot/adafruitsample/blob/master/Lesson_203/FullSolution/BMP280.cs
    class BMP280 : IDisposable
    {
        #region Private constants

        /// <summary>
        /// Name of the I2C controller.
        /// </summary>
        private static readonly string I2C_CONTROLLER_NAME = "I2C1";

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
        /// Gets thebits between msb and lsb of the temperatur measurement.
        /// </summary>
        private static readonly byte REGISTER_XLSB_TEMPERATUR = 0xFC;

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

        public async Task InitializeAsync()
        {
            // Setup settings.
            I2cConnectionSettings settings = new I2cConnectionSettings(BMP280_ADDRESS)
            {
                BusSpeed = I2cBusSpeed.FastMode
            };

            // Find i2c device.
            string deviceSelector = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(deviceSelector);
            bmp280 = await I2cDevice.FromIdAsync(devices[0].Id, settings);

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

            var t = ReadTemperatur();
            Logger.Log(this, $"TEMP: {t}");

        }
        
        public double ReadTemperatur()
        {
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

            // Return combined / transformed value.
            return (part1 + part2) / 5120.0;
        }

        #endregion

        #region IDisposeable

        public void Dispose()
        {

        }

        #endregion

        #region Private Helpers 

        private async Task<BMP280CalibrationInformation> ReadCalibrationInformation()
        {
            // Read information from I2C device.
            var information = new BMP280CalibrationInformation
            {
                // Read temperatur calibration information
                Temperatur1 = ReadUIntFromLittleEndian(REGISTER_DIGIT_TEMPERATUR_1),
                Temperatur2 = (short)ReadUIntFromLittleEndian(REGISTER_DIGIT_TEMPERATUR_2),
                Temperatur3 = (short)ReadUIntFromLittleEndian(REGISTER_DIGIT_TEMPERATUR_3)
            };

            // Ensure that every request has been read and processed.
            await Task.Delay(1);

            // Return the information.
            return information;
        }

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

        private async Task WriteControlRegister()
        {
            byte[] writeBuffer = new byte[] { REGISTER_CONTROL, 0x3F };
            bmp280.Write(writeBuffer);

            // Ensure that every request has been read and processed.
            await Task.Delay(1);
        }

        #endregion
    }
}
