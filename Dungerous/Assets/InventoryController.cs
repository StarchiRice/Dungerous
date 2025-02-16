using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    //Variables
    public float pickupRadius;
    public LayerMask whatIsPickupable;
    public Transform inventoryPoint;
    public List<GameObject> items;

    public Vector3 camOffset;

    //References
    public Camera inventoryCam;


    // Start is called before the first frame update
    void Start()
    {
        inventoryCam.transform.parent = null;
    }

    // Update is called once per frame
    void Update()
    {
        Collider[] pickedUpItems = Physics.OverlapSphere(inventoryPoint.position, pickupRadius, whatIsPickupable);
        for (int i = 0; i < pickedUpItems.Length; i++)
        {
            if (items.Contains(pickedUpItems[i].gameObject) == false)
            {
                items.Add(pickedUpItems[i].gameObject);
                pickedUpItems[i].gameObject.transform.parent = inventoryPoint;
            }
        }

        inventoryCam.transform.position = transform.position + camOffset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(inventoryPoint.position, pickupRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(inventoryCam.gameObject.transform.position, inventoryCam.gameObject.transform.position + new Vector3(0, 0, 2.5f));
    }
}
