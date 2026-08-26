using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ItemPool : MonoBehaviour
{
    public static ItemPool Instance { get; private set; }

    private ObjectPool<Item> pool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        pool = new ObjectPool<Item>(
            createFunc: CreateItem,
            actionOnGet: OnGetItem,
            actionOnRelease: OnReleaseItem,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 500
        );
    }

    private Item CreateItem()
    {
        GameObject go = new GameObject("Pooled_Item");
        go.transform.SetParent(transform);
        return go.AddComponent<Item>();
    }

    private void OnGetItem(Item item)
    {
        item.gameObject.SetActive(true);
    }

    private void OnReleaseItem(Item item)
    {
        item.StopPulse();
        item.inTray = false;
        if (item.TryGetComponent(out Collider2D col)) col.enabled = true;
        item.gameObject.SetActive(false);
        item.transform.SetParent(transform);
    }

    private void OnDestroyItem(Item item)
    {
        Destroy(item.gameObject);
    }

    // Instances in memory objects required before playing the level
    public void PreparePool(int requiredAmount)
    {
        if (pool.CountInactive >= requiredAmount) return;

        List<Item> tempItems = new List<Item>();

        // Extract until reach the total amount required (creats more if necessary)
        while (tempItems.Count < requiredAmount)
        {
            tempItems.Add(pool.Get());
        }

        // Put everything back to the pool
        foreach (Item item in tempItems)
        {
            pool.Release(item);
        }
    }

    public Item GetItem() => pool.Get();
    public void ReleaseItem(Item item) => pool.Release(item);
}