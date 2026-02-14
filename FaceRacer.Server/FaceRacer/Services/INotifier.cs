using FaceRacer.DB.Entities;

namespace FaceRacer.Services.Notifiers;

public interface INotifier
{
    Task NotifyAsync(List<RecordChange> changes, CancellationToken cancellationToken);
}