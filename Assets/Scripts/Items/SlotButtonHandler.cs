using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SlotButtonHandler : MonoBehaviour
{
    public int slotNumber;
    private Image slotImage;
    private static SlotButtonHandler selectedSlot = null;
    private Inventory Inventory;
    public HungerBar hungerBar;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
        Inventory = FindObjectOfType<Inventory>();
        hungerBar = FindObjectOfType<HungerBar>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1 + slotNumber - 1))
        {
            SelectSlot();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if( slotImage.color == Color.red)
            {
                IInventoryItem[] items = Inventory.GetItems();          
                int s = slotNumber;                  
                Inventory.RemoveItem(items[slotNumber-1], s);
                selectedSlot = null;
                slotImage.color = Color.white;
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (slotImage.color == Color.red)
            {
                IInventoryItem[] items = Inventory.GetItems();
                int s = slotNumber;
                if(items[slotNumber - 1].Name == "Mushroom")
                {
                    print("Am mancat ciuperca !");
                    Debug.Log("Nivel HungerBar " + hungerBar.hunger);
                    hungerBar.hunger = 1f;
                    hungerBar.UpdateHungerBar();
                    Inventory.RemoveItem(items[slotNumber - 1], s);
                }
                selectedSlot = null;
                slotImage.color = Color.white;
            }
        }
    }

    private void SelectSlot()
    {
        if (selectedSlot != null)
        {
            selectedSlot.slotImage.color = Color.white;
        }

        if (selectedSlot == this)
        {        
            selectedSlot = null;
        }
        else
        {
            slotImage.color = Color.red;
            selectedSlot = this;
        }
    }
}