using Avalonia.Data;
using Avalonia.Data.Converters;
using Material.Icons;
using System;
using System.Globalization;

namespace AtaraxiaAI.Base.Converters
{
    public class StringToMaterialIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is string iconName)
            {
                return (MaterialIconKind)Enum.Parse(typeof(MaterialIconKind), iconName);
            }
            else
            {
                return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
