using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileItem : MonoBehaviour
{
    //Variables
    public float throwForce;
    public Transform shootPoint;
    public bool consumableItem;
    
    //References
    public GameObject projObj;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShootProjectile(Vector3 shootDirection)
    {
        GameObject curProj = Instantiate(projObj, shootPoint.position, Quaternion.identity, null);
        curProj.GetComponent<Rigidbody>().AddForce(shootDirection * throwForce, ForceMode.Impulse);
        if(consumableItem)
        {
            Destroy(gameObject);
        }
    }
}
