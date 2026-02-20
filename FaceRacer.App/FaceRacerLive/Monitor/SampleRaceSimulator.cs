using FaceRacer.Shared.Dto;

using System.Threading;

namespace FaceRacerLive.Monitor;

internal class SampleRaceSimulator
{
    private int _index = 0;
    private IReadOnlyList<SessionData>? _simulationArray;

    public SessionData? GetCurrentSession(CancellationToken cancellationToken = default)
    {
        EnsureSimulationArray();

        var sessionData = _simulationArray![_index];

        _index = (_index + 1) % _simulationArray.Count;

        return sessionData;
    }

    private void EnsureSimulationArray()
    {
        if (_simulationArray is null || _simulationArray.Count == 0)
        {
            _simulationArray = SimulationSessionStore.TryGetSessions();
        }
    }

    public void ResetSimulation()
    {
        _index = 0;
    }
}