# Driving Lessons Booking

Weekly demand collection for a driving school: teachers' availability is published as a link, students request slots, and the school receives the demand.

## Language

### Access

**User**:
A person who can sign in to the system with an email and password. Holds exactly one Role.
_Avoid_: Account, Admin user, Login

**Role**:
What a User is allowed to do — either Administrator or Teacher. Exactly one per User.
_Avoid_: Permission, Group

**Administrator**:
The Role that can do everything in the system. May optionally be linked to a Teacher (the owner who also teaches).
_Avoid_: Admin, Owner, Manager

**Teacher Role**:
The Role that sees only its linked Teacher's Week Schedules and Publications and reports that Teacher's availability. Always linked to exactly one Teacher.

**Temporary Password**:
A password an Administrator sets for a User when creating them or resetting their password.
_Avoid_: Initial password, Reset link

**Deleted User**:
A User that can no longer sign in, kept so an Administrator can restore them.
_Avoid_: Disabled, Suspended, Removed

### School

**Teacher**:
The person whose Week Schedules, Publications and Students the school manages. Exists independently of any User.
_Avoid_: Instructor

**Student**:
A learner identified by national ID, belonging to exactly one Teacher and learning on exactly one of that Teacher's Cars.
_Avoid_: Pupil, Trainee

**Change Teacher**:
Moving a Student to another Teacher together with one of that Teacher's Cars.
_Avoid_: Assign, Transfer, Reassign, Move

**Change Car**:
Switching a Student to another of their current Teacher's Cars.
_Avoid_: Assign car

**Inactive Student**:
A Student an Administrator has deactivated; cannot identify on the student form until reactivated.
_Avoid_: Deleted student, Archived

**Roster**:
A bulk file of Students that adds new Students and updates existing ones; it never deactivates a Student.
