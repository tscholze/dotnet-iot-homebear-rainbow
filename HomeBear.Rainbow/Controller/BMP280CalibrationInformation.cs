using System;

namespace HomeBear.Rainbow.Controller
{
    /// <summary>
    /// BMP280 calibration information.
    /// 
    /// Links:
    ///     - Datasheet:
    ///         http://www.adafruit.com/datasheets/BST-BMP280-DS001-11.pdf
    /// </summary>
    class BMP280CalibrationInformation
    {
        /// <summary>
        /// Calibration value for the first temperatur digit.
        /// </summary>
        public UInt16 Temperatur1 { get; set; }

        /// <summary>
        /// Calibration value for the second temperatur digit.
        /// </summary>
        public Int16 Temperatur2 { get; set; }

        /// <summary>
        /// Calibration value for the third temperatur digit.
        /// </summary>
        public Int16 Temperatur3 { get; set; }

        /// <summary>
        /// Calibration value for the first pressure digit.
        /// </summary>
        public UInt16 Pressure1 { get; set; }

        /// <summary>
        /// Calibration value for the second pressure digit.
        /// </summary>
        public Int16 Pressure2 { get; set; }

        /// <summary>
        /// Calibration value for the third pressure digit.
        /// </summary>
        public Int16 Pressure3 { get; set; }

        /// <summary>
        /// Calibration value for the fourth pressure digit.
        /// </summary>
        public Int16 Pressure4 { get; set; }

        /// <summary>
        /// Calibration value for the fith pressure digit.
        /// </summary>
        public Int16 Pressure5 { get; set; }

        /// <summary>
        /// Calibration value for the sixth pressure digit.
        /// </summary>
        public Int16 Pressure6 { get; set; }

        /// <summary>
        /// Calibration value for the seventh pressure digit.
        /// </summary>
        public Int16 Pressure7 { get; set; }

        /// <summary>
        /// Calibration value for the eigth pressure digit.
        /// </summary>
        public Int16 Pressure8 { get; set; }

        /// <summary>
        /// Calibration value for the ninth pressure digit.
        /// </summary>
        public Int16 Pressure9 { get; set; }
    }
}
