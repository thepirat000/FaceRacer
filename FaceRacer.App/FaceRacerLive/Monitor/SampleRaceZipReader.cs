using System.IO.Compression;
using System.Text.Json;
using FaceRacer.Shared.Dto;

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

        return await ReadAllSessionsFromZipAsync(zipStream, leaveOpen: false, cancellationToken);
    }

    public static Task<List<SessionData>> ReadAllSessionsFromZipAsync(Stream zipStream, CancellationToken cancellationToken = default)
        => ReadAllSessionsFromZipAsync(zipStream, leaveOpen: false, cancellationToken);

    private static async Task<List<SessionData>> ReadAllSessionsFromZipAsync(Stream zipStream, bool leaveOpen, CancellationToken cancellationToken = default)
    {
        if (zipStream is null)
        {
            throw new ArgumentNullException(nameof(zipStream));
        }

        await using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen);

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