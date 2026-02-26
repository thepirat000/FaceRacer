using FaceRacer.Shared.Dto;

namespace FaceRacerLive.Monitor;

public class SampleRaceSimulator
{
    private int _index = 0;
    private IReadOnlyList<SessionData>? _simulationArray;

    public SessionData? GetCurrentSession(CancellationToken cancellationToken = default)
    {
        EnsureSimulationArray();

        if (_index + 1 > _simulationArray!.Count)
        {
            return null;
        }

        var sessionData = _simulationArray[_index];

        _index++;

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
        _simulationArray = null;
    }
}