using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LibraryOfMoviesProject.Converters
{
    public class WidthToColumnsConverter : IValueConverter
    {
        private const double MinItemWidth = 180;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double actualWidth)
            {
                int columns = (int)(actualWidth / MinItemWidth);

                return columns > 0 ? columns : 1;
            }

            return 1;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        } 
    }
}
