using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems;
using PantheonWars.Systems.Interfaces;

namespace PantheonWars.Tests.Systems;

/// <summary>
///     Unit tests for RoleManager
///     Tests role CRUD operations, permission management, and role assignment
/// </summary>
[ExcludeFromCodeCoverage]
public class RoleManagerTests
{
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly RoleManager _roleManager;

    public RoleManagerTests()
    {
        _mockReligionManager = new Mock<IReligionManager>();
        _roleManager = new RoleManager(_mockReligionManager.Object);
    }

    #region Helper Methods

    private ReligionData CreateTestReligion(string religionId = "religion-1", string founderId = "founder-1")
    {
        var religion = new ReligionData
        {
            ReligionUID = religionId,
            ReligionName = "Test Religion",
            Deity = DeityType.Khoras,
            FounderUID = founderId,
            IsPublic = true,
            MemberUIDs = new List<string> { founderId, "member-1", "member-2" },
            Roles = RoleDefaults.CreateDefaultRoles(),
            MemberRoles = new Dictionary<string, string>
            {
                [founderId] = RoleDefaults.FOUNDER_ROLE_ID,
                ["member-1"] = RoleDefaults.MEMBER_ROLE_ID,
                ["member-2"] = RoleDefaults.MEMBER_ROLE_ID
            }
        };

        return religion;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullReligionManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RoleManager(null!));
    }

    #endregion

    #region CreateCustomRole Tests

    [Fact]
    public void CreateCustomRole_WithValidParameters_CreatesRole()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.CreateCustomRole("religion-1", "founder-1", "Moderator");

        // Assert
        Assert.True(result.success);
        Assert.NotNull(result.role);
        Assert.Equal("Moderator", result.role.RoleName);
        Assert.False(result.role.IsDefault);
        Assert.False(result.role.IsProtected);
        Assert.True(result.role.HasPermission(RolePermissions.VIEW_MEMBERS));
        Assert.Empty(result.error);
        _mockReligionManager.Verify(m => m.Save(religion), Times.Once);
    }

    [Fact]
    public void CreateCustomRole_WithNonExistentReligion_ReturnsError()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("non-existent")).Returns((ReligionData?)null);

        // Act
        var result = _roleManager.CreateCustomRole("non-existent", "player-1", "Test");

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("Religion not found", result.error);
    }

    [Fact]
    public void CreateCustomRole_WithoutManageRolesPermission_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act - member-1 doesn't have MANAGE_ROLES permission
        var result = _roleManager.CreateCustomRole("religion-1", "member-1", "Test");

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("You don't have permission to manage roles", result.error);
    }

    [Theory]
    [InlineData("AB")] // Too short
    [InlineData("A")] // Too short
    [InlineData("")] // Empty
    [InlineData("ThisRoleNameIsWayTooLongAndExceedsTheMaximumLengthOf30Characters")] // Too long
    [InlineData("Role@#$")] // Special characters
    public void CreateCustomRole_WithInvalidRoleName_ReturnsError(string invalidName)
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.CreateCustomRole("religion-1", "founder-1", invalidName);

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("Role name must be 3-30 characters and alphanumeric", result.error);
    }

    [Theory]
    [InlineData("Founder")]
    [InlineData("founder")]
    [InlineData("FOUNDER")]
    [InlineData("Officer")]
    [InlineData("Member")]
    public void CreateCustomRole_WithReservedName_ReturnsError(string reservedName)
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.CreateCustomRole("religion-1", "founder-1", reservedName);

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("Cannot use reserved role names (Founder, Officer, Member)", result.error);
    }

    [Fact]
    public void CreateCustomRole_WithDuplicateName_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        var existingRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = existingRole;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.CreateCustomRole("religion-1", "founder-1", "Moderator");

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("A role with this name already exists", result.error);
    }

    [Fact]
    public void CreateCustomRole_ExceedingMaxRoles_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();

        // Add 5 custom roles (max limit)
        for (var i = 1; i <= 5; i++)
        {
            var role = new RoleData($"custom-{i}", $"Role{i}", false, false, 10 + i);
            religion.Roles[$"custom-{i}"] = role;
        }

        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.CreateCustomRole("religion-1", "founder-1", "NewRole");

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("Maximum of 5 custom roles allowed", result.error);
    }

    #endregion

    #region DeleteRole Tests

    [Fact]
    public void DeleteRole_WithValidRole_DeletesRole()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = customRole;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.DeleteRole("religion-1", "founder-1", "custom-1");

        // Assert
        Assert.True(result.success);
        Assert.Empty(result.error);
        Assert.False(religion.Roles.ContainsKey("custom-1"));
        _mockReligionManager.Verify(m => m.Save(religion), Times.Once);
    }

    [Fact]
    public void DeleteRole_WithNonExistentReligion_ReturnsError()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("non-existent")).Returns((ReligionData?)null);

        // Act
        var result = _roleManager.DeleteRole("non-existent", "player-1", "role-1");

        // Assert
        Assert.False(result.success);
        Assert.Equal("Religion not found", result.error);
    }

    [Fact]
    public void DeleteRole_WithoutPermission_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.DeleteRole("religion-1", "member-1", "custom-1");

        // Assert
        Assert.False(result.success);
        Assert.Equal("You don't have permission to manage roles", result.error);
    }

    [Fact]
    public void DeleteRole_WithNonExistentRole_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.DeleteRole("religion-1", "founder-1", "non-existent-role");

        // Assert
        Assert.False(result.success);
        Assert.Equal("Role not found", result.error);
    }

    [Theory]
    [InlineData(RoleDefaults.FOUNDER_ROLE_ID)]
    [InlineData(RoleDefaults.MEMBER_ROLE_ID)]
    public void DeleteRole_WithProtectedRole_ReturnsError(string protectedRoleId)
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.DeleteRole("religion-1", "founder-1", protectedRoleId);

        // Assert
        Assert.False(result.success);
        Assert.Equal("Cannot delete system roles (Founder, Member)", result.error);
    }

    [Fact]
    public void DeleteRole_WithMembersAssigned_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = customRole;
        religion.MemberRoles["member-1"] = "custom-1"; // Assign member to custom role
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.DeleteRole("religion-1", "founder-1", "custom-1");

        // Assert
        Assert.False(result.success);
        Assert.Contains("Cannot delete role with 1 member(s)", result.error);
    }

    #endregion

    #region RenameRole Tests

    [Fact]
    public void RenameRole_WithValidParameters_RenamesRole()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = customRole;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.RenameRole("religion-1", "founder-1", "custom-1", "Admin");

        // Assert
        Assert.True(result.success);
        Assert.NotNull(result.role);
        Assert.Equal("Admin", result.role.RoleName);
        Assert.Empty(result.error);
        _mockReligionManager.Verify(m => m.Save(religion), Times.Once);
    }

    [Fact]
    public void RenameRole_WithNonExistentReligion_ReturnsError()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("non-existent")).Returns((ReligionData?)null);

        // Act
        var result = _roleManager.RenameRole("non-existent", "player-1", "role-1", "NewName");

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("Religion not found", result.error);
    }

    [Fact]
    public void RenameRole_WithoutPermission_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.RenameRole("religion-1", "member-1", "custom-1", "NewName");

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("You don't have permission to manage roles", result.error);
    }

    [Fact]
    public void RenameRole_FounderRole_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.RenameRole("religion-1", "founder-1", RoleDefaults.FOUNDER_ROLE_ID, "NewName");

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("Cannot rename Founder role", result.error);
    }

    [Fact]
    public void RenameRole_WithInvalidName_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = customRole;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.RenameRole("religion-1", "founder-1", "custom-1", "AB"); // Too short

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("Role name must be 3-30 characters and alphanumeric", result.error);
    }

    [Fact]
    public void RenameRole_WithReservedName_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = customRole;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.RenameRole("religion-1", "founder-1", "custom-1", "Founder");

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("Cannot use reserved role names", result.error);
    }

    [Fact]
    public void RenameRole_WithDuplicateName_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole1 = new RoleData("custom-1", "Moderator", false, false, 10);
        var customRole2 = new RoleData("custom-2", "Admin", false, false, 11);
        religion.Roles["custom-1"] = customRole1;
        religion.Roles["custom-2"] = customRole2;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.RenameRole("religion-1", "founder-1", "custom-1", "Admin");

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("A role with this name already exists", result.error);
    }

    #endregion

    #region ModifyRolePermissions Tests

    [Fact]
    public void ModifyRolePermissions_WithValidPermissions_UpdatesPermissions()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = customRole;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        var newPermissions = new HashSet<string>
        {
            RolePermissions.INVITE_PLAYERS,
            RolePermissions.KICK_MEMBERS
        };

        // Act
        var result = _roleManager.ModifyRolePermissions("religion-1", "founder-1", "custom-1", newPermissions);

        // Assert
        Assert.True(result.success);
        Assert.NotNull(result.role);
        Assert.Equal(2, result.role.Permissions.Count);
        Assert.True(result.role.HasPermission(RolePermissions.INVITE_PLAYERS));
        Assert.True(result.role.HasPermission(RolePermissions.KICK_MEMBERS));
        Assert.Empty(result.error);
        _mockReligionManager.Verify(m => m.Save(religion), Times.Once);
    }

    [Fact]
    public void ModifyRolePermissions_WithNonExistentReligion_ReturnsError()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("non-existent")).Returns((ReligionData?)null);
        var permissions = new HashSet<string> { RolePermissions.VIEW_MEMBERS };

        // Act
        var result = _roleManager.ModifyRolePermissions("non-existent", "player-1", "role-1", permissions);

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("Religion not found", result.error);
    }

    [Fact]
    public void ModifyRolePermissions_WithoutPermission_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        var permissions = new HashSet<string> { RolePermissions.VIEW_MEMBERS };

        // Act
        var result = _roleManager.ModifyRolePermissions("religion-1", "member-1", "custom-1", permissions);

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("You don't have permission to manage roles", result.error);
    }

    [Fact]
    public void ModifyRolePermissions_ForFounderRole_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        var permissions = new HashSet<string> { RolePermissions.VIEW_MEMBERS };

        // Act
        var result =
            _roleManager.ModifyRolePermissions("religion-1", "founder-1", RoleDefaults.FOUNDER_ROLE_ID, permissions);

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Equal("Cannot modify Founder role permissions", result.error);
    }

    [Fact]
    public void ModifyRolePermissions_WithInvalidPermission_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = customRole;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        var invalidPermissions = new HashSet<string>
        {
            RolePermissions.VIEW_MEMBERS,
            "invalid_permission"
        };

        // Act
        var result = _roleManager.ModifyRolePermissions("religion-1", "founder-1", "custom-1", invalidPermissions);

        // Assert
        Assert.False(result.success);
        Assert.Null(result.role);
        Assert.Contains("Invalid permission: invalid_permission", result.error);
    }

    [Fact]
    public void ModifyRolePermissions_WithEmptyPermissions_ClearsAllPermissions()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        customRole.AddPermission(RolePermissions.INVITE_PLAYERS);
        religion.Roles["custom-1"] = customRole;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        var emptyPermissions = new HashSet<string>();

        // Act
        var result = _roleManager.ModifyRolePermissions("religion-1", "founder-1", "custom-1", emptyPermissions);

        // Assert
        Assert.True(result.success);
        Assert.NotNull(result.role);
        Assert.Empty(result.role.Permissions);
    }

    #endregion

    #region AssignRole Tests

    [Fact]
    public void AssignRole_WithValidParameters_AssignsRole()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = customRole;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.AssignRole("religion-1", "founder-1", "member-1", "custom-1");

        // Assert
        Assert.True(result.success);
        Assert.Empty(result.error);
        Assert.Equal("custom-1", religion.MemberRoles["member-1"]);
        _mockReligionManager.Verify(m => m.Save(religion), Times.Once);
    }

    [Fact]
    public void AssignRole_WithNonExistentReligion_ReturnsError()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("non-existent")).Returns((ReligionData?)null);

        // Act
        var result = _roleManager.AssignRole("non-existent", "player-1", "target-1", "role-1");

        // Assert
        Assert.False(result.success);
        Assert.Equal("Religion not found", result.error);
    }

    [Fact]
    public void AssignRole_ToNonMember_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.AssignRole("religion-1", "founder-1", "non-member", "custom-1");

        // Assert
        Assert.False(result.success);
        Assert.Equal("Target player is not a member of this religion", result.error);
    }

    [Fact]
    public void AssignRole_WithoutPermission_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = customRole;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act - member-1 doesn't have MANAGE_ROLES permission
        var result = _roleManager.AssignRole("religion-1", "member-1", "member-2", "custom-1");

        // Assert
        Assert.False(result.success);
        Assert.Equal("You don't have permission to assign this role", result.error);
    }

    [Fact]
    public void AssignRole_ToFounder_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = customRole;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act - Try to assign a different role to the founder
        var result = _roleManager.AssignRole("religion-1", "founder-1", "founder-1", "custom-1");

        // Assert
        Assert.False(result.success);
        Assert.Equal("Cannot change Founder's role. Use /religion transfer instead", result.error);
    }

    [Fact]
    public void AssignRole_FounderRole_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act - Try to assign Founder role to someone
        var result = _roleManager.AssignRole("religion-1", "founder-1", "member-1", RoleDefaults.FOUNDER_ROLE_ID);

        // Assert
        Assert.False(result.success);
        // The CanAssignRole method returns false for FOUNDER_ROLE_ID before we get to the specific check
        Assert.Equal("You don't have permission to assign this role", result.error);
    }

    [Fact]
    public void AssignRole_WithNonExistentRole_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.AssignRole("religion-1", "founder-1", "member-1", "non-existent-role");

        // Assert
        Assert.False(result.success);
        Assert.Equal("You don't have permission to assign this role", result.error);
    }

    #endregion

    #region TransferFounder Tests

    [Fact]
    public void TransferFounder_WithValidParameters_TransfersFounderRole()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.TransferFounder("religion-1", "founder-1", "member-1");

        // Assert
        Assert.True(result.success);
        Assert.Empty(result.error);
        Assert.Equal(RoleDefaults.FOUNDER_ROLE_ID, religion.MemberRoles["member-1"]);
        Assert.Equal(RoleDefaults.MEMBER_ROLE_ID, religion.MemberRoles["founder-1"]);
        Assert.Equal("member-1", religion.FounderUID);
        Assert.Equal("member-1", religion.MemberUIDs[0]); // Founder should be first
        _mockReligionManager.Verify(m => m.Save(religion), Times.Once);
    }

    [Fact]
    public void TransferFounder_WithNonExistentReligion_ReturnsError()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("non-existent")).Returns((ReligionData?)null);

        // Act
        var result = _roleManager.TransferFounder("non-existent", "player-1", "player-2");

        // Assert
        Assert.False(result.success);
        Assert.Equal("Religion not found", result.error);
    }

    [Fact]
    public void TransferFounder_ByNonFounder_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act - member-1 is not a founder
        var result = _roleManager.TransferFounder("religion-1", "member-1", "member-2");

        // Assert
        Assert.False(result.success);
        Assert.Equal("Only the founder can transfer founder status", result.error);
    }

    [Fact]
    public void TransferFounder_ToNonMember_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.TransferFounder("religion-1", "founder-1", "non-member");

        // Assert
        Assert.False(result.success);
        Assert.Equal("Target player is not a member of this religion", result.error);
    }

    [Fact]
    public void TransferFounder_ToSelf_ReturnsError()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _roleManager.TransferFounder("religion-1", "founder-1", "founder-1");

        // Assert
        Assert.False(result.success);
        Assert.Equal("You are already the founder", result.error);
    }

    #endregion

    #region GetReligionRoles Tests

    [Fact]
    public void GetReligionRoles_WithValidReligion_ReturnsOrderedRoles()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole1 = new RoleData("custom-1", "Moderator", false, false, 10);
        var customRole2 = new RoleData("custom-2", "Admin", false, false, 5);
        religion.Roles["custom-1"] = customRole1;
        religion.Roles["custom-2"] = customRole2;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var roles = _roleManager.GetReligionRoles("religion-1");

        // Assert
        Assert.NotEmpty(roles);
        Assert.Equal(5, roles.Count); // 3 default + 2 custom

        // Verify ordering by DisplayOrder
        Assert.Equal(RoleDefaults.FOUNDER_ROLE_ID, roles[0].RoleUID); // DisplayOrder 0
        Assert.Equal(RoleDefaults.OFFICER_ROLE_ID, roles[1].RoleUID); // DisplayOrder 1
        Assert.Equal(RoleDefaults.MEMBER_ROLE_ID, roles[2].RoleUID); // DisplayOrder 2
        Assert.Equal("custom-2", roles[3].RoleUID); // DisplayOrder 5
        Assert.Equal("custom-1", roles[4].RoleUID); // DisplayOrder 10
    }

    [Fact]
    public void GetReligionRoles_WithNonExistentReligion_ReturnsEmptyList()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("non-existent")).Returns((ReligionData?)null);

        // Act
        var roles = _roleManager.GetReligionRoles("non-existent");

        // Assert
        Assert.Empty(roles);
    }

    #endregion

    #region GetPlayerRole Tests

    [Fact]
    public void GetPlayerRole_WithValidParameters_ReturnsRole()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var role = _roleManager.GetPlayerRole("religion-1", "founder-1");

        // Assert
        Assert.NotNull(role);
        Assert.Equal(RoleDefaults.FOUNDER_ROLE_ID, role.RoleUID);
        Assert.Equal("Founder", role.RoleName);
    }

    [Fact]
    public void GetPlayerRole_WithNonExistentReligion_ReturnsNull()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("non-existent")).Returns((ReligionData?)null);

        // Act
        var role = _roleManager.GetPlayerRole("non-existent", "player-1");

        // Assert
        Assert.Null(role);
    }

    [Fact]
    public void GetPlayerRole_ForMember_ReturnsMemberRole()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var role = _roleManager.GetPlayerRole("religion-1", "member-1");

        // Assert
        Assert.NotNull(role);
        Assert.Equal(RoleDefaults.MEMBER_ROLE_ID, role.RoleUID);
        Assert.Equal("Member", role.RoleName);
    }

    #endregion

    #region GetRoleMemberCounts Tests

    [Fact]
    public void GetRoleMemberCounts_WithValidReligion_ReturnsCounts()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = customRole;
        religion.MemberRoles["member-2"] = "custom-1"; // Change member-2 to custom role
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var counts = _roleManager.GetRoleMemberCounts("religion-1");

        // Assert
        Assert.NotEmpty(counts);
        Assert.Equal(1, counts[RoleDefaults.FOUNDER_ROLE_ID]); // founder-1
        Assert.Equal(1, counts[RoleDefaults.MEMBER_ROLE_ID]); // member-1
        Assert.Equal(1, counts["custom-1"]); // member-2
    }

    [Fact]
    public void GetRoleMemberCounts_WithNonExistentReligion_ReturnsEmptyDictionary()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("non-existent")).Returns((ReligionData?)null);

        // Act
        var counts = _roleManager.GetRoleMemberCounts("non-existent");

        // Assert
        Assert.Empty(counts);
    }

    #endregion

    #region GetPlayersWithRole Tests

    [Fact]
    public void GetPlayersWithRole_WithValidParameters_ReturnsPlayers()
    {
        // Arrange
        var religion = CreateTestReligion();
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var players = _roleManager.GetPlayersWithRole("religion-1", RoleDefaults.MEMBER_ROLE_ID);

        // Assert
        Assert.NotEmpty(players);
        Assert.Equal(2, players.Count);
        Assert.Contains("member-1", players);
        Assert.Contains("member-2", players);
    }

    [Fact]
    public void GetPlayersWithRole_WithNonExistentReligion_ReturnsEmptyList()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("non-existent")).Returns((ReligionData?)null);

        // Act
        var players = _roleManager.GetPlayersWithRole("non-existent", "role-1");

        // Assert
        Assert.Empty(players);
    }

    [Fact]
    public void GetPlayersWithRole_WithUnusedRole_ReturnsEmptyList()
    {
        // Arrange
        var religion = CreateTestReligion();
        var customRole = new RoleData("custom-1", "Moderator", false, false, 10);
        religion.Roles["custom-1"] = customRole;
        _mockReligionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var players = _roleManager.GetPlayersWithRole("religion-1", "custom-1");

        // Assert
        Assert.Empty(players);
    }

    #endregion
}