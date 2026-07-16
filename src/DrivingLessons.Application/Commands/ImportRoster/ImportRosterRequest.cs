namespace DrivingLessons.Application.Commands.ImportRoster;

public record ImportRosterRequest(string FileName, Stream Content);
