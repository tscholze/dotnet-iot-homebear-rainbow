using HomeBear.Rainbow.Controller;
using HomeBear.Rainbow.Utils;
using System;
using Windows.ApplicationModel;
using Windows.System.Threading;

namespace HomeBear.Rainbow.ViewModel
{
    class MainPageViewModel : BaseViewModel
    {
        #region Public properties 

        private string currentTime;
        /// <summary>
        /// Gets the current time.
        /// </summary>
        public string CurrentTime
        {
            get
            {
                return currentTime;
            }

            set
            {
                currentTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the personal, formatted greeting.
        /// </summary>
        public string Greeting
        {
            get
            {
                return "Hey ho maker friends!";
            }
        }

        /// <summary>
        /// Gets the app name.
        /// </summary>
        public string AppName
        {
            get
            {
                return Package.Current.DisplayName;
            }
        }

        /// <summary>
        /// Gets the app author's url.
        /// </summary>
        public string AppAuthorUrl
        {
            get
            {
                return "tscholze.github.io";
            }
        }

        /// <summary>
        /// Gets the current formatted app version.
        /// </summary>
        public string AppVersion
        {
            get
            {
                return string.Format("Version: {0}.{1}",
                    Package.Current.Id.Version.Major,
                    Package.Current.Id.Version.Minor);
            }
        }

        #endregion

        #region Private properties

        /// <summary>
        /// Underlying RainbowHAT.
        /// </summary>
        readonly RainbowHAT rainbowHAT = new RainbowHAT();

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor of the MainPageViewModel.
        /// Will setup timers and commands.
        /// </summary>
        public MainPageViewModel()
        {
            // Setup timer.
            ThreadPoolTimer.CreatePeriodicTimer
                (ClockTimer_Tick,
                TimeSpan.FromSeconds(1)
           );

            // Setup callback
            rainbowHAT.CaptiveButtonPressed += CaptiveButtonPressed;
        }

        private void CaptiveButtonPressed(object sender, RainbowHATEvent e)
        {
            Logger.Log(this, "CaptiveButtonPressed called");

            // Check which button has been pressed.
            if(e.Button == RainbowHATButtonSource.CaptiveA)
            {
                rainbowHAT.PerformAction(RainbowHATAction.TurnOnRed);
                rainbowHAT.PerformAction(RainbowHATAction.LEDsOn);
            }
            else if (e.Button == RainbowHATButtonSource.CaptiveC)
            {
                rainbowHAT.PerformAction(RainbowHATAction.TurnOffRed);
                rainbowHAT.PerformAction(RainbowHATAction.LEDsOff);
            }
        }

        #endregion

        #region Private helper

        /// <summary>
        /// Will be update the `CurrentTime` member with each tick.
        /// </summary>
        /// <param name="timer"></param>
        private void ClockTimer_Tick(ThreadPoolTimer timer)
        {
            CurrentTime = DateTime.Now.ToShortTimeString();
        }

        #endregion
    }
}
