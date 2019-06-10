using HomeBear.Rainbow.Utils;
using Microsoft.IoT.Lightning.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace HomeBear.Rainbow.Controller
{
    /// <summary>
    /// HT16K33 14-segment display controller.
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
    /// </summary>
    class HT16K33 : IDisposable
    {
        #region Private constants

        /// <summary>
        /// Maps chars to binary repräsentation.
        /// </summary>
        private static readonly Dictionary<char, byte[]> BITMASK_DICTIONARY = new Dictionary<char, byte[]>{
            { ' ',  new byte[]{0b00000000, 0b00000000} },
            { '!',  new byte[]{0b00000110, 0b01000000} },
            { '&',  new byte[]{0b00100011, 0b01011101} },
            { '(',  new byte[]{0b00100100, 0b00000000} },
            { ')',  new byte[]{0b00001001, 0b00000000} },
            { '0',  new byte[]{0b00001100, 0b00111111} },
            { '1',  new byte[]{0b00000000, 0b00000110} },
            { '2',  new byte[]{0b00000000, 0b11011011} },
            { '3',  new byte[]{0b00000000, 0b10001111} },
            { '4',  new byte[]{0b00000000, 0b11100110} },
            { '5',  new byte[]{0b00100000, 0b01101001} },
            { '6',  new byte[]{0b00000000, 0b11111101} },
            { '7',  new byte[]{0b00000000, 0b00000111} },
            { '8',  new byte[]{0b00000000, 0b11111111} },
            { '9',  new byte[]{0b00000000, 0b11101111} },
            { '?',  new byte[]{0b01100000, 0b10100011} },
            { '@',  new byte[]{0b00000010, 0b10111011} },
            { 'A',  new byte[]{0b00000000, 0b11110111} },
            { 'B',  new byte[]{0b00010010, 0b10001111} },
            { 'C',  new byte[]{0b00000000, 0b00111001} },
            { 'D',  new byte[]{0b00010010, 0b00001111} },
            { 'E',  new byte[]{0b00000000, 0b11111001} },
            { 'F',  new byte[]{0b00000000, 0b01110001} },
            { 'G',  new byte[]{0b00000000, 0b10111101} },
            { 'H',  new byte[]{0b00000000, 0b11110110} },
            { 'I',  new byte[]{0b00010010, 0b00000000} },
            { 'J',  new byte[]{0b00000000, 0b00011110} },
            { 'K',  new byte[]{0b00100100, 0b01110000} },
            { 'L',  new byte[]{0b00000000, 0b00111000} },
            { 'M',  new byte[]{0b00000101, 0b00110110} },
            { 'N',  new byte[]{0b00100001, 0b00110110} },
            { 'O',  new byte[]{0b00000000, 0b00111111} },
            { 'P',  new byte[]{0b00000000, 0b11110011} },
            { 'Q',  new byte[]{0b00100000, 0b00111111} },
            { 'R',  new byte[]{0b00100000, 0b11110011} },
            { 'S',  new byte[]{0b00000000, 0b11101101} },
            { 'T',  new byte[]{0b00010010, 0b00000001} },
            { 'U',  new byte[]{0b00000000, 0b00111110} },
            { 'V',  new byte[]{0b00001100, 0b00110000} },
            { 'W',  new byte[]{0b00101000, 0b00110110} },
            { 'X',  new byte[]{0b00101101, 0b00000000} },
            { 'Y',  new byte[]{0b00010101, 0b00000000} },
            { 'Z',  new byte[]{0b00001100, 0b00001001} },
            { 'a',  new byte[]{0b00010000, 0b01011000} },
            { 'b',  new byte[]{0b00100000, 0b01111000} },
            { 'c',  new byte[]{0b00000000, 0b11011000} },
            { 'd',  new byte[]{0b00001000, 0b10001110} },
            { 'e',  new byte[]{0b00001000, 0b01011000} },
            { 'f',  new byte[]{0b00000000, 0b01110001} },
            { 'g',  new byte[]{0b00000100, 0b10001110} },
            { 'h',  new byte[]{0b00010000, 0b01110000} },
            { 'i',  new byte[]{0b00010000, 0b00000000} },
            { 'j',  new byte[]{0b00000000, 0b00001110} },
            { 'k',  new byte[]{0b00110110, 0b00000000} },
            { 'l',  new byte[]{0b00000000, 0b00110000} },
            { 'm',  new byte[]{0b00010000, 0b11010100} },
            { 'n',  new byte[]{0b00010000, 0b01010000} },
            { 'o',  new byte[]{0b00000000, 0b11011100} },
            { 'p',  new byte[]{0b00000001, 0b01110000} },
            { 'q',  new byte[]{0b00000100, 0b10000110} },
            { 'r',  new byte[]{0b00000000, 0b01010000} },
            { 's',  new byte[]{0b00100000, 0b10001000} },
            { 't',  new byte[]{0b00000000, 0b01111000} },
            { 'u',  new byte[]{0b00000000, 0b00011100} },
            { 'v',  new byte[]{0b00100000, 0b00000100} },
            { 'w',  new byte[]{0b00101000, 0b00010100} },
            { 'x',  new byte[]{0b00101000, 0b11000000} },
            { 'y',  new byte[]{0b00100000, 0b00001100} },
            { 'z',  new byte[]{0b00001000, 0b01001000} },
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
        private byte[] segmentBuffer = Enumerable.Repeat(Convert.ToByte(0b00000000), BUFFER_SIZE).ToArray();

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
            segmentBuffer = new byte[BUFFER_SIZE];

            // Write buffer to device.
            ht16k33.Write(segmentBuffer);

            // Dispose device.
            ht16k33.Dispose();
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
            var i2cController = await I2cController.GetDefaultAsync();

            // Ensure controller has been found.
            if (i2cController == null)
            {
                throw new OperationCanceledException("I2cController device not found.");
            }

            // Get device.
            ht16k33 = i2cController.GetDevice(settings);

            // Ensure device has been found.
            if (ht16k33 == null)
            {
                throw new OperationCanceledException("HT16K33 device not found.");
            }

            // Setup device.
            WriteSetup();
            WriteBlinkrate();
            WriteBrightness();

            // DEV
            Show("1234");
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

        /// <summary>
        /// Converts given byte to bitmask.
        /// If characters is not included in bitmask data, return default value.
        /// </summary>
        /// <param name="character">Character to look for.</param>
        /// <returns>For or default bitmask.</returns>
        private byte[] ConvertCharToBitmask(char character)
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
        private byte[] GetDefaultCharBitmask()
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
                segmentBuffer[i * 2] = Convert.ToByte(bitmask[0] & 0xFF);
                segmentBuffer[i * 2 + 1] = Convert.ToByte(bitmask[1] & 0xFF);
            }

            // Write buffer to device.
            ht16k33.Write(segmentBuffer);
        }

        #endregion
    }
}
