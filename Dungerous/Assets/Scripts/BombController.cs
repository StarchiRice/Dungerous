using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombController : MonoBehaviour
{
    //Variables
    public float explosionDamage;
    public float fuseTime;
    private float curFuseTime;

    public float explodeRadius;
    public LayerMask whatCanExplode;

    //References
    public GameObject explosionEffect;

    // Start is called before the first frame update
    void Start()
    {
        curFuseTime = fuseTime;
    }

    // Update is called once per frame
    void Update()
    {
        if(curFuseTime > 0)
        {
            curFuseTime -= Time.deltaTime;
        }
        else
        {
            Explode();
        }
    }

    public void Explode()
    {
        Instantiate(explosionEffect, transform.position, Quaternion.identity, null);
        Collider[] affectCol = Physics.OverlapSphere(transform.position, explodeRadius, whatCanExplode);
        for (int i = 0; i < affectCol.Length; i++)
        {
            if(affectCol[i].GetComponent<EnemyHealth>() != null)
            {
                affectCol[i].GetComponent<EnemyHealth>().TakeDamage(explosionDamage, 1.5f, (affectCol[i].transform.position - transform.position).normalized, Random.Range(1, 1000), transform);
            }
            else
            {
                Destroy(affectCol[i].gameObject);
            }
        }
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explodeRadius);
    }
}
