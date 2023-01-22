using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.IO;

namespace AtaraxiaAI.Base.Converters
{
    public class BytesToBitmapConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is byte[] imageBuffer)
            {
                using MemoryStream ms = new MemoryStream(imageBuffer);
                return new Avalonia.Media.Imaging.Bitmap(ms);
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
