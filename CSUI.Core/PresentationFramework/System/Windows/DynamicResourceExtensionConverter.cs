using System.ComponentModel;
using System.Globalization;

namespace System.Windows
{
    /// <summary>
    /// Converts from a <see cref="DynamicResourceExtension"/> instance to a serialization representation.
    /// </summary>
    public class DynamicResourceExtensionConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return base.CanConvertTo(context, destinationType);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
