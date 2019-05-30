using System;

namespace HomeBear.Rainbow.Controller
{
    /// <summary>
    /// BMP280 calibration information.
    /// </summary>
    class BMP280CalibrationInformation
    {
        /// <summary>
        /// Calibration value for the first temperatur digit.
        /// </summary>
        public UInt16 Temperatur1 { get; set; }

        /// <summary>
        /// Calibration value for the first temperatur digit.
        /// </summary>
        public Int16 Temperatur2 { get; set; }

        /// <summary>
        /// Calibration value for the first temperatur digit.
        /// </summary>
        public Int16 Temperatur3 { get; set; }
    }
}
