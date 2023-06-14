using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObiectInventar : MonoBehaviour, IInventoryItem
{
    public string _Name = null;
    public string Name
    {
        get
        {
            return _Name;
        }
    }


    public Sprite _Image = null;
    
    public Sprite Image
    {
        get
        {
            return _Image;
        }
    }

    public void OnPickup()
    {
        gameObject.SetActive(false);
    }
}
