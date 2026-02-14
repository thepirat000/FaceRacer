using FaceRacerLive.Dto;

namespace FaceRacerLive.Monitor;

public interface IRaceMonitor
{
    Task<SessionData> GetCurrentSession();
}