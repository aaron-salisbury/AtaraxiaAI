using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AtaraxiaAI.Presentation.Desktop.Base.Converters;

/// <summary>
/// Converts <see cref="DateTime"/> and <see cref="DateTimeOffset"/> values to and from formatted string representations.
/// Supports custom date formats for both conversion and parsing. Intended for use in Avalonia data binding scenarios.
/// </summary>
public class DateFormatConverter : IValueConverter
{
    /// <summary>
    /// Converts a <see cref="DateTime"/> or <see cref="DateTimeOffset"/> value to a formatted string.
    /// </summary>
    /// <param name="value">The value produced by the binding source.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The date format string to use for conversion, or <c>null</c> for the default format.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// A formatted date string if the value is a <see cref="DateTime"/> or <see cref="DateTimeOffset"/>; otherwise, <c>null</c>.
    /// </returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dto)
        {
            value = dto.DateTime;
        }

        if (value is DateTime dt)
        {
            if (parameter is string parameterText)
            {
                return dt.ToString(parameterText);
            }
            else
            {
                return dt.ToString();
            }
        }

        return null;
    }

    /// <summary>
    /// Converts a formatted date string back to a <see cref="DateTime"/> or <see cref="DateTimeOffset"/> value.
    /// </summary>
    /// <param name="value">The value that is produced by the binding target.</param>
    /// <param name="targetType">The type to convert to (<see cref="DateTime"/> or <see cref="DateTimeOffset"/>).</param>
    /// <param name="parameter">The expected date format string for parsing, or <c>null</c> for default parsing.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// A <see cref="DateTime"/> or <see cref="DateTimeOffset"/> if parsing is successful; otherwise, <c>null</c>.
    /// </returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        if (targetType == typeof(DateTimeOffset))
        {
            if (parameter != null &&
                DateTimeOffset.TryParseExact(value.ToString(), parameter?.ToString() ?? "dd/MM/yyyy", null, DateTimeStyles.None, out DateTimeOffset dtoExact))
            {
                return dtoExact;
            }

            if (DateTimeOffset.TryParse(value.ToString(), out DateTimeOffset dto))
            {
                return dto;
            }
        }

        if (targetType == typeof(DateTime))
        {
            if (parameter != null &&
                DateTime.TryParseExact(value.ToString(), parameter?.ToString() ?? "dd/MM/yyyy", null, DateTimeStyles.None, out DateTime dtExact))
            {
                return dtExact;
            }

            if (DateTime.TryParse(value.ToString(), out DateTime dt))
            {
                return dt;
            }
        }

        return null;
    }
}
