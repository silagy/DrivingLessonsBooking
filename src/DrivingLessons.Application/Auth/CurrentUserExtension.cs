using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Auth;

public static class CurrentUserExtension
{
    public static bool MayReach(this ICurrentUser user, TeacherId teacherId)
    {
        if (user.Role == Role.Administrator)
        {
            return true;
        }

        return user.TeacherId == teacherId;
    }
}
