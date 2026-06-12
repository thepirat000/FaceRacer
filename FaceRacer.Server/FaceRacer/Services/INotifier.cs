using FaceRacer.DB.Entities;
using FaceRacer.Shared.Dto;

namespace FaceRacer.Services.Notifiers;

public interface INotifier
{
    Task NotifyUpdateAsync(List<RecordChange> changes, CancellationToken cancellationToken);
    Task NotifyLastSessionAsync(SessionInfo session, CancellationToken cancellationToken);
}