using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Water : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && other.GetComponent<RigidbodyFirstPersonController>() != null)
        {
            RigidbodyFirstPersonController movement = other.GetComponent<RigidbodyFirstPersonController>();
            movement.isSwimming = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {

        if (other.CompareTag("Player") && other.GetComponent<RigidbodyFirstPersonController>() != null)
        {
            RigidbodyFirstPersonController movement = other.GetComponent<RigidbodyFirstPersonController>();
            movement.isSwimming = false;
        }
    }
}
