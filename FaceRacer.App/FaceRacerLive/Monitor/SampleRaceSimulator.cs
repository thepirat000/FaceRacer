using FaceRacer.Shared.Dto;

namespace FaceRacerLive.Monitor;

internal class SampleRaceSimulator
{
    private int _index;
    private IReadOnlyList<SessionData>? _simulationArray;

    public async Task<SessionData?> GetCurrentSession(CancellationToken cancellationToken = default)
    {
        // TODO: do this dynamically
        _simulationArray ??= await SampleRaceZipReader.ReadAllSessionsFromZipAsync("sample_races/session_121_2026_02_14.zip", cancellationToken);

        if (_simulationArray is null || _simulationArray.Count == 0)
        {
            return null;
        }

        var sessionData = _simulationArray[_index];

        _index = (_index + 1) % _simulationArray.Count;

        return sessionData;
    }

    public void ResetSimulation()
    {
        _index = 0;
    }
}