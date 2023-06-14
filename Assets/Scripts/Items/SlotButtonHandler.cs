using UnityEngine;
using UnityEngine.UI;

public class SlotButtonHandler : MonoBehaviour
{
    public int slotNumber;
    private Image slotImage;
    private static SlotButtonHandler selectedSlot = null;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1 + slotNumber - 1))
        {
            SelectSlot();
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