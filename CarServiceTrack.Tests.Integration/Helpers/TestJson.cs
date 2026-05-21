using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarServiceTrack.Tests.Integration.Helpers;

/// <summary>
/// Shared System.Text.Json options for deserializing API responses in tests.
/// The API (Program.cs) registers a <see cref="JsonStringEnumConverter"/>, so enum
/// properties (e.g. PaymentStatus, ServiceOrderStatus) are serialized as strings.
/// The default <c>ReadFromJsonAsync</c> options do NOT include that converter, so
/// reading "Pending" into an enum throws. Pass these options to every deserialize
/// call that touches a DTO with an enum member.
/// </summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
