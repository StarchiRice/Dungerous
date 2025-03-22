using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyHealth : MonoBehaviour
{
    //Variables
    public float maxHealth, curHealth, curHitStun;
    public Vector3 curHitDir;
    public int lastHitID;
    public Transform lastHitOrigin;
    public float curKnockbackFalloff;
    public float knockbackFalloff;

    //References
    public GameObject model;
    private CharacterController charCtrl;

    // Start is called before the first frame update
    void Start()
    {
        curHealth = maxHealth;
        charCtrl = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        if(curHealth <= 0 && curHitStun <= 0.5f)
        {
            Die();
        }
        if (curHitStun > 0)
        {
            charCtrl.Move(new Vector3(curHitDir.x, 0, curHitDir.z) * 60 * curKnockbackFalloff * Mathf.Clamp(curHitStun, 0, 1) * Time.deltaTime);
            charCtrl.Move(Physics.gravity * Time.deltaTime);
            curHitStun -= Time.deltaTime;
            if (curKnockbackFalloff > 0)
            {
                curKnockbackFalloff -= Time.deltaTime;
            }
            else
            {
                curKnockbackFalloff = 0;
            }
            model.transform.localRotation = Quaternion.Euler(new Vector3(-15, 0, 0));
        }
        else
        {
            curHitStun = 0;
            model.transform.localRotation = Quaternion.identity;
        }
    }

    public void TakeDamage(float dmgTaken, float hitStun, Vector3 hitDir, int hitID, Transform hitOrigin)
    {
        if (curHealth > 0)
        {
            curHealth -= dmgTaken;
            curHitStun = hitStun;
            curHitDir = hitDir;
            lastHitID = hitID;
            curKnockbackFalloff = knockbackFalloff;
            lastHitOrigin = hitOrigin;
        }
    }

    public void Die()
    {
        transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.zero, 5 * Time.deltaTime);
        if (transform.localScale.magnitude <= 0.3f)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if(curHitStun > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, curHitDir);
        }
    }
}
