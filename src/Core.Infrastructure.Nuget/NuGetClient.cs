using System.IO.Compression;
using System.Net.Http.Json;
using System.Security.Cryptography;
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
        Timeout = TimeSpan.FromSeconds(60)
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

    /// <inheritdoc/>
    public async Task<string?> DownloadPackageAsync(string packageId, string? version = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get version to download
            var targetVersion = version ?? await GetLatestVersionAsync(packageId, cancellationToken);
            if (targetVersion == null)
            {
                return null;
            }

            var packageIdLower = packageId.ToLowerInvariant();
            var cacheDir = GetNuGetPackagesCacheDirectory();
            var packageDir = Path.Combine(cacheDir, packageIdLower, targetVersion);

            // Skip if already cached
            if (Directory.Exists(packageDir))
            {
                return targetVersion;
            }

            // Download .nupkg
            var nupkgUrl = $"{NuGetApiBaseUrl}/{packageIdLower}/{targetVersion}/{packageIdLower}.{targetVersion}.nupkg";
            var nupkgBytes = await HttpClient.GetByteArrayAsync(nupkgUrl, cancellationToken);

            // Create package directory
            Directory.CreateDirectory(packageDir);

            // Save .nupkg file
            var nupkgPath = Path.Combine(packageDir, $"{packageIdLower}.{targetVersion}.nupkg");
            await File.WriteAllBytesAsync(nupkgPath, nupkgBytes, cancellationToken);

            // Extract .nupkg (it's a zip file)
            ZipFile.ExtractToDirectory(nupkgPath, packageDir, overwriteFiles: true);

            // Create .nupkg.metadata file with content hash
            var contentHash = Convert.ToBase64String(SHA512.HashData(nupkgBytes));
            var metadata = new { version = 2, contentHash };
            var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(packageDir, ".nupkg.metadata"), metadataJson, cancellationToken);

            // Save .sha512 file
            await File.WriteAllTextAsync(
                Path.Combine(packageDir, $"{packageIdLower}.{targetVersion}.nupkg.sha512"),
                contentHash,
                cancellationToken);

            return targetVersion;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the NuGet packages cache directory path.
    /// </summary>
    private static string GetNuGetPackagesCacheDirectory()
    {
        var nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrEmpty(nugetPackages))
        {
            return nugetPackages;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".nuget", "packages");
    }
}
