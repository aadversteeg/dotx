using System.Net.Http.Json;
using System.Text.Json;
using Core.Application;

namespace Core.Infrastructure.Nuget;

/// <summary>
/// Implements <see cref="INuGetClient"/> using the NuGet.org API.
/// </summary>
public class NuGetClient : INuGetClient
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private const string NuGetApiBaseUrl = "https://api.nuget.org/v3-flatcontainer";

    /// <inheritdoc/>
    public async Task<string?> GetLatestVersionAsync(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{NuGetApiBaseUrl}/{packageId.ToLowerInvariant()}/index.json";
            var response = await HttpClient.GetFromJsonAsync<NuGetVersionsResponse>(url, cancellationToken);

            if (response?.Versions == null || response.Versions.Length == 0)
            {
                return null;
            }

            return response.Versions[^1];
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }
}
