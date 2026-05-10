using FaceRacer.Shared.Dto;

namespace FaceRacerLive.Monitor;

public class SampleRaceSimulator
{
    private int _index = 0;
    private IReadOnlyList<SessionData>? _simulationArray;

    private static int _nextSessionsMockTick;

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

    public Task<NextSessionsResponse?> GetNextSessionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Mocked response until the real endpoint is implemented.
        // Cycles variations so UI can show changes (new drivers, kart assigned, etc.).
        var tick = Interlocked.Increment(ref _nextSessionsMockTick);
        var variant = tick % 3;

        var session131Runs = new List<NextSessionRun>
        {
            new()
            {
                full_name = "Adriano Colombo",
                kart_color = "99242c",
                kart_name = "Individual",
                kart = variant == 0 ? "18" : "20",
                best_time = variant == 0 ? "30.769" : "30.721",
                kart_id = 18,
                uuid = "c87983a8-426e-4c54-a71f-f8a87d70a4a9"
            },
            new()
            {
                full_name = "Katya Farias",
                kart_color = "99242c",
                kart_name = "Individual",
                kart = variant == 2 ? "32" : "-",
                best_time = "-",
                kart_id = 56,
                uuid = "69448d12-99cd-4142-ab43-e9f436f06da7"
            },
            new()
            {
                full_name = "Fernanda Gomez",
                kart_color = "29abe1",
                kart_name = "Doble",
                kart = "-",
                best_time = "-",
                kart_id = null,
                uuid = "66e9ce26-fc65-4b2e-a917-d32c211e2d5e"
            }
        };

        if (variant >= 1)
        {
            session131Runs.Add(new NextSessionRun
            {
                full_name = "Mario Farias Diaz",
                kart_color = "99242c",
                kart_name = "Individual",
                kart = variant == 2 ? "3" : "-",
                best_time = "-",
                kart_id = 3,
                uuid = "d2e71c9f-9354-4244-9f65-ede6ac248ddd"
            });
        }

        var session132Runs = new List<NextSessionRun>
        {
            new()
            {
                full_name = "José Luis Gonzalez",
                kart_color = "99242c",
                kart_name = "Individual",
                kart = variant == 0 ? "-" : "9",
                best_time = "-",
                kart_id = 9,
                uuid = "72f5de1f-283a-4d49-8474-2cba6c623f4b"
            },
            new()
            {
                full_name = "Diego Perez",
                kart_color = "99242c",
                kart_name = "Individual",
                kart = "-",
                best_time = variant == 2 ? "36.166" : "-",
                kart_id = null,
                uuid = "a7abac2d-66f8-4003-9420-1f4fc2c90d2c"
            },
            new()
            {
                full_name = "Antonio Orozco",
                kart_color = "99242c",
                kart_name = "Doble",
                kart = "-",
                best_time = "-",
                kart_id = null,
                uuid = "d0c6d14a-f944-4791-a803-6bcb1ff1fc3d"
            }
        };

        if (variant == 2)
        {
            session132Runs.Add(new NextSessionRun
            {
                full_name = "Sofia De Haro raigoza",
                kart_color = "99242c",
                kart_name = "Individual",
                kart = "16",
                best_time = "-",
                kart_id = 16,
                uuid = "88513465-edd0-4323-a85d-4600c0fe5812"
            });
        }

        var session133Runs = new List<NextSessionRun>
        {
            new()
            {
                full_name = "Kevin Gomez",
                kart_color = "99242c",
                kart_name = "Individual",
                kart = "-",
                best_time = "-",
                kart_id = null,
                uuid = "1f72532e-4993-4966-b40a-fb38b302179f"
            },
            new()
            {
                full_name = "Pedro Gómez",
                kart_color = "99242c",
                kart_name = "Individual",
                kart = variant == 0 ? "-" : "31",
                best_time = "35.237",
                kart_id = null,
                uuid = "9b603cf8-fd38-41dd-b814-27b4189c51da"
            },
            new()
            {
                full_name = "Fernanda Gomez",
                kart_color = "29abe1",
                kart_name = "Doble",
                kart = "-",
                best_time = "-",
                kart_id = null,
                uuid = "614e366d-1bc1-467f-831c-0e2466637256"
            }
        };

        var response = new NextSessionsResponse
        {
            success = true,
            errors = false,
            data = new NextSessionsOuterData
            {
                data = new List<NextSession>
                {
                    new()
                    {
                        id = 57173,
                        name = "Sesión #131",
                        plain_name = "SIGUIENTE SESION",
                        label = "Sesión #131",
                        start_time = DateTime.Now.AddMinutes(-5).ToString("HH:mm"),
                        participants_text = $"Conductores: {session131Runs.Count}",
                        start_after = "Después 5",
                        checkin_time = DateTime.Now.AddMinutes(-15).ToString("HH:mm"),
                        time_left = null,
                        runs = new NextSessionRuns { data = session131Runs }
                    },
                    new()
                    {
                        id = 57174,
                        name = "Sesión #132",
                        plain_name = "SIGUIENTE SESION",
                        label = "Sesión #132",
                        start_time = DateTime.Now.AddMinutes(12).ToString("HH:mm"),
                        participants_text = $"Conductores: {session132Runs.Count}",
                        start_after = "Después 12",
                        checkin_time = DateTime.Now.AddMinutes(2).ToString("HH:mm"),
                        time_left = null,
                        runs = new NextSessionRuns { data = session132Runs }
                    },
                    new()
                    {
                        id = 57175,
                        name = "Sesión #133",
                        plain_name = "SIGUIENTE SESION",
                        label = "Sesión #133",
                        start_time = DateTime.Now.AddMinutes(25).ToString("HH:mm"),
                        participants_text = $"Conductores: {session133Runs.Count}",
                        start_after = "Después 25",
                        checkin_time = DateTime.Now.AddMinutes(15).ToString("HH:mm"),
                        time_left = null,
                        runs = new NextSessionRuns { data = session133Runs }
                    },
                    new()
                    {
                        id = 56458,
                        name = "Session #66",
                        plain_name = "SIGUIENTE SESION",
                        label = "Session #66",
                        start_time = DateTime.Now.AddMinutes(35).ToString("HH:mm"),
                        participants_text = "Conductores: 0",
                        start_after = "Después 35",
                        checkin_time = DateTime.Now.AddMinutes(25).ToString("HH:mm"),
                        time_left = null,
                        runs = new NextSessionRuns { data = new List<NextSessionRun>() }
                    }
                }
            }
        };

        return Task.FromResult<NextSessionsResponse?>(response);
    }
}