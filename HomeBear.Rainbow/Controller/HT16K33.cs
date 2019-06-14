using HomeBear.Rainbow.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.I2c;

namespace HomeBear.Rainbow.Controller
{
    /// <summary>
    /// HT16K33 14-segment display controller.
    /// This is a C# port of the offical Pimoroni Python library.
    /// 
    /// Links:
    ///         - Pimoroni source:
    ///             https://github.com/pimoroni/rainbow-hat/blob/master/library/rainbowhat/alphanum4.py
    ///             https://github.com/pimoroni/rainbow-hat/blob/master/library/rainbowhat/HT16K33.py
    ///             
    ///         - Bitmask overview:
    ///             https://github.com/dmadison/LED-Segment-ASCII/blob/master/14-Segment/14-Segment-ASCII_BIN.txt
    ///             
    ///         - Other
    ///             https://github.com/markubiak/ht16k33-fourteensegment-display/blob/master/led_backpack.js
    ///             https://stackoverflow.com/questions/56528744/ht16k33-based-fourteen-segment-control-does-not-always-show-the-correct-characte/
    /// </summary>
    class HT16K33 : IDisposable
    {
        #region Private constants

        /// <summary>
        /// Maps chars to binary repräsentation.
        /// </summary>
        private static readonly Dictionary<char, int> BITMASK_DICTIONARY = new Dictionary<char, int>{
            { ' ', 0b0000000000000000 },
            { '!', 0b0000000000000110 },
            { '#', 0b0001001011001110 },
            { '$', 0b0001001011101101 },
            { '%', 0b0000110000100100 },
            { '&', 0b0010001101011101 },
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
            { ']', 0b0000000000001111 },
            { '^', 0b0000110000000011 },
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
        /// I2C command register to setup.
        /// </summary>
        private static readonly byte REGISTER_SYSTEM_SETUP = 0x20;

        /// <summary>
        /// I2C display command register.
        /// </summary>
        private static readonly byte REGISTER_DISPLAY_SETUP = 0x80;

        /// <summary>
        /// I2C register to turn on the display.
        /// </summary>
        private static readonly byte REGISTER_DISPLAY_ON = 0x01;

        /// <summary>
        /// I2C command register to set brightness.
        /// </summary>
        private static readonly byte REGISTER_BRIGHTNESS_SETUP = 0xE0;

        /// <summary>
        /// I2C register to prevent blinking.
        /// </summary>
        private static readonly byte REGISTER_BLINKRATE_OFF = 0x00;

        /// <summary>
        /// I2C OSCILLATOR register.
        /// </summary>
        private static readonly byte REGISTER_OSCILLATOR = 0x01;

        /// <summary>
        /// Defines the maximum brightness.
        /// </summary>
        private static readonly int MAX_BRIGHTNESS = 15;

        /// <summary>
        /// Write Buffer size.
        /// </summary>
        private static readonly int BUFFER_SIZE = NUMBER_OF_SEGMENTS * 2;

        /// <summary>
        /// Default character for bitmask.
        /// </summary>
        private static readonly char DEFAULT_BITMASK_CHAR = ' ';

        #endregion

        #region Private properties

        // Why not byte
        /// <summary>
        /// Segment digit buffer.
        /// Used to write data to the display.
        /// </summary>
        private readonly byte[] segmentBuffer = Enumerable.Repeat(Convert.ToByte(0b00000000), BUFFER_SIZE).ToArray();

        /// <summary>
        /// Underyling HT16K33 device.
        /// </summary>
        private I2cDevice ht16k33;

        #endregion

        #region IDisposable

        /// <summary>
        /// Will dispose all related attributes.
        /// </summary>
        public void Dispose()
        {
            // Reset buffer.
            ClearSegments();

            // Dispose device.
            ht16k33.Dispose();
        }

        #endregion

        #region Public helper

        /// <summary>
        /// Initializes the HT16K33.
        /// 
        /// Caution:
        ///     This is required before accessing other
        ///     methods in this class.
        /// </summary>
        /// <returns>Task.</returns>
        public void Initialize(I2cController i2cController)
        {
            Logger.Log(this, "InitializeAsync");

            // Setup device.
            ht16k33 = i2cController.GetDevice(new I2cConnectionSettings(HT16K33_ADDRESS) { BusSpeed = I2cBusSpeed.FastMode });

            // Ensure device has been found.
            if (ht16k33 == null)
            {
                throw new OperationCanceledException("HT16K33 device not found.");
            }

            // Setup device.
            ClearSegments();
            WriteSetup();
            WriteBlinkrate();
            WriteBrightness();
        }

        #endregion

        #region Private helper

        /// <summary>
        /// Writes the setup values.
        /// </summary>
        private void WriteSetup()
        {
            byte[] writeBuffer = new byte[] { Convert.ToByte(REGISTER_SYSTEM_SETUP | REGISTER_OSCILLATOR) };
            ht16k33.Write(writeBuffer);
        }

        /// <summary>
        /// Writes (preventing) blink values.
        /// </summary>
        private void WriteBlinkrate()
        {
            byte[] writeBuffer = new byte[] { Convert.ToByte(REGISTER_DISPLAY_SETUP | REGISTER_DISPLAY_ON | (REGISTER_BLINKRATE_OFF << 1)) };
            ht16k33.Write(writeBuffer);
        }

        /// <summary>
        /// Write brightness (to the maximum).
        /// </summary>
        private void WriteBrightness()
        {
            byte[] writeBuffer = new byte[] { Convert.ToByte(REGISTER_BRIGHTNESS_SETUP | MAX_BRIGHTNESS) };
            ht16k33.Write(writeBuffer);
        }

        private void ClearSegments()
        {
            Show("    ");
        }

        /// <summary>
        /// Converts given byte to bitmask.
        /// If characters is not included in bitmask data, return default value.
        /// </summary>
        /// <param name="character">Character to look for.</param>
        /// <returns>For or default bitmask.</returns>
        private int ConvertCharToBitmask(char character)
        {
            // Check if char is available.
            if(BITMASK_DICTIONARY.Keys.Contains(character))
            {
                return BITMASK_DICTIONARY[character];
            }

            // If not, return default.
            return GetDefaultCharBitmask();
        }

        /// <summary>
        /// Gets the default value for a char in the segment buffer.
        /// </summary>
        /// <returns>Default value (' ').</returns>
        private int GetDefaultCharBitmask()
        {
            return BITMASK_DICTIONARY[DEFAULT_BITMASK_CHAR];
        }

        #endregion

        #region Public helpers

        /// <summary>
        /// Will show the given string in the seven segment display.
        /// Maximum length: NUMBER_OF_SEGMENTS (4) chars.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public void Show(string message)
        {
            // Ensure message has valid length.
            if(message.Length > NUMBER_OF_SEGMENTS)
            {
                throw new OperationCanceledException($"Maximum message length is {NUMBER_OF_SEGMENTS} characters.");
            }

            // Update buffer
            Logger.Log(this, $"Show for {message}");
            for (int i = 0; i < message.Length; i++)
            {
                var bitmask = ConvertCharToBitmask(message[i]);
                segmentBuffer[i * 2] = Convert.ToByte(bitmask & 0xFF);
                segmentBuffer[i * 2 + 1] = Convert.ToByte((bitmask >> 8) & 0xFF);
            }

            // Write buffer to device.
            var sendData = segmentBuffer.ToList();
            sendData.Insert(0, 0x00);
            ht16k33.Write(sendData.ToArray());
        }

        #endregion
    }
}
