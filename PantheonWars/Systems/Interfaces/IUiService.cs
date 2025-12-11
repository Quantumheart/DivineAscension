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

    // Civilization Operations
    void RequestCivilizationList(string deityFilter = "");
    void RequestCivilizationInfo(string civId);
    void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "");
}