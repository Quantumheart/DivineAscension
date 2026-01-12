using System;
using System.Collections.Generic;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems.Networking.Client;

public class UiService(DivineAscensionNetworkClient networkClient)
    : IUiService
{
    private readonly DivineAscensionNetworkClient _networkClient =
        networkClient ?? throw new ArgumentNullException(nameof(networkClient));

    public void RequestBlessingData()
    {
        _networkClient.RequestBlessingData();
    }

    public void RequestBlessingUnlock(string blessingId)
    {
        _networkClient.RequestBlessingUnlock(blessingId);
    }

    public void RequestReligionList(string deityFilter = "")
    {
        _networkClient.RequestReligionList(deityFilter);
    }

    public void RequestPlayerReligionInfo()
    {
        _networkClient.RequestPlayerReligionInfo();
    }

    public void RequestReligionAction(string action, string religionUID = "", string targetPlayerUID = "")
    {
        _networkClient.RequestReligionAction(action, religionUID, targetPlayerUID);
    }

    public void RequestCreateReligion(string religionName, string domain, string deityName, bool isPublic)
    {
        _networkClient.RequestCreateReligion(religionName, domain, deityName, isPublic);
    }

    public void RequestEditDescription(string religionUID, string description)
    {
        _networkClient.RequestEditDescription(religionUID, description);
    }

    public void RequestSetDeityName(string religionUID, string newDeityName)
    {
        _networkClient.SendSetDeityNameRequest(religionUID, newDeityName);
    }

    public void RequestReligionDetail(string religionUID)
    {
        _networkClient.RequestReligionDetail(religionUID);
    }

    public void RequestReligionRoles(string religionUID)
    {
        _networkClient.RequestReligionRoles(religionUID);
    }

    public void RequestCreateRole(string religionUID, string roleName)
    {
        _networkClient.RequestCreateRole(religionUID, roleName);
    }

    public void RequestModifyRolePermissions(string religionUID, string roleUID, HashSet<string> permissions)
    {
        _networkClient.RequestModifyRolePermissions(religionUID, roleUID, permissions);
    }

    public void RequestDeleteRole(string religionUID, string roleUID)
    {
        _networkClient.RequestDeleteRole(religionUID, roleUID);
    }

    public void RequestAssignRole(string religionUID, string targetPlayerUID, string roleUID)
    {
        _networkClient.RequestAssignRole(religionUID, targetPlayerUID, roleUID);
    }

    public void RequestTransferFounder(string religionUID, string newFounderUID)
    {
        _networkClient.RequestTransferFounder(religionUID, newFounderUID);
    }

    public void RequestCivilizationList(string deityFilter = "")
    {
        _networkClient.RequestCivilizationList(deityFilter);
    }

    public void RequestCivilizationInfo(string civId)
    {
        _networkClient.RequestCivilizationInfo(civId);
    }

    public void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "",
        string icon = "")
    {
        _networkClient.RequestCivilizationAction(action, civId, targetId, name, icon);
    }

    public void RequestDiplomacyInfo(string civId)
    {
        _networkClient.RequestDiplomacyInfo(civId);
    }

    public void RequestDiplomacyAction(string action, string targetCivId = "", string proposalOrRelationshipId = "",
        string proposedStatus = "")
    {
        _networkClient.RequestDiplomacyAction(action, targetCivId, proposalOrRelationshipId, proposedStatus);
    }
}