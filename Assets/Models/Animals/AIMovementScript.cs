using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIMovementScript : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float rotSpeed = 5f;

    private bool isWandering = false;
    private bool isRotatingLeft = false;
    private bool isRotatingRight = false;
    private bool isWalking = false;
    private bool isEating = false;

    // Update is called once per frame
    void Update()
    {
        if (isWandering == false)
        {
            StartCoroutine(Wander());
        }
        if (isRotatingRight == true)
        {
            gameObject.GetComponent<Animator>().Play("Idle");
            transform.Rotate(transform.up * Time.deltaTime * rotSpeed);
        }
        if (isRotatingLeft == true)
        {
            gameObject.GetComponent<Animator>().Play("Idle");
            transform.Rotate(transform.up * Time.deltaTime * -rotSpeed);
        }
        if (isWalking == true)
        {
            gameObject.GetComponent<Animator>().Play("Walk");
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
        if (isEating == true)
        {
            gameObject.GetComponent<Animator>().Play("Eat");
        }
    }

    IEnumerator Wander()
    {
        int rotTime = Random.Range(1, 6);
        int rotateWait = Random.Range(1, 6);
        int rotateLorR = Random.Range(1, 3);
        int walkWait = Random.Range(1, 11);
        int walkTime = Random.Range(1, 6);
        int eatWait = Random.Range(1, 5);
        float eatTime = 4.5f;

        isWandering = true;
        if (Random.Range(1, 3) == 1)
        {
            yield return new WaitForSeconds(walkWait);
            isWalking = true;
            yield return new WaitForSeconds(walkTime);
            isWalking = false;

            gameObject.GetComponent<Animator>().Play("Idle");
        }


        if (Random.Range(1, 5) == 1)
        {
            yield return new WaitForSeconds(eatWait);
            isEating = true;
            yield return new WaitForSeconds(eatTime);
            isEating = false;

            gameObject.GetComponent<Animator>().Play("Idle");
        }


        if (Random.Range(1, 3) == 1)
        {
            yield return new WaitForSeconds(rotateWait);
            if (rotateLorR == 1)
            {
                isRotatingRight = true;
                yield return new WaitForSeconds(rotTime);
                isRotatingRight = false;
            }
            if (rotateLorR == 2)
            {
                isRotatingLeft = true;
                yield return new WaitForSeconds(rotTime);
                isRotatingLeft = false;
            }
        }

        isWandering = false;
    }
}