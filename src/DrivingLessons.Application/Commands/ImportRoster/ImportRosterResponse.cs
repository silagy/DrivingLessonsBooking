namespace DrivingLessons.Application.Commands.ImportRoster;

public record ImportRosterResponse(Guid RosterImportId, int Added, int Updated, int Deactivated, int Failed);
