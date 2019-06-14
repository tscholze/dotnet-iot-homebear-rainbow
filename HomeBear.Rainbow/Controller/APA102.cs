using HomeBear.Rainbow.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Gpio;

namespace HomeBear.Rainbow.Controller
{
    /// <summary>
    /// APA102 controller.
    /// Specified for using it with the RainbowHAT.
    /// This is a C# port of the offical Pimoroni Python library.
    /// 
    /// Links:
    ///     - Pimoroni original code:
    ///         https://github.com/pimoroni/rainbow-hat/blob/master/library/rainbowhat/apa102.py
    /// 
    ///     - PimoroniSharp port of the mostly similar Blinkt!:
    ///         https://github.com/MarcJenningsUK/PimoroniSharp/blob/master/Pimoroni.Blinkt/Blinkt.cs
    /// </summary>
    partial class APA102 : IDisposable
    {
        #region Private constants

        /// <summary>
        /// Data BCM GPIO pin number.
        /// </summary>
        private const int GPIO_NUMBER_DATA = 10;

        /// <summary>
        /// Clock BCM GPIO pin numbers.
        /// </summary>
        private const int GPIO_NUMBER_CLOCK = 11;

        /// <summary>
        /// Number of available LEDs of the APA102.
        /// </summary>
        private const int NUMBER_OF_LEDS = 7;

        /// <summary>
        /// Number of pulses that is required to lock the clock.
        /// </summary>
        private const int NUMBER_OF_CLOCK_LOCK_PULSES = 36;

        /// <summary>
        /// Number of pulses that is required to release the clock.
        /// </summary>
        private const int NUMBER_OF_CLOCK_UNLOCK_PULSES = 32;

        #endregion

        #region Private properties 

        /// <summary>
        /// GPIO pin for the data value.
        /// </summary>
        private GpioPin dataPin;

        /// <summary>
        /// GPIO pin for the clock value.
        /// </summary>
        private GpioPin clockPin;

        /// <summary>
        /// GPIO pin for the clear signal value;
        /// </summary>
        private GpioPin csPin;

        /// <summary>
        /// List of all led leds.
        /// </summary>
        private readonly APA102LED[] leds = new APA102LED[NUMBER_OF_LEDS];

        #endregion

        #region Disposeable

        /// <summary>
        /// Will dispose all related attributes.
        /// </summary>
        public void Dispose()
        {
            TurnOff();
            WriteLEDValues();
            clockPin.Dispose();
            dataPin.Dispose();
            csPin.Dispose();
        }

        #endregion

        #region Private helper

        /// <summary>
        /// Initializes the APA102.
        /// 
        /// Caution:
        ///     This is required before accessing other
        ///     methods in this class.
        /// </summary>
        /// <returns>Task.</returns>
        public void Initialize(GpioController gpioController)
        {
            Logger.Log(this, "InitializeAsync");

            // Setup LEDs.
            for (int i = 0; i < NUMBER_OF_LEDS; i++)
            {
                leds[i] = new APA102LED();
            }

            // Setup pins.
            dataPin = gpioController.OpenPin(GPIO_NUMBER_DATA);
            clockPin = gpioController.OpenPin(GPIO_NUMBER_CLOCK);
            csPin = gpioController.OpenPin(8);
            dataPin.SetDriveMode(GpioPinDriveMode.Output);
            clockPin.SetDriveMode(GpioPinDriveMode.Output);
            csPin.SetDriveMode(GpioPinDriveMode.Output);

            WriteLEDValues();
        }

        /// <summary>
        /// Sets the clock state to locked or unlocked.
        /// </summary>
        /// <param name="locked">New locked state.</param>
        private void SetClockState(bool locked)
        {
            // Get the number of required pulses.
            var numberOfPulses = locked ? NUMBER_OF_CLOCK_LOCK_PULSES : NUMBER_OF_CLOCK_UNLOCK_PULSES;

            // Switch of data transfer.
            dataPin.Write(GpioPinValue.Low);

            // Send pulses to clock pin.
            for (int i = 0; i < numberOfPulses; i++)
            {
                clockPin.Write(GpioPinValue.High);
                clockPin.Write(GpioPinValue.Low);
            }
        }

        /// <summary>
        /// Writes an LED value to the device.
        /// </summary>
        /// <param name="led">LED value to write.</param>
        private void WriteLEDValue(APA102LED led)
        {
            var sendBright = (int)((31.0m * led.Brightness)) & 31;
            WriteByte(Convert.ToByte(224 | sendBright));
            WriteByte(Convert.ToByte(led.Blue));
            WriteByte(Convert.ToByte(led.Green));
            WriteByte(Convert.ToByte(led.Red));
        }

        /// <summary>
        /// Writes all LED values to the device.
        /// </summary>
        private void WriteLEDValues()
        {
            // Prepare for writing to APA102.
            csPin.Write(GpioPinValue.Low);
            SetClockState(true);

            // Update each LED.
            foreach (var led in leds)
            {
                WriteLEDValue(led);
            }

            // Raise update signal.
            SetClockState(false);
            csPin.Write(GpioPinValue.High);
        }

        /// <summary>
        /// Writes an input byte to the device.
        /// </summary>
        /// <param name="input">Input byte to write.</param>
        private void WriteByte(byte input)
        {
            int value;
            byte modded = Convert.ToByte(input);
            for (int i = 0; i < 8; i++)
            {
                value = modded & 128;
                dataPin.Write(value == 128 ? GpioPinValue.High : GpioPinValue.Low);
                clockPin.Write(GpioPinValue.High);
                modded = Convert.ToByte((modded << 1) % 256);
                clockPin.Write(GpioPinValue.Low);
            }
        }

        /// <summary>
        /// Performs given action.
        /// </summary>
        /// <param name="action">Action to perform.</param>
        /// <param name="value">Optional value of the action.</param>
        /// <param name="writeByte">If true, changes will be written to the device.</param>
        /// <param name="index">Optional index of the action.</param>
        private void PerformAction(APA102Action action, int? value = null, bool writeByte = false, int? index = null)
        {
            // Get specified leds.
            List<APA102LED> specifiedLEDs;
            if (index is int ledIndex)
            {
                specifiedLEDs = new List<APA102LED> { leds[ledIndex] };
            }
            else
            {
                specifiedLEDs = leds.ToList();
            }

            // Perform action.
            switch (action)
            {
                case APA102Action.TurnOn:
                    specifiedLEDs.ForEach(p => p.TurnOn());
                    break;

                case APA102Action.TurnOff:
                    specifiedLEDs.ForEach(p => p.TurnOff());
                    break;

                case APA102Action.ModifyBrightness:
                    var brightnessValues = Convert.ToDecimal(value) / 100;
                    specifiedLEDs.ForEach(p => p.SetBrightness(brightnessValues));
                    break;

                case APA102Action.ModifyRed:
                    var redValue = Convert.ToInt32(value);
                    specifiedLEDs.ForEach(p => p.SetRed(redValue));
                    break;

                case APA102Action.ModifyGreen:
                    var greenValue = Convert.ToInt32(value);
                    specifiedLEDs.ForEach(p => p.SetGreen(greenValue));
                    break;

                case APA102Action.ModifyBlue:
                    var blueValue = Convert.ToInt32(value);
                    specifiedLEDs.ForEach(p => p.SetBlue(blueValue));
                    break;

                case APA102Action.ShowColors:
                    leds[0].SetRgbHex("#ee4035", 0.1m);
                    leds[1].SetRgbHex("#ee4035", 0.1m);
                    leds[2].SetRgbHex("#f37736", 0.1m);
                    leds[3].SetRgbHex("#fdf498", 0.1m);
                    leds[4].SetRgbHex("#7bc043", 0.1m);
                    leds[5].SetRgbHex("#0392cf", 0.1m);
                    leds[6].SetRgbHex("#0392cf", 0.1m);
                    break;

                default:
                    throw new NotImplementedException($"{action} is not implemented");
            }

            // Write bytes to GPIO if required.
            if (writeByte)
            {
                WriteLEDValues();
            }
        }

        #endregion

        #region Public method helper

        /// <summary>
        /// Will turn on all LEDs to maximum whiteness.
        /// </summary>
        /// <param name="index">If set, only specified led will be modified.</param>
        public void TurnOn(int? index = null)
        {
            PerformAction(APA102Action.TurnOn, null, true, index);
        }

        /// <summary>
        /// Will turn on all leds to maximum darkness.
        /// </summary>
        /// <param name="index">If set, only specified led will be modified.</param>
        public void TurnOff(int? index = null)
        {
            PerformAction(APA102Action.TurnOff, null, true, index);
        }

        /// <summary>
        /// Shows color full LED values on the strip of seven.
        /// </summary>
        public void ShowColors()
        {
            PerformAction(APA102Action.ShowColors, writeByte: true);
        }

        /// <summary>
        /// Sets brightness value for all or specified leds.
        /// </summary>
        /// <param name="value">New value.</param>
        /// <param name="writeByte">If `true`, the changes will 
        /// immediately written to the GPIO pins.</param>
        /// <param name="index">If set, only specified led will be modified.</param>
        public void SetBrightness(int value, bool writeByte = false, int? index = null)
        {
            PerformAction(APA102Action.ModifyBrightness, value, writeByte, index);
        }

        /// <summary>
        /// Sets red value for all or specified leds.
        /// </summary>
        /// <param name="value">New value.</param>
        /// <param name="writeByte">If `true`, the changes will 
        /// immediately written to the GPIO pins.</param>
        /// <param name="index">If set, only specified led will be modified.</param>
        public void SetRed(int value, bool writeByte = false, int? index = null)
        {
            PerformAction(APA102Action.ModifyRed, value, writeByte, index);
        }

        /// <summary>
        /// Sets green value for all or specified leds.
        /// </summary>
        /// <param name="value">New value.</param>
        /// <param name="writeByte">If `true`, the changes will 
        /// immediately written to the GPIO pins.</param>
        /// <param name="index">If set, only specified led will be modified.</param>
        public void SetGreen(int value, bool writeByte = false, int? index = null)
        {
            PerformAction(APA102Action.ModifyGreen, value, writeByte, index);
        }

        /// <summary>
        /// Sets value for all or specified leds.
        /// </summary>
        /// <param name="value">New value.</param>
        /// <param name="writeByte">If `true`, the changes will 
        /// immediately written to the GPIO pins.</param>
        public void SetBlue(int value, bool writeByte = false, int? index = null)
        {
            PerformAction(APA102Action.ModifyBlue, value, writeByte, index);

        }

        /// <summary>
        /// Updates all leds with underlying values.
        /// </summary>
        public void UpdateAll()
        {
            WriteLEDValues();
        }

        #endregion

    }
}
