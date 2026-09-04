public interface IUserDataService
{
    void SaveProfile(UserProfile profile);
    UserProfile LoadProfile();
    bool HasProfile();
}