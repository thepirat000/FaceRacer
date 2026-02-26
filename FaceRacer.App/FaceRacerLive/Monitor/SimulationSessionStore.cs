using FaceRacer.Shared.Dto;

namespace FaceRacerLive.Monitor;

internal static class SimulationSessionStore
{
    private static readonly Lock Gate = new();
    private static IReadOnlyList<SessionData>? _sessions;
    private static string? _zipFileName;

    public static void SetSessions(IReadOnlyList<SessionData> sessions)
    {
        if (sessions is null)
        {
            throw new ArgumentNullException(nameof(sessions));
        }

        lock (Gate)
        {
            _sessions = sessions;
        }
    }

    public static void SetSessions(IReadOnlyList<SessionData> sessions, string? zipFileName)
    {
        if (sessions is null)
        {
            throw new ArgumentNullException(nameof(sessions));
        }

        lock (Gate)
        {
            _sessions = sessions;
            _zipFileName = zipFileName;
        }
    }

    public static IReadOnlyList<SessionData>? TryGetSessions()
    {
        lock (Gate)
        {
            return _sessions;
        }
    }

    public static bool HasSimulation()
    {
        return _sessions?.Count > 0;
    }

    public static string? TryGetZipFileName()
    {
        lock (Gate)
        {
            return _zipFileName;
        }
    }

    public static void Clear()
    {
        lock (Gate)
        {
            _sessions = null;
            _zipFileName = null;
        }
    }
}
