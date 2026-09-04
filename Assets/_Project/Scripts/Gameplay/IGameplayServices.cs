using UnityEngine;

public interface IItemPool
{
    void PreparePool(int requiredAmount);
    Item GetItem();
    void ReleaseItem(Item item);
}

public interface IGameStateController
{
    bool TrySetState(GameState newState);
}

public interface ICameraController
{
    void AdjustToBoard(int width, int height, float traySpace);
    float GetCameraBottomY();
}