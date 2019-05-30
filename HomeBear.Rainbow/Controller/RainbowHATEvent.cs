using System;
namespace HomeBear.Rainbow.Controller
{
    /// <summary>
    /// This event args containts specific information about
    /// the underlying RainbowHAT event triggering source.
    /// </summary>
    class RainbowHATEvent : EventArgs
    {
        #region Public properties

        /// <summary>
        /// Optional, underlying button that could be pressed.
        /// </summary>
        public RainbowHATButtonSource? Button;

        /// <summary>
        /// Optional, current meassured temperatur.
        /// </summary>
        public double? Temperature;

        /// <summary>
        /// Optional, current meassured pressure.
        /// </summary>
        public double? Pressure;

        #endregion

        #region Constructors

        /// <summary>
        /// Convenience initializer to set up a button as the source
        /// of the event.
        /// </summary>
        /// <param name="button">Optional, underlying button that has been pressed.</param>
        /// <param name="temperature">Optional, current meassured temperatur.</param>
        /// <param name="pressure">Optional, current meassured pressure.</param>
        public RainbowHATEvent(RainbowHATButtonSource? button = null, double? temperature = null, double? pressure = null) : base()
        {
            Button = button;
            Temperature = temperature;
            Pressure = pressure;
        }

        #endregion
    }
}
