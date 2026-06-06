namespace FaceRacer.Shared.Dto;

public sealed record SessionInfo
(
    string SessionId, 
    string BestTime, 
    DateOnly Date, 
    string ClockText, 
    string ResultPositionText, 
    int Position, 
    string SessionUrl, 
    int UserId,
    string Username, 
    string UserFullName
);