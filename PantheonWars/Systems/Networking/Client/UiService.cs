using System;
using PantheonWars.Systems.Interfaces;

namespace PantheonWars.Systems.Networking.Client;

public class UiService(PantheonWarsNetworkClient networkClient)
    : IUiService
{
    private readonly PantheonWarsNetworkClient _networkClient =
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

    public void RequestCreateReligion(string religionName, string deity, bool isPublic)
    {
        _networkClient.RequestCreateReligion(religionName, deity, isPublic);
    }

    public void RequestEditDescription(string religionUID, string description)
    {
        _networkClient.RequestEditDescription(religionUID, description);
    }

    public void RequestCivilizationList(string deityFilter = "")
    {
        _networkClient.RequestCivilizationList(deityFilter);
    }

    public void RequestCivilizationInfo(string civId)
    {
        _networkClient.RequestCivilizationInfo(civId);
    }

    public void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "")
    {
        _networkClient.RequestCivilizationAction(action, civId, targetId, name);
    }
}