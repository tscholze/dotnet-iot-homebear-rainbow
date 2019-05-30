using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeBear.Rainbow.Controller
{
    class BMP280CalibrationInformation
    {
        /// <summary>
        /// CAlibration value for the first temperatur digit.
        /// </summary>
        public UInt16 Temperatur1 { get; set; }

        /// <summary>
        /// CAlibration value for the first temperatur digit.
        /// </summary>
        public Int16 Temperatur2 { get; set; }

        /// <summary>
        /// CAlibration value for the first temperatur digit.
        /// </summary>
        public Int16 Temperatur3 { get; set; }
    }
}
