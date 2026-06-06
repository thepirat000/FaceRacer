using FaceRacer.DB.Entities;

namespace FaceRacer.Services.Notifiers;

public interface INotifier
{
    Task NotifyUpdateAsync(List<RecordChange> changes, CancellationToken cancellationToken);
}