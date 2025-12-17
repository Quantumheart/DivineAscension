using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models;

namespace PantheonWars.Tests.Data;

[ExcludeFromCodeCoverage]
public class RoleDataTests
{
    [Fact]
    public void RoleData_Constructor_SetsPropertiesCorrectly()
    {
        var role = new RoleData("test-uid", "TestRole", true, false, 5);

        Assert.Equal("test-uid", role.RoleUID);
        Assert.Equal("TestRole", role.RoleName);
        Assert.True(role.IsDefault);
        Assert.False(role.IsProtected);
        Assert.Equal(5, role.DisplayOrder);
        Assert.NotNull(role.Permissions);
        Assert.Empty(role.Permissions);
    }

    [Fact]
    public void AddPermission_AddsPermissionToSet()
    {
        var role = new RoleData("test", "Test", false, false, 0);
        role.AddPermission(RolePermissions.INVITE_PLAYERS);

        Assert.True(role.HasPermission(RolePermissions.INVITE_PLAYERS));
        Assert.Single(role.Permissions);
    }

    [Fact]
    public void RemovePermission_RemovesPermissionFromSet()
    {
        var role = new RoleData("test", "Test", false, false, 0);
        role.AddPermission(RolePermissions.INVITE_PLAYERS);
        role.RemovePermission(RolePermissions.INVITE_PLAYERS);

        Assert.False(role.HasPermission(RolePermissions.INVITE_PLAYERS));
        Assert.Empty(role.Permissions);
    }

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        var original = new RoleData("test", "Test", true, true, 1);
        original.AddPermission(RolePermissions.KICK_MEMBERS);

        var clone = original.Clone();

        Assert.Equal(original.RoleUID, clone.RoleUID);
        Assert.Equal(original.RoleName, clone.RoleName);
        Assert.Equal(original.IsDefault, clone.IsDefault);
        Assert.Equal(original.IsProtected, clone.IsProtected);
        Assert.Equal(original.DisplayOrder, clone.DisplayOrder);
        Assert.True(clone.HasPermission(RolePermissions.KICK_MEMBERS));

        // Verify it's a deep copy
        clone.AddPermission(RolePermissions.BAN_PLAYERS);
        Assert.False(original.HasPermission(RolePermissions.BAN_PLAYERS));
    }

    [Fact]
    public void RoleDefaults_CreateFounderRole_HasAllPermissions()
    {
        var founder = RoleDefaults.CreateFounderRole();

        Assert.Equal("Founder", founder.RoleName);
        Assert.True(founder.IsDefault);
        Assert.True(founder.IsProtected);
        Assert.Equal(0, founder.DisplayOrder);

        foreach (var permission in RolePermissions.AllPermissions) Assert.True(founder.HasPermission(permission));
    }

    [Fact]
    public void RoleDefaults_CreateOfficerRole_HasCorrectPermissions()
    {
        var officer = RoleDefaults.CreateOfficerRole();

        Assert.Equal("Officer", officer.RoleName);
        Assert.True(officer.IsDefault);
        Assert.False(officer.IsProtected);

        Assert.True(officer.HasPermission(RolePermissions.INVITE_PLAYERS));
        Assert.True(officer.HasPermission(RolePermissions.KICK_MEMBERS));
        Assert.False(officer.HasPermission(RolePermissions.BAN_PLAYERS));
        Assert.False(officer.HasPermission(RolePermissions.MANAGE_ROLES));
    }

    [Fact]
    public void RoleDefaults_CreateMemberRole_HasViewOnlyPermissions()
    {
        var member = RoleDefaults.CreateMemberRole();

        Assert.Equal("Member", member.RoleName);
        Assert.True(member.IsDefault);
        Assert.True(member.IsProtected);

        Assert.True(member.HasPermission(RolePermissions.VIEW_MEMBERS));
        Assert.False(member.HasPermission(RolePermissions.INVITE_PLAYERS));
    }

    [Theory]
    [InlineData("ValidRole", true)]
    [InlineData("Role 123", true)]
    [InlineData("ABC", true)]
    [InlineData("AB", false)] // Too short
    [InlineData("", false)] // Empty
    [InlineData("A", false)] // Too short
    [InlineData("ThisRoleNameIsWayTooLongAndExceedsTheMaximum", false)] // Too long
    [InlineData("Role@#$", false)] // Special chars
    public void RoleDefaults_IsValidRoleName_ValidatesCorrectly(string name, bool expected)
    {
        Assert.Equal(expected, RoleDefaults.IsValidRoleName(name));
    }

    [Theory]
    [InlineData("Founder", true)]
    [InlineData("founder", true)] // Case insensitive
    [InlineData("OFFICER", true)]
    [InlineData("Member", true)]
    [InlineData("CustomRole", false)]
    public void RoleDefaults_IsReservedName_IdentifiesReservedNames(string name, bool expected)
    {
        Assert.Equal(expected, RoleDefaults.IsReservedName(name));
    }
}