using System.Collections.Generic;

namespace PantheonWars.Systems.Interfaces;

public interface IUiService
{
    // Blessing Operations
    void RequestBlessingData();
    void RequestBlessingUnlock(string blessingId);

    // Religion Operations
    void RequestReligionList(string deityFilter = "");
    void RequestPlayerReligionInfo();
    void RequestReligionAction(string action, string religionUID = "", string targetPlayerUID = "");
    void RequestCreateReligion(string religionName, string deity, bool isPublic);
    void RequestEditDescription(string religionUID, string description);

    // Religion Roles Operations
    void RequestReligionRoles(string religionUID);
    void RequestCreateRole(string religionUID, string roleName);
    void RequestModifyRolePermissions(string religionUID, string roleUID, HashSet<string> permissions);
    void RequestDeleteRole(string religionUID, string roleUID);
    void RequestAssignRole(string religionUID, string targetPlayerUID, string roleUID);
    void RequestTransferFounder(string religionUID, string newFounderUID);

    // Civilization Operations
    void RequestCivilizationList(string deityFilter = "");
    void RequestCivilizationInfo(string civId);

    void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "",
        string icon = "");
}