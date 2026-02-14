using System.IO.Compression;
using System.Text.Json;

using FaceRacerLive.Dto;

namespace FaceRacerLive.Monitor;

internal static class SampleRaceZipReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<List<SessionData>> ReadAllSessionsFromZipAsync(string zipMauiAssetPath, CancellationToken cancellationToken = default)
    {
        await using var zipStream = await FileSystem.OpenAppPackageFileAsync(zipMauiAssetPath);

        await using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

        var sessions = new List<SessionData>(capacity: archive.Entries.Count);

        foreach (var entry in archive.Entries
                     .Where(e => e.Length > 0 && e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var entryStream = await entry.OpenAsync(cancellationToken);

            using var reader = new StreamReader(entryStream);

            var json = await reader.ReadToEndAsync(cancellationToken);

            var session = JsonSerializer.Deserialize<SessionData>(json, SerializerOptions);

            if (session != null)
            {
                sessions.Add(session);
            }
        }

        return sessions;
    }
}