namespace DrivingLessons.Application.Common.Exceptions;

public class ConcurrentChangeException(Exception innerException)
    : Exception("The data changed in another request at the same time.", innerException);
