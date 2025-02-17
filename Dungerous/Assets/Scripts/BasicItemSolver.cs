using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicItemSolver : MonoBehaviour
{
    //Variables
    public int itemID;
    //1: sword

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.parent != null)
        {
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
