namespace FaceRacer.Shared.Dto;

public sealed record SessionBoxResponse(bool success, bool error, int total, string html)
{

}