using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace HomeBear.Rainbow.Controller
{
    // https://github.com/pimoroni/rainbow-hat/blob/master/library/rainbowhat/bmp280.py
    class BMP280: IDisposable
    {
        #region Public Properties

        #endregion

        #region Private properties 

        /// <summary>
        /// System's default GPIO controller.
        /// </summary>
        private GpioController gpioController = GpioController.GetDefault();

        #endregion

        #region Constructor & Deconstructor

        public BMP280()
        {
          
        }

        #endregion

        #region IDisposeable

        public void Dispose()
        {

        }

        #endregion
    }
}
