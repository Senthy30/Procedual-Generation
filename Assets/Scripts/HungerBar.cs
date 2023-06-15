using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HungerBar : MonoBehaviour
{
    public float maxHunger = 100f;
    public float hunger = 100f;
    public float hungerRate = 1f;
    public Slider hungerBar;

    void Start(){
        // hungerBar = GameObject.Find("hungerBar").GetComponent<Slider>();
        // Set the initial value of the hunger bar
        UpdateHungerBar();
    }

    void Update(){
        // Decrease the hunger over time
        hunger -= hungerRate * (Time.deltaTime / 4);

        // Ensure hunger value is within range
        if (hunger < 0){
            hunger = 0;
        }
        else if (hunger > maxHunger){
            hunger = maxHunger;
        }

        // Update the hunger bar display
        UpdateHungerBar();
    }

    void UpdateHungerBar(){
        hungerBar.value = hunger;
    }
        

   
    void Eat(float foodValue){
        hungerBar.value += foodValue;
    }
}
