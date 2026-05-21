using System.Globalization;
using System.Runtime.CompilerServices;

namespace CarServiceTrack.Tests.Unit;

public static class TestCultureSetup
{
    [ModuleInitializer]
    public static void Init()
    {
        var culture = new CultureInfo("en");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
