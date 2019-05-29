using HomeBear.Rainbow.Utils;
using System;
using Windows.Devices.Gpio;
using Windows.System.Threading;

namespace HomeBear.Rainbow.Controller
{
    /// <summary>
    /// This class helps to send controls and read values from the RainbowHAT.
    /// Use the `Default` property to access this controller.
    /// 
    /// Links:
    ///     - Pimoroni: https://shop.pimoroni.com/products/rainbow-hat-for-android-things
    ///     - Scheme: https://pinout.xyz/pinout/rainbow_hat
    /// </summary>
    partial class RainbowHAT: IDisposable
    {
        #region Private constants 

        /// <summary>
        /// GPIO (BCM) pin number of the red LED.
        /// </summary>
        private static readonly int GPIO_NUMBER_RED = 6;

        /// <summary>
        /// GPIO (BCM) pin number of the green LED.
        /// </summary>
        private static readonly int GPIO_NUMBER_GREEN = 19;

        /// <summary>
        /// GPIO (BCM) pin number of the blue LED.
        /// </summary>
        private static readonly int GPIO_NUMBER_BLUE = 26;

        #endregion

        #region Private properties

        /// <summary>
        /// Default gpio controller of the system.
        /// </summary>
        private GpioController gpioController = GpioController.GetDefault();

        /// <summary>
        /// GPIO pin of the red LED.
        /// </summary>
        private GpioPin redPin;

        /// <summary>
        /// GPIO pin of the green LED.
        /// </summary>
        private GpioPin greenPin;

        /// <summary>
        /// GPIO pin of the blue LED.
        /// </summary>
        private GpioPin bluePin;

        /// <summary>
        /// GPIO pin of the A button.
        /// </summary>
        private GpioPin buttonAPin;

        /// <summary>
        /// GPIO pin of the B button.
        /// </summary>
        private GpioPin buttonBPin;

        /// <summary>
        /// GPIO pin of the C button.
        /// </summary>
        private GpioPin buttonCPin;

        /// <summary>
        /// Default APA102 controller.
        /// </summary>
        private readonly APA102 apa102 = new APA102();

        /// <summary>
        /// Default BMP280 controller.
        /// </summary>
        private readonly BMP280 bmp280 = new BMP280();

        /// <summary>
        /// Timer that will trigger an input read of the
        /// button GPIO pin values.
        /// </summary>
        private ThreadPoolTimer buttonsValueReadTimer;

        #endregion

        #region Constructor & Deconstructor

        /// <summary>
        /// Private constructor with rudimentary setup.
        /// </summary>
        public RainbowHAT()
        {
            gpioController = GpioController.GetDefault();

            // Ensure that we have a valid gpio connection
            if (gpioController == null)
            {
                throw new OperationCanceledException("Operation canceled due missing GPIO controller");
            }

            Init();
        }
        #endregion

        #region Disposeable

        /// <summary>
        /// Will dispose all related attributes.
        /// </summary>
        public void Dispose()
        {
            // Cancel timers
            buttonsValueReadTimer.Cancel();

            // Dispose child controller
            apa102.Dispose();
            bmp280.Dispose();

            // Dispose pins
            redPin.Dispose();
            greenPin.Dispose();
            bluePin.Dispose();
            buttonAPin.Dispose();
            buttonBPin.Dispose();
            buttonCPin.Dispose();
        }

        #endregion

        #region Public events

        /// <summary>
        /// Event that will be called if an captive button has been pressed.
        /// </summary>
        public event EventHandler<RainbowHATEvent> CaptiveButtonPressed;

        #endregion

        #region Public helpers

        /// <summary>
        /// Performs the given action on the RainbowHAT
        /// </summary>
        /// <param name="action">Action to perform.</param>
        public void PerformAction(RainbowHATAction action)
        {
            switch (action)
            {
                case RainbowHATAction.TurnOnRed:
                    redPin.Write(GpioPinValue.High);
                    break;

                case RainbowHATAction.TurnOffRed:
                    redPin.Write(GpioPinValue.Low);
                    break;

                case RainbowHATAction.LEDsOn:
                    apa102.TurnOn();
                    break;

                case RainbowHATAction.LEDsOff:
                    apa102.TurnOff();
                    break;

                default:
                    Logger.Log(this, $"Unknown action should be performed: {action}");
                    break;
            }
        }

        #endregion

        #region Private helpers

        /// <summary>
        /// Initializes the RainbowHAT. It will setup all required 
        /// GPIO pins.
        /// 
        /// Caution;
        ///     This is required before accessing other
        ///     methods in this class.
        /// </summary>
        private void Init()
        {
            Logger.Log(this, "Init");

            // Setup LEDs.
            redPin = gpioController.OpenPin(GPIO_NUMBER_RED);
            redPin.Write(GpioPinValue.Low);
            redPin.SetDriveMode(GpioPinDriveMode.Output);
            greenPin = gpioController.OpenPin(GPIO_NUMBER_GREEN);
            greenPin.Write(GpioPinValue.Low);
            greenPin.SetDriveMode(GpioPinDriveMode.Output);
            bluePin = gpioController.OpenPin(GPIO_NUMBER_BLUE);
            bluePin.Write(GpioPinValue.Low);
            bluePin.SetDriveMode(GpioPinDriveMode.Output);

            // Setup buttons
            buttonAPin = gpioController.OpenPin(21);
            buttonAPin.SetDriveMode(GpioPinDriveMode.Input);
            buttonBPin = gpioController.OpenPin(20);
            buttonBPin.SetDriveMode(GpioPinDriveMode.Input);
            buttonCPin = gpioController.OpenPin(16);
            buttonCPin.SetDriveMode(GpioPinDriveMode.Input);

            // Setup timer.
            buttonsValueReadTimer = ThreadPoolTimer.CreatePeriodicTimer(ButtonsValueReadTimer_Tick, TimeSpan.FromMilliseconds(500));
        }

        /// <summary>
        /// Triggered each time the ButtonsValueReadTimer ticks.
        /// Will check if a captive button is currently pressed.
        /// </summary>
        /// <param name="timer">Underlying timer.</param>
        private void ButtonsValueReadTimer_Tick(ThreadPoolTimer timer)
        {
            if (buttonAPin.Read() == GpioPinValue.Low)
            {
                Logger.Log(this, "'A'-Button tapped!");
                CaptiveButtonPressed(this, new RainbowHATEvent(RainbowHATButtonSource.CaptiveA));
            }
            else if (buttonBPin.Read() == GpioPinValue.Low)
            {
                Logger.Log(this, "'B'-Button tapped!");
                CaptiveButtonPressed(this, new RainbowHATEvent(RainbowHATButtonSource.CaptiveB));
            }
            else if (buttonCPin.Read() == GpioPinValue.Low)
            {
                Logger.Log(this, "'C'-Button tapped!");
                CaptiveButtonPressed(this, new RainbowHATEvent(RainbowHATButtonSource.CaptiveC));
            }
        }

        #endregion
    }
}