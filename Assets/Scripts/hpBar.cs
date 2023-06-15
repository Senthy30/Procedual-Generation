using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

public class hpBar : MonoBehaviour
{
    public float healthMax = 100f;
    public float health = 100f;
    public float healthRate = 1f;
    public Slider HPBar, hungerBar, oxygenBar, energyBar;
    public HungerBar hungerBarScript;
    public GameObject player;

    private Button respawn;

    void Start()
    {
        // HPBar = GameObject.Find("hpBar").GetComponent<Slider>();
        // hungerBar = GameObject.Find("hungerBar").GetComponent<Slider>();
        // oxygenBar = GameObject.Find("oxygenBar").GetComponent<Slider>();
        //energyBar = GameObject.Find("energyBar").GetComponent<Slider>();
        respawn = GameObject.Find("respawnButton").GetComponent<Button>();

        respawn.gameObject.SetActive(false);

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
            Time.timeScale = 0f;
            respawn.gameObject.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void Respawn()
    {
        health = 100;
        HPBar.value = health;

        GameObject.Find("hungerBar").GetComponent<Slider>().value = 100;

        Debug.Log(energyBar.value);

        float maxValue = 100;

        oxygenBar.value = maxValue;
        energyBar.value = maxValue;

        hungerBarScript.hunger = maxValue;
        player.GetComponent<RigidbodyFirstPersonController>().Energy = maxValue;
        player.GetComponent<RigidbodyFirstPersonController>().UnderWater = maxValue;

        Time.timeScale = 1f;
        Cursor.visible = false;
        respawn.gameObject.SetActive(false);

        Vector3 vector = new Vector3(0f, 35f, 0f);

        player.GetComponent<Rigidbody>().velocity = Vector3.zero;
        player.GetComponent<Rigidbody>().position = vector;

    }

    public void FallDamage(float damageTaken){
        health -= 100;
        UpdateHpBar();
    }

    void UpdateHpBar(){
        HPBar.value = health;
    }


}
