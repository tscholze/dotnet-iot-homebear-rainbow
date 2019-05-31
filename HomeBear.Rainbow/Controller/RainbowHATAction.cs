namespace HomeBear.Rainbow.Controller
{
    /// <summary>
    /// Available RainbowHAT actions.
    /// </summary>
    public enum RainbowHATAction
    {
        /// <summary>
        /// Turns on the red LED.
        /// </summary>
        TurnOnRed,

        /// <summary>
        /// Turns on the green LED.
        /// </summary>
        TurnOnGreen,

        /// <summary>
        /// Turns on the blue LED.
        /// </summary>
        TurnOnBlue,

        /// <summary>
        /// Turns off the red LED.
        /// </summary>
        TurnOffRed,

        /// <summary>
        /// Turns off the green LED.
        /// </summary>
        TurnOffGreen,

        /// <summary>
        /// Turns off the blue LED.
        /// </summary>
        TurnOffBlue,

        /// <summary>
        /// Turns on all LEDs of the APA102.
        /// </summary>
        LEDsOn,

        /// <summary>
        /// Turns off all LEDs of the APA102.
        /// </summary>
        LEDsOff,

        /// <summary>
        /// Buzzes the piezzo element for a given time span.
        /// </summary>
        Buzz
    }
}
