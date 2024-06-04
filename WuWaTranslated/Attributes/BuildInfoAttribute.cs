using System.Globalization;
using System.Reflection;

namespace WuWaTranslated.Attributes;

[AttributeUsage(AttributeTargets.Assembly)]
public class BuildInfoAttribute : Attribute
{
    public static readonly BuildInfoAttribute Instance = Assembly.GetCallingAssembly().GetCustomAttribute<BuildInfoAttribute>() ?? new BuildInfoAttribute();

    public DateTime BuildTime { get; } = DateTime.UnixEpoch;

    public BuildInfoAttribute()
    {
    }

    public BuildInfoAttribute(string buildTime)
    {
        BuildTime = DateTime.TryParse(buildTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var buildDateTime)
            ? buildDateTime
            : DateTime.UnixEpoch;
    }
}