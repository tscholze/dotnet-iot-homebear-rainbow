namespace HomeBear.Rainbow.Controller
{
    /// <summary>
    /// Describes all available APA 102 actions per led.
    /// </summary>
    enum APA102Action
    {
        /// <summary>
        /// Turns the LED on.
        /// </summary>
        TurnOn,

        /// <summary>
        /// Turns the LED off.
        /// </summary>
        TurnOff,

        /// <summary>
        /// Modifies the brightness value.
        /// </summary>
        ModifyBrightness,

        /// <summary>
        /// Modifies the red part value of the LED.
        /// </summary>
        ModifyRed,

        /// <summary>
        /// Modifies the green part value of the LED.
        /// </summary>
        ModifyGreen,

        /// <summary>
        /// Modifies the blue part value of the LED.
        /// </summary>
        ModifyBlue
    }
}
