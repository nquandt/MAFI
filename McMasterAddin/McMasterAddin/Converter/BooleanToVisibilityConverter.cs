using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace McMasterAddin.Converter
{
    public enum BooleanToVisibilityConverterType
    {
        Normal = 1,
        Reverse = 2
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var targertValue = false;

            if (value == null)
            {
                throw new Exception("BooleanToVisibilityConverter - Convert Error");
            }
            else if (!Boolean.TryParse(value.ToString(), out targertValue))
            {
                throw new Exception("BooleanToVisibilityConverter - Convert Error");
            }
            else
            {
                var parameterValue = BooleanToVisibilityConverterType.Normal;

                if (parameter != null)
                {
                    Enum.TryParse<BooleanToVisibilityConverterType>(parameter.ToString(), out parameterValue);
                }

                if (parameterValue == BooleanToVisibilityConverterType.Reverse)
                {
                    return targertValue ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    return targertValue ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var targetValue = Visibility.Collapsed;

            if (value == null)
            {
                throw new Exception("BooleanToVisibilityConverter - ConvertBack Error");
            }
            else if (!Enum.TryParse<Visibility>(value.ToString(), out targetValue))
            {
                throw new Exception("BooleanToVisibilityConverter - ConvertBack Error");
            }
            else
            {
                var parameterValue = BooleanToVisibilityConverterType.Normal;

                if (parameter != null)
                {
                    Enum.TryParse<BooleanToVisibilityConverterType>(parameter.ToString(), out parameterValue);
                }

                if (parameterValue == BooleanToVisibilityConverterType.Reverse)
                {
                    return targetValue == Visibility.Visible ? false : true;
                }
                else
                {
                    return targetValue == Visibility.Visible ? true : false;
                }
            }
        }
    }
}
