using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DataGateMonitor.Serialization;

/// <summary>Single Newtonsoft.Json settings for HTTP, storage, and SignalR JSON payloads.</summary>
public static class ProjectJson
{
    public static readonly JsonSerializerSettings WebSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
    };

    public static string Serialize(object? value) =>
        JsonConvert.SerializeObject(value, WebSettings);

    public static T? Deserialize<T>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? default : JsonConvert.DeserializeObject<T>(json, WebSettings);

    public static HttpContent ToJsonContent(object value) =>
        new StringContent(Serialize(value), Encoding.UTF8, "application/json");

    public static async Task<T?> ReadContentAsync<T>(HttpContent content, CancellationToken ct)
        where T : class
    {
        var json = await content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return Deserialize<T>(json);
    }
}
