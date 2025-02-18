using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Diagnostics;

namespace AvaloniaLsbProject1.Styles
{
    public class PageToColorConverter : IValueConverter
    {
        // Active: highlighted background for the current page
        private static readonly SolidColorBrush ActiveBrush = new SolidColorBrush(Color.Parse("#7481D4"));
        // Inactive: base background for menu items not selected
        private static readonly SolidColorBrush InactiveBrush = new SolidColorBrush(Color.Parse("#5661A6"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine($"Convert called: CurrentPage={value}, ButtonPage={parameter}");

            if (value is string currentPage && parameter is string buttonPage)
            {
                return currentPage.Equals(buttonPage, StringComparison.OrdinalIgnoreCase)
                    ? ActiveBrush
                    : InactiveBrush;
            }

            return InactiveBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
