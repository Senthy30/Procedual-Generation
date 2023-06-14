using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIEnemyScript : MonoBehaviour
{
    public float hp = 10f;
    public float damage = 10f;
    public GameObject[] ItemsDeadState = null;

    private bool isDead = false;
    private bool isAttacking = false;
    private float deathAnimationSpeed = 5f;

    private GameObject RigidBodyFPSController;
    Vector3 wayPointPos;
    private float speed = 1.0f;
    private float waitBetweenAnim;

    // Start is called before the first frame update
    void Start()
    {
        RigidBodyFPSController = GameObject.Find("RigidBodyFPSController");
    }

    // Update is called once per frame
    void Update()
    {
        if (!isDead && !isAttacking)
        {
            if (Vector3.Distance(transform.position, RigidBodyFPSController.transform.position) > 1.1)
            {
                gameObject.GetComponent<Animator>().Play("Walk");
                transform.LookAt(new Vector3(RigidBodyFPSController.transform.position.x, transform.position.y, RigidBodyFPSController.transform.position.z));
                transform.rotation = Quaternion.Euler(
                    transform.rotation.eulerAngles.x,
                    transform.rotation.eulerAngles.y + 180,
                    transform.rotation.eulerAngles.z
                    );
                wayPointPos = new Vector3(RigidBodyFPSController.transform.position.x, transform.position.y, RigidBodyFPSController.transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, wayPointPos, speed * Time.deltaTime);
            }
        }
        else if (isDead && !isAttacking)
        {
            gameObject.GetComponent<Animator>().Play("Idle");

            Quaternion targetQuaternion = Quaternion.Euler(0, 0, 90);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetQuaternion, deathAnimationSpeed * Time.deltaTime);
        }
        else if (!isDead && isAttacking)
        {
            gameObject.GetComponent<Animator>().Play("Attack");
        }

        if (waitBetweenAnim >= 0)
            waitBetweenAnim -= Time.deltaTime;
        if (waitBetweenAnim < 0)
            isAttacking = false;
    }

    private void OnMouseDown()
    {
        if (!isDead)
        {
            if (hp > 0)
            {
                hp -= 1;
            }

            if (hp == 0)
            {
                isDead = true;
                Invoke("ShowItemsDeadState", 3f);
            }

        }
    }

    private void ShowItemsDeadState()
    {
        foreach (var item in ItemsDeadState)
        {
            item.SetActive(true);
        }

        Destroy(GetComponent<BoxCollider>());

        transform.Find("mesh").GetComponent<SkinnedMeshRenderer>().enabled = false;
    }

    private void OnCollisionStay(Collision collision)
    {   
        if (collision.collider.gameObject.CompareTag("Player"))
        {
            isAttacking = true;
            waitBetweenAnim = 0.5f;
        }
    }
}
