using System.ComponentModel;
using System.Reflection;

namespace MedMinimalApi.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum enumValue)
        {
            // Busca o texto da [Description] via reflexão nativa
            return enumValue.GetType()
                .GetField(enumValue.ToString())?
                .GetCustomAttribute<DescriptionAttribute>()?
                .Description ?? enumValue.ToString();
        }
    }
}
