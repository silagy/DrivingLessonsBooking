using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class IdentityPasswordHasherTest
{
    private const string Password = "Correct#Horse2026";

    private readonly IdentityPasswordHasher hasher = new();

    [TestMethod]
    public void Verifies_The_Password_It_Hashed()
    {
        //given
        var hash = hasher.Hash(Password);

        //when
        var verified = hasher.Verify(hash, Password);

        //then
        verified.ShouldBeTrue();
    }

    [TestMethod]
    public void Rejects_A_Wrong_Password()
    {
        //given
        var hash = hasher.Hash(Password);

        //when
        var verified = hasher.Verify(hash, "Wrong#Horse2026");

        //then
        verified.ShouldBeFalse();
    }

    [TestMethod]
    public void Hash_Is_Not_The_Plain_Password()
    {
        //when
        var hash = hasher.Hash(Password);

        //then
        hash.Value.ShouldNotContain(Password);
    }

    [TestMethod]
    public void Verifies_A_Hash_Written_By_The_Old_Admin_Seeder()
    {
        //given
        var oldSeederHasher = new PasswordHasher<LegacyAdmin>();
        var oldAdmin = new LegacyAdmin();
        var storedHash = oldSeederHasher.HashPassword(oldAdmin, Password);
        var passwordHash = PasswordHash.Of(storedHash);

        //when
        var verified = hasher.Verify(passwordHash, Password);

        //then
        verified.ShouldBeTrue();
    }

    private sealed class LegacyAdmin;
}
