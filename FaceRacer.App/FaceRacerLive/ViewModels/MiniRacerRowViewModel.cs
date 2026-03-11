using FaceRacerLive.ViewModels;

namespace FaceRacerLive.ViewModels;

internal sealed class MiniRacerRowViewModel
{
    public MiniRacerRowViewModel(RacerRowViewModel racer, string bestDeltaPrev)
    {
        Racer = racer;
        BestDeltaPrev = bestDeltaPrev;
    }

    public RacerRowViewModel Racer { get; }
    public string BestDeltaPrev { get; }
}
