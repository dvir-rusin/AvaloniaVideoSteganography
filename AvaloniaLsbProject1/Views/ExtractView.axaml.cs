using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaLsbProject1.ViewModels;

namespace AvaloniaLsbProject1.Views
{
    public partial class ExtractView : UserControl
    {
        public ExtractView(byte[]? sharedKey,string role)
        {
            InitializeComponent();
            DataContext = new ExtractViewModel(sharedKey,role);

            // When the control is loaded, assign its parent window to the view model.
            this.Loaded += OnLoaded;

            // Attach the Click event handler
            BackButton.Click += OnBackButtonClick;
            
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Retrieve the parent window from VisualRoot and assign it to the view model.
            if (DataContext is ExtractViewModel vm)
            {
                
                vm.ParentWindow = this.VisualRoot as Window;
            }
        }

        private void OnBackButtonClick(object sender, RoutedEventArgs e)
        {
            (this.VisualRoot as MainWindow)?.NavigateToHome();
        }
    }
}
