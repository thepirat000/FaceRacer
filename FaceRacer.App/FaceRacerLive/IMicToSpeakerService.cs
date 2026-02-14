namespace FaceRacerLive;

public interface IMicToSpeakerService
{
    bool IsRunning { get; }
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync();
}