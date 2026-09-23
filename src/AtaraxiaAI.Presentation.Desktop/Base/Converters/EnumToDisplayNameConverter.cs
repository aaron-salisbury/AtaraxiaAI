using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace AtaraxiaAI.Presentation.Desktop.Base.Converters;

/// <summary>
/// Converts a collection of enum values to their display names using the <see cref="DisplayAttribute"/> if present.
/// Intended for use in Avalonia data binding scenarios.
/// </summary>
public class EnumToDisplayNameConverter : IValueConverter
{
    /// <summary>
    /// Converts a collection of enum values to a list of their display names.
    /// </summary>
    /// <param name="value">An <see cref="IEnumerable"/> of enum values to convert.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">An optional parameter (not used).</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// A <see cref="List{String}"/> of display names if <paramref name="value"/> is an <see cref="IEnumerable"/> of enums;
    /// otherwise, a <see cref="BindingNotification"/> with an error.
    /// </returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }
        else if (value is IEnumerable enums)
        {
            List<string> enumDisplayNames = [];

            foreach (Enum e in enums)
            {
                enumDisplayNames.Add(GetAttribute<DisplayAttribute>(e)?.Name ?? e.ToString());
            }

            return enumDisplayNames;
        }
        else
        {
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }
    }

    /// <summary>
    /// Not supported. Throws <see cref="NotSupportedException"/> if called.
    /// </summary>
    /// <param name="value">The value that is produced by the binding target.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">An optional parameter.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>This method does not return a value.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static TAttribute? GetAttribute<TAttribute>(Enum enumValue) where TAttribute : Attribute
    {
        return enumValue
            .GetType()
            .GetMember(enumValue.ToString())
            .First()
            .GetCustomAttribute<TAttribute>();
    }
}
