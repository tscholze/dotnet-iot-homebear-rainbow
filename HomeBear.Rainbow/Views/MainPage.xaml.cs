using HomeBear.Rainbow.ViewModel;
using Windows.UI.Xaml.Controls;

namespace HomeBear.Rainbow
{
    /// <summary>
    /// Entry page of the app. 
    /// It provides an navigation point to all other functionality of HomeBear.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Properties 

        /// <summary>
        /// Underlying view model of the view / page.
        /// </summary>
        readonly MainPageViewModel viewModel;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor of the Main Page.
        /// Will initialize the data context.
        /// </summary>
        public MainPage()
        {

            InitializeComponent();
            DataContext = viewModel = new MainPageViewModel();

            
        }

        #endregion
    }
}
