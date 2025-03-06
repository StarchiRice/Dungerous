using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombController : MonoBehaviour
{
    //Variables
    public float explosionForce;
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
            Destroy(affectCol[i].gameObject);
        }
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explodeRadius);
    }
}
