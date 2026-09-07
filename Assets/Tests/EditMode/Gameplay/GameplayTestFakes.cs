using UnityEngine;

public class FakeItemPool : IItemPool
{
    public int PreparedCount { get; private set; }
    public int RequestedItemsCount { get; private set; }
    public int ReleasedItemsCount { get; private set; }

    public void PreparePool(int count)
    {
        PreparedCount = count;
    }

    public Item GetItem()
    {
        RequestedItemsCount++;
        var go = new GameObject("FakeItem");
        return go.AddComponent<Item>();
    }

    public void ReleaseItem(Item item)
    {
        ReleasedItemsCount++;
        if (item != null)
        {
            Object.DestroyImmediate(item.gameObject);
        }
    }
}

public class FakeCameraController : ICameraController
{
    public bool AdjustToBoardCalled { get; private set; }
    public float MockBottomY { get; set; } = -5f;

    public void AdjustToBoard(int width, int height, float traySpace)
    {
        AdjustToBoardCalled = true;
    }

    public float GetCameraBottomY() => MockBottomY;
}

public class FakeGameStateController : IGameStateController
{
    public GameState CurrentState { get; private set; } = GameState.Playing;
    public GameState? LastRequestedState { get; private set; }

    public bool TrySetState(GameState newState)
    {
        LastRequestedState = newState;
        CurrentState = newState;
        return true;
    }
}