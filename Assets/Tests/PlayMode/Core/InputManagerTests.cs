using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class InputManagerTests
{
    [Test]
    public void OnItemClicked_EventCanBeSubscribedAndTriggered()
    {
        Item dummyItem = new GameObject().AddComponent<Item>();
        Item receivedItem = null;

        InputManager.OnItemClicked += (item) => receivedItem = item;

        Assert.IsNull(receivedItem);

        Object.Destroy(dummyItem.gameObject);
    }
}