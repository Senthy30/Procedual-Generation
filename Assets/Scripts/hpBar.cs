using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class hpBar : MonoBehaviour
{
    public float healthMax = 100f;
    public float health = 100f;
    public float healthRate = 1f;
    public Slider HPBar, hungerBar, oxygenBar;

    void Start()
    {
        HPBar = GameObject.Find("hpBar").GetComponent<Slider>();
        hungerBar = GameObject.Find("hungerBar").GetComponent<Slider>();
        oxygenBar = GameObject.Find("oxygenBar").GetComponent<Slider>();


        // Set the initial value of the hunger bar
        UpdateHpBar();
    }

    void Update()
    {
        // Decrease the hunger over time

        starving();
        // Update the hunger bar display

        drowning();

        stillAlive();

        UpdateHpBar();
    }

    void starving(){
        if (hungerBar.value <= 0){
            health -= healthRate * (Time.deltaTime / 2);
        }
        UpdateHpBar();
    }

    void drowning()
    {
        if (oxygenBar.value <= 0)
        {
            health -= healthRate * (Time.deltaTime * 4);
        }
        UpdateHpBar();
    }

    public void stillAlive()
    {
        if (HPBar.value <= 0)
        {
            Debug.Log("die");
        }
    }

    public void FallDamage(float damageTaken){
        health -= damageTaken;
        UpdateHpBar();
    }

    void UpdateHpBar(){
        HPBar.value = health;
    }


}
