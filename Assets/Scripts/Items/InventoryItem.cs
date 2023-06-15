using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInventoryItem
{
    string Name { get; }

    Sprite Image { get; }

    void OnPickup();

    void OnDrop();
}
public class InventoryEventArgs : EventArgs
{
    public InventoryEventArgs(IInventoryItem item, int slotIndex)
    {
        Item = item;
        SlotIndex = slotIndex;
    }

    public IInventoryItem Item;
    public int SlotIndex;
}

