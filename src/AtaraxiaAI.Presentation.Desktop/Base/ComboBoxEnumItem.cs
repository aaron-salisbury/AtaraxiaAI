using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace AtaraxiaAI.Presentation.Desktop.Base;

public class ComboBoxEnumItem
{
    public int Value { get; set; }
    public string? Text { get; set; }

    /// <summary>Creates an item showing the value's <see cref="DisplayAttribute"/> name, or its member name.</summary>
    public static ComboBoxEnumItem From<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        string name = value.ToString();
        string? displayName = typeof(TEnum).GetMember(name).FirstOrDefault()?.GetCustomAttribute<DisplayAttribute>()?.GetName();

        return new ComboBoxEnumItem { Value = Convert.ToInt32(value), Text = displayName ?? name };
    }
}
