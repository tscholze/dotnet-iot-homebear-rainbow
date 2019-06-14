using HomeBear.Rainbow.Utils;
using Microsoft.IoT.Lightning.Providers;
using System;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Devices.Pwm;
using Windows.System.Threading;

namespace HomeBear.Rainbow.Controller
{
    /// <summary>
    /// This class helps to send controls and read values from the RainbowHAT.
    /// This is a C# port of the offical Pimoroni Python library.
    /// 
    /// Usage:
    ///     - Add event listener to events to react to value changes or user input.
    ///     - Call `PerformAction(RainbowHATAction) to trigger actions.
    /// 
    /// Links:
    ///     - Pimoroni product page:
    ///         https://shop.pimoroni.com/products/rainbow-hat-for-android-things
    ///         
    ///     - Pimoroni source:
    ///         https://github.com/pimoroni/rainbow-hat/blob/master/library/rainbowhat/lights.py
    ///         https://github.com/pimoroni/rainbow-hat/blob/master/library/rainbowhat/touch.py
    ///         https://github.com/pimoroni/rainbow-hat/blob/master/library/rainbowhat/buzzer.py
    ///         
    ///     - GPIO pin scheme: 
    ///         https://pinout.xyz/pinout/rainbow_hat
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

        /// <summary>
        /// GPIO (BCM) pin number of the buzzer (piezo) element.
        /// </summary>
        private static readonly int GPIO_NUMBER_BUZZER = 13;

        /// <summary>
        /// Cooldown between unsuccessful perform action method calls.
        /// </summary>
        private static readonly int PERFORM_ACTION_COOLDOWN_SECONDS = 1;

        /// <summary>
        /// Time span between button reads.
        /// </summary>
        private static readonly TimeSpan BUTTON_READ_INTERVAL = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// Time span between sensor reads.
        /// </summary>
        private static readonly TimeSpan SENSOR_READ_INTERVAL = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Determines if RainbowHAT is already initialized.
        /// </summary>
        private bool isInitialized = false;

        #endregion

        #region Private properties

        /// <summary>
        /// Default gpio controller of the system.
        /// </summary>
        private GpioController gpioController;

        /// <summary>
        /// Default pwm controller of the system.
        /// </summary>
        private PwmController pwmController;

        /// <summary>
        /// Default i2c controller of the system.
        /// </summary>
        private I2cController i2cController;

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
        /// PWM pin of the buzzer.
        /// </summary>
        private PwmPin buzzerPin;

        /// <summary>
        /// Default APA102 controller.
        /// </summary>
        private readonly APA102 apa102 = new APA102();

        /// <summary>
        /// Default BMP280 controller.
        /// </summary>
        private readonly BMP280 bmp280 = new BMP280();

        /// <summary>
        /// Default HT16K33 controller.
        /// </summary>
        private readonly HT16K33 ht16k33 = new HT16K33();

        /// <summary>
        /// Timer that will trigger an input read of the
        /// button GPIO pin values.
        /// </summary>
        private ThreadPoolTimer captiveButtonsValueReadTimer;

        /// <summary>
        /// Timer that will trigger an input read of the
        /// temperature sensor.
        /// </summary>
        private ThreadPoolTimer temperatureValueReadTimer;

        /// <summary>
        /// Timer that will trigger an input read of the
        /// pressure sensor.
        /// </summary>
        private ThreadPoolTimer pressureValueReadTimer;

        #endregion

        #region Constructor & Deconstructor

        /// <summary>
        /// Private constructor with rudimentary setup.
        /// </summary>
        public RainbowHAT()
        {
            InitializeAsync();
        }

        #endregion

        #region Disposeable

        /// <summary>
        /// Will dispose all related attributes.
        /// </summary>
        public void Dispose()
        {
            // Cancel timers
            captiveButtonsValueReadTimer.Cancel();
            temperatureValueReadTimer.Cancel();
            pressureValueReadTimer.Cancel();

            // Dispose child controller
            apa102.Dispose();
            bmp280.Dispose();
            ht16k33.Dispose();

            // Dispose pins
            redPin.Dispose();
            greenPin.Dispose();
            bluePin.Dispose();
            buttonAPin.Dispose();
            buttonBPin.Dispose();
            buttonCPin.Dispose();
            buzzerPin.Dispose();

            // Dispose controllers
            pwmController = null;
            i2cController = null;
            gpioController = null;
        }

        #endregion

        #region Public events

        /// <summary>
        /// Event that will be called if an captive button has been pressed.
        /// </summary>
        public event EventHandler<RainbowHATEvent> CaptiveButtonPressed;

        /// <summary>
        /// Event that will be called if a temperatur value has been meassured.
        /// </summary>
        public event EventHandler<RainbowHATEvent> TemperatureMeasured;

        /// <summary>
        /// Event that will be called if a pressure value has been meassured.
        /// </summary>
        public event EventHandler<RainbowHATEvent> PressureMeasured;

        #endregion

        #region Public helpers

        /// <summary>
        /// Performs the given action on the RainbowHAT
        /// </summary>
        /// <param name="action">Action to perform.</param>
        public void PerformAction(RainbowHATAction action)
        {
            // Ensure device has been initialized.
            if(!isInitialized)
            {
                Logger.Log(this, "Device is not initialized, yet. Retrying `PerformAction(...)` soon again.");
                ThreadPoolTimer.CreateTimer((ThreadPoolTimer threadPoolTimer) => { PerformAction(action); }, TimeSpan.FromSeconds(PERFORM_ACTION_COOLDOWN_SECONDS));
                return;
            }

            // Try perform action.
            Logger.Log(this, $"PerformAction called with '{action}' action.");
            switch (action)
            {
                case RainbowHATAction.TurnOnRed:
                    redPin.Write(GpioPinValue.High);
                    break;

                case RainbowHATAction.TurnOffRed:
                    redPin.Write(GpioPinValue.Low);
                    break;

                case RainbowHATAction.TurnOnGreen:
                    greenPin.Write(GpioPinValue.High);
                    break;

                case RainbowHATAction.TurnOffGreen:
                    greenPin.Write(GpioPinValue.Low);
                    break;

                case RainbowHATAction.TurnOnBlue:
                    bluePin.Write(GpioPinValue.High);
                    break;

                case RainbowHATAction.TurnOffBlue:
                    bluePin.Write(GpioPinValue.Low);
                    break;

                case RainbowHATAction.LEDsOn:
                    apa102.TurnOn();
                    break;

                case RainbowHATAction.LEDsOff:
                    apa102.TurnOff();
                    break;

                case RainbowHATAction.Buzz:
                    buzzerPin.Start();
                    ThreadPoolTimer.CreateTimer((ThreadPoolTimer threadPoolTimer) => { buzzerPin.Stop(); }, TimeSpan.FromMilliseconds(500));
                    break;

                case RainbowHATAction.ShowRainbow:
                    apa102.ShowColors();
                    break;

                case RainbowHATAction.ShowDemo:
                    redPin.Write(GpioPinValue.High);
                    greenPin.Write(GpioPinValue.High);
                    bluePin.Write(GpioPinValue.High);
                    apa102.ShowColors();
                    ht16k33.Show("Demo");
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
        /// Caution:
        ///     This is required before accessing other
        ///     methods in this class.
        /// </summary>
        private async void InitializeAsync()
        {
            Logger.Log(this, "Starting InitializeAsync");

            // Check if drivers are enabled
            Logger.Log(this, "Checking for LightningProvider");
            if (!LightningProvider.IsLightningEnabled)
            {
                Logger.Log(this, "LightningProvider not enabled. Returning.");
                return;
            }

            // Aggregate provider.
            LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

            // Get default controllers.
            Logger.Log(this, "Getting default controller.");
            pwmController = (await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider()))[1];
            gpioController = await GpioController.GetDefaultAsync();
            i2cController = await I2cController.GetDefaultAsync();

            // Ensure requiored controllers are available.
            if (gpioController == null || i2cController == null || pwmController == null)
            {
                Logger.Log(this, "One or more controller missing.");
                return;
            }

            // Setup controllers.
            pwmController.SetDesiredFrequency(50);

            // Setup LEDs.
            Logger.Log(this, "Setup LEDs");
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
            Logger.Log(this, "Setup buttons");
            buttonAPin = gpioController.OpenPin(21);
            buttonAPin.SetDriveMode(GpioPinDriveMode.Input);
            buttonBPin = gpioController.OpenPin(20);
            buttonBPin.SetDriveMode(GpioPinDriveMode.Input);
            buttonCPin = gpioController.OpenPin(16);
            buttonCPin.SetDriveMode(GpioPinDriveMode.Input);

            // Setup buzzer
            Logger.Log(this, "Setup buzzers / servos / motors");
            buzzerPin = pwmController.OpenPin(GPIO_NUMBER_BUZZER);
            buzzerPin.SetActiveDutyCyclePercentage(0.05);

            // Setup timer.
            Logger.Log(this, "Setup timers");
            captiveButtonsValueReadTimer = ThreadPoolTimer.CreatePeriodicTimer(CaptiveButtonsValueReadTimer_Tick, BUTTON_READ_INTERVAL);
            temperatureValueReadTimer = ThreadPoolTimer.CreatePeriodicTimer(TemperatureValueReadTimer_Tick, SENSOR_READ_INTERVAL);
            pressureValueReadTimer = ThreadPoolTimer.CreatePeriodicTimer(PreassureValueReadTimer_Tick, SENSOR_READ_INTERVAL);

            // Initialze child devices
            Logger.Log(this, "Setup APA102");
            apa102.Initialize(gpioController);

            Logger.Log(this, "Setup BMP280");
            await bmp280.InitializeAsync(i2cController);

            Logger.Log(this, "Setup HT16K33");
            ht16k33.Initialize(i2cController);

            // Set device as intialized.
            Logger.Log(this, "Finished InitializeAsync");
            isInitialized = true;
        }

        /// <summary>
        /// Triggered each time the ButtonsValueReadTimer ticks.
        /// Will check if a captive button is currently pressed and triggeres `CaptiveButtonPressed`.
        /// </summary>
        /// <param name="timer">Underlying timer.</param>
        private void CaptiveButtonsValueReadTimer_Tick(ThreadPoolTimer timer)
        {
            // Check which button has been pressed.
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

        /// <summary>
        /// Triggered each time the TemperatureValueReadTimer ticks.
        /// Will read temperature value from BMP280 and triggers `TemperatureMeasured`.
        /// </summary>
        /// <param name="timer">Underlying timer.</param>
        private void TemperatureValueReadTimer_Tick(ThreadPoolTimer timer)
        {
            // Read and format values.
            var temperature = bmp280.ReadTemperature();
            var formattedTemperature = temperature.ToString("0.00");
            var time = DateTime.Now.ToString("{hh:mm:ss}");
            Logger.Log(this, $"{time} -> Temperatur: {formattedTemperature} C");

            // Trigger event.
            TemperatureMeasured(this, new RainbowHATEvent(temperature: temperature));
        }

        /// <summary>
        /// Triggered each time the PreassureValueReadTimer ticks.
        /// Will read temperature value from BMP280 and triggers `PressureMeasured`.
        /// </summary>
        /// <param name="timer">Underlying timer.</param>
        private void PreassureValueReadTimer_Tick(ThreadPoolTimer timer)
        {
            // Read and format values.
            var pressure = bmp280.ReadPressure();
            var formattedPressure = pressure.ToString("0.00");
            var time = DateTime.Now.ToString("{hh:mm:ss}");
            Logger.Log(this, $"{time} -> Pressure: {formattedPressure} hPa");

            // Trigger event
            PressureMeasured(this, new RainbowHATEvent(pressure: pressure));
        }

        #endregion
    }
}