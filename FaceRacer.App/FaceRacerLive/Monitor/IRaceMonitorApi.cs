using FaceRacerLive.Dto;

namespace FaceRacerLive.Monitor;

public interface IRaceMonitorApi
{
    Task<SessionData?> GetCurrentSession(CancellationToken cancellationToken);
}