public interface IItemPool
{
    void PreparePool(int count);
    Item GetItem();
    void ReleaseItem(Item item);
}