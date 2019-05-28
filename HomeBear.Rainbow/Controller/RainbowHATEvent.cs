using System;
namespace HomeBear.Rainbow.Controller
{
    /// <summary>
    /// This event args containts specific information about
    /// the underlying RainbowHAT event triggering source.
    /// </summary>
    class RainbowHATEvent : EventArgs
    {
        /// <summary>
        /// Underlying button that could be pressed.
        /// </summary>
        public RainbowHATButtonSource Button;

        /// <summary>
        /// Convenience initializer to set up a button as the source
        /// of the event
        /// </summary>
        /// <param name="button">Underlying button that has been pressed.</param>
        public RainbowHATEvent(RainbowHATButtonSource button) : base()
        {
            Button = button;
        }
    }
}
