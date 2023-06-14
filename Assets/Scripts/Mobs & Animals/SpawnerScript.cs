using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerScript : MonoBehaviour
{
    public GameObject cowPrefab;
    public GameObject pigPrefab;
    public GameObject chickenPrefab;
    public GameObject zombiePrefab;

    public float spawnAnimalsTime;
    public float spawnMobsTime;
    private int randAnimal;
    
    private bool isDay;
    private bool isSpawning = false;
    private DayNightCycle dayNightCycle;

    // Start is called before the first frame update
    void Start()
    {
        dayNightCycle = FindObjectOfType<DayNightCycle>();
    }
    private void Update()
    {
        if (dayNightCycle.currentTimeEditorMode > 0 && dayNightCycle.currentTimeEditorMode < 750)
        {
            isDay = true;
        }
        else
        {
            isDay = false;
        }

        if (isSpawning == false)
        {
            StartCoroutine(spawn());
        }
    }

    IEnumerator spawn()
    {
        isSpawning = true;
        while (isDay)
        {
            yield return new WaitForSeconds(spawnAnimalsTime);
            randAnimal = Random.Range(0, 3);
            if (randAnimal == 0)
            {
                Instantiate(cowPrefab, transform.position, Quaternion.identity);
            }
            if (randAnimal == 1)
            {
                Instantiate(pigPrefab, transform.position, Quaternion.identity);

            }
            if (randAnimal == 2)
            {
                Instantiate(chickenPrefab, transform.position, Quaternion.identity);

            }
        }

        while (!isDay)
        {
            yield return new WaitForSeconds(spawnMobsTime);
            Instantiate(zombiePrefab, transform.position, Quaternion.identity);
        }
        isSpawning = false;
    }
}
