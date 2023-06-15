using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private const int SLOTS = 9;

    private IInventoryItem[] mItems = new IInventoryItem[10];

    public event EventHandler<InventoryEventArgs> ItemAdded;
    public event EventHandler<InventoryEventArgs> ItemRemoved;
    //public event EventHandler<InventoryEventArgs> ItemUsed;

    public IInventoryItem[] GetItems()
    {
        return mItems;
    }

    public void SetItems(IInventoryItem[] newItems)
    {
        mItems = newItems;
    }

    public void AddItem(IInventoryItem item)
    {
        if (Array.Exists(mItems, element => element == null))
        {
            Collider collider = (item as MonoBehaviour).GetComponent<Collider>();
            if (collider.enabled)
            {
                collider.enabled = false;

                int nullIndex = Array.FindIndex(mItems, element => element == null);
                mItems[nullIndex] = item;
                if (nullIndex != -1)
                {
                    mItems[nullIndex] = item;
                }
                item.OnPickup();

                if (ItemAdded != null)
                {
                    ItemAdded(this, new InventoryEventArgs(item,0));
                }
            }
        }
    }
    public void RemoveItem(IInventoryItem item, int slotNumber)
    {
        mItems[slotNumber - 1] = null;
        item.OnDrop();
        Collider collider = (item as MonoBehaviour).GetComponent<Collider>();
        if (collider)
        { 
            collider.enabled = true; 
        }
         if (ItemRemoved != null)
         {
            ItemRemoved(this, new InventoryEventArgs(item,slotNumber));
         }
    
    }
    /*
        public Inventory()
        {
            for (int i = 0; i < SLOTS; i++)
            {
                mSlots.Add(new InventorySlot(i));
            }
        }

        private InventorySlot FindStackableSlot(InventoryItemBase item)
        {
            foreach (InventorySlot slot in mSlots)
            {
                if (slot.IsStackable(item))
                    return slot;
            }
            return null;
        }

        private InventorySlot FindNextEmptySlot()
        {
            foreach (InventorySlot slot in mSlots)
            {
                if (slot.IsEmpty)
                    return slot;
            }
            return null;
        }



        internal void UseItem(InventoryItemBase item)
        {
            if (ItemUsed != null)
            {
                ItemUsed(this, new InventoryEventArgs(item));
            }

            item.OnUse();
        }


        } */
}