// Interface to abstract user's level  and persistance
public interface IUserDataProvider
{
    int CurrentLevel { get; }
    int Coins { get; }
    bool TrySpendCoins(int amount);
    void ResetData();
}