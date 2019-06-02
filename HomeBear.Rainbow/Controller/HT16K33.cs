using HomeBear.Rainbow.Utils;
using Microsoft.IoT.Lightning.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace HomeBear.Rainbow.Controller
{
    // https://github.com/pimoroni/rainbow-hat/blob/master/library/rainbowhat/alphanum4.py
    // https://github.com/pimoroni/rainbow-hat/blob/master/library/rainbowhat/HT16K33.py
    class HT16K33 : IDisposable
    {
        #region Private constants

        /// <summary>
        /// Maps chars to binary repräsentation.
        /// </summary>
        private static Dictionary<char, int> DIGIT_DICTIONARY = new Dictionary<char, int>{
            { ' ', 0b0000000000000000 },
            { '!', 0b0000000000000110 },
            { '"', 0b0000001000100000 },
            { '#', 0b0001001011001110 },
            { '$', 0b0001001011101101 },
            { '%', 0b0000110000100100 },
            { '&', 0b0010001101011101 },
            { '\'', 0b0000010000000000 },
            { '(', 0b0010010000000000 },
            { ')', 0b0000100100000000 },
            { '*', 0b0011111111000000 },
            { '+', 0b0001001011000000 },
            { ',', 0b0000100000000000 },
            { '-', 0b0000000011000000 },
            { '.', 0b0000000000000000 },
            { '/', 0b0000110000000000 },
            { '0', 0b0000110000111111 },
            { '1', 0b0000000000000110 },
            { '2', 0b0000000011011011 },
            { '3', 0b0000000010001111 },
            { '4', 0b0000000011100110 },
            { '5', 0b0010000001101001 },
            { '6', 0b0000000011111101 },
            { '7', 0b0000000000000111 },
            { '8', 0b0000000011111111 },
            { '9', 0b0000000011101111 },
            { ':', 0b0001001000000000 },
            { ';', 0b0000101000000000 },
            { '<', 0b0010010000000000 },
            { '=', 0b0000000011001000 },
            { '>', 0b0000100100000000 },
            { '?', 0b0001000010000011 },
            { '@', 0b0000001010111011 },
            { 'A', 0b0000000011110111 },
            { 'B', 0b0001001010001111 },
            { 'C', 0b0000000000111001 },
            { 'D', 0b0001001000001111 },
            { 'E', 0b0000000011111001 },
            { 'F', 0b0000000001110001 },
            { 'G', 0b0000000010111101 },
            { 'H', 0b0000000011110110 },
            { 'I', 0b0001001000000000 },
            { 'J', 0b0000000000011110 },
            { 'K', 0b0010010001110000 },
            { 'L', 0b0000000000111000 },
            { 'M', 0b0000010100110110 },
            { 'N', 0b0010000100110110 },
            { 'O', 0b0000000000111111 },
            { 'P', 0b0000000011110011 },
            { 'Q', 0b0010000000111111 },
            { 'R', 0b0010000011110011 },
            { 'S', 0b0000000011101101 },
            { 'T', 0b0001001000000001 },
            { 'U', 0b0000000000111110 },
            { 'V', 0b0000110000110000 },
            { 'W', 0b0010100000110110 },
            { 'X', 0b0010110100000000 },
            { 'Y', 0b0001010100000000 },
            { 'Z', 0b0000110000001001 },
            { '[', 0b0000000000111001 },
            { '\\', 0b0010000100000000 },
            { ']', 0b0000000000001111 },
            { '^', 0b0000110000000011 },
            { '_', 0b0000000000001000 },
            { '`', 0b0000000100000000 },
            { 'a', 0b0001000001011000 },
            { 'b', 0b0010000001111000 },
            { 'c', 0b0000000011011000 },
            { 'd', 0b0000100010001110 },
            { 'e', 0b0000100001011000 },
            { 'f', 0b0000000001110001 },
            { 'g', 0b0000010010001110 },
            { 'h', 0b0001000001110000 },
            { 'i', 0b0001000000000000 },
            { 'j', 0b0000000000001110 },
            { 'k', 0b0011011000000000 },
            { 'l', 0b0000000000110000 },
            { 'm', 0b0001000011010100 },
            { 'n', 0b0001000001010000 },
            { 'o', 0b0000000011011100 },
            { 'p', 0b0000000101110000 },
            { 'q', 0b0000010010000110 },
            { 'r', 0b0000000001010000 },
            { 's', 0b0010000010001000 },
            { 't', 0b0000000001111000 },
            { 'u', 0b0000000000011100 },
            { 'v', 0b0010000000000100 },
            { 'w', 0b0010100000010100 },
            { 'x', 0b0010100011000000 },
            { 'y', 0b0010000000001100 },
            { 'z', 0b0000100001001000 },
            { '{', 0b0000100101001001 },
            { '|', 0b0001001000000000 },
            { '}', 0b0010010010001001 },
            { '~', 0b0000010100100000 }
        };

        /// <summary>
        /// Number of seven segment displays.
        /// </summary>
        private static readonly int NUMBER_OF_SEGMENTS = 4;

        /// <summary>
        /// I2C adress of the BMP280.
        /// </summary>
        private static readonly byte HT16K33_ADDRESS = 0x70;

        /// <summary>
        /// I2C blink command register.
        /// </summary>
        private static readonly byte REGISTER_BLINK_COMMAND = 0x80;

        /// <summary>
        /// I2C display on register.
        /// </summary>
        private static readonly byte REGISTER_BLINK_DISPLAY_ON = 0x01;

        /// <summary>
        /// I2C register to prevent blinking.
        /// </summary>
        private static readonly byte REGISTER_BLINK_OFF = 0x02;

        /// <summary>
        /// I2C command register to setup.
        /// </summary>
        private static readonly byte REGISTER_SYSTEM_SETUP = 0x20;

        /// <summary>
        /// I2C OSCILLATOR register.
        /// </summary>
        private static readonly byte REGISTER_OSCILLATOR = 0x01;

        /// <summary>
        /// I2C command register to set brightness.
        /// </summary>
        private static readonly byte REGISTER_BRIGHTNESS_COMMAND = 0xE0;

        /// <summary>
        /// Defines the maximum brightness.
        /// </summary>
        private static readonly int MAX_BRIGHTNESS = 15;

        byte WRITE_MODE = Convert.ToByte(HT16K33_ADDRESS << 1);

        #endregion

        #region Private properties

        // Why not byte
        /// <summary>
        /// Segment digit buffer.
        /// Used to write data to the display.
        /// </summary>
        int[] segmentBuffer = new int[10];

        /// <summary>
        /// Underyling HT16K33 device.
        /// </summary>
        private I2cDevice ht16k33;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            segmentBuffer = new int[10];
        }

        #endregion

        #region Public helper

        /// <summary>
        /// Initializes the HT16K33 async.
        /// 
        /// Caution:
        ///     This is required before accessing other
        ///     methods in this class.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task InitializeAsync()
        {
            Logger.Log(this, "InitializeAsync");

            // Prefill segment buffer.
            segmentBuffer = Enumerable.Repeat(0, 16).ToArray();

            // Check if drivers are enabled
            if (!LightningProvider.IsLightningEnabled)
            {
                Logger.Log(this, "LightningProvider not enabled. Returning.");
                return;
            }

            // Setup settings.
            I2cConnectionSettings settings = new I2cConnectionSettings(HT16K33_ADDRESS)
            {
                BusSpeed = I2cBusSpeed.FastMode
            };

            // Find i2c device.
            var i2cControllers = await I2cController.GetControllersAsync(LightningI2cProvider.GetI2cProvider());
            var i2cController = i2cControllers[0];
            ht16k33 = i2cController.GetDevice(settings);

            // Ensure device has been found.
            if (ht16k33 == null)
            {
                throw new OperationCanceledException("HT16K33 device not found.");
            }

            // Setup device.
            WriteSetup();
            WriteBlink();
            WriteBrightness();

            // DEV
            Show("Hi");
        }

        #endregion

        #region Private helper

        /// <summary>
        /// Writes the setup values.
        /// </summary>
        private void WriteSetup()
        {
            byte[] writeBuffer = new byte[] { WRITE_MODE, Convert.ToByte(REGISTER_SYSTEM_SETUP | REGISTER_OSCILLATOR) };
            ht16k33.Write(writeBuffer);
        }

        /// <summary>
        /// Writes (preventing) blink values.
        /// </summary>
        private void WriteBlink()
        {
            byte[] writeBuffer = new byte[] { WRITE_MODE, Convert.ToByte(REGISTER_BLINK_COMMAND | REGISTER_BLINK_DISPLAY_ON | REGISTER_BLINK_OFF) };
            ht16k33.Write(writeBuffer);
        }

        /// <summary>
        /// Write brightness (to the maximum).
        /// </summary>
        private void WriteBrightness()
        {
            byte[] writeBuffer = new byte[] { WRITE_MODE, Convert.ToByte(REGISTER_BRIGHTNESS_COMMAND | MAX_BRIGHTNESS) };
            ht16k33.Write(writeBuffer);
        }

        /// <summary>
        /// Updated display buffer.
        /// </summary>
        /// <param name="position">Segment index.</param>
        /// <param name="character">Character at index.</param>
        private void UpdateBuffer(int position, char character)
        {
            // Ensure position is valid.
            if(position < 0 || position > 3)
            {
                return;
            }

            var bitmask = DIGIT_DICTIONARY[character];
            segmentBuffer[position * 2] = bitmask & 0xFF;
            segmentBuffer[position * 2 + 1] = (bitmask >> 8) & 0xFF;
        }

        #endregion

        #region Public helpers

        /// <summary>
        /// Will show the given string in the seven segment display.
        /// </summary>
        /// <param name="message">Message tp show.</param>
        /// <param name="isRightAligned">True if right aligned (default).</param>
        public void Show(string message, bool isRightAligned = true)
        {
            Logger.Log(this, $"Show for {message}");

            var position = isRightAligned ? NUMBER_OF_SEGMENTS - message.Length : 0;

            for (int i = 0; i < message.Length; i++)
            {
                UpdateBuffer(position, message[i]);
            }

            List<byte> foo = new List<byte>
            {
                 WRITE_MODE, 
                0x00
            };

            Logger.Log(this, $"Writing to device.");
            foreach (var c in segmentBuffer)
            {
                foo.AddRange(BitConverter.GetBytes(c));
            }

            ht16k33.Write(foo.ToArray());
        }

        #endregion
    }
}
