using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    //Variables
    public float inventoryRollSpeed;

    public float pickupRadius;
    public LayerMask whatIsPickupable;
    public Transform inventoryPoint;
    public List<GameObject> items;

    public Vector3 camOffset;
    private Vector3 ballRightDir;

    public RaycastHit inventoryRaySelect;

    //References
    public Camera inventoryCam;
    private PlayerController player;
    [HideInInspector]
    public EquipmentController equipCtrl;

    // Start is called before the first frame update
    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        player.ballTrans = transform;
    }

    void Start()
    {
        inventoryCam.transform.parent = null;
        equipCtrl = player.GetComponent<EquipmentController>();
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

        if (player.checkingInventory)
        {
            RollInventory();
        }
    }

    private void RollInventory()
    {
        Vector3 ballDir = inventoryCam.transform.TransformDirection(new Vector3(-player.moveLInput.x, -player.moveLInput.y, 0)); ;
        ballRightDir = Vector3.Cross(Vector3.forward, ballDir);
        float dist = ballDir.magnitude * Time.deltaTime;
        float alpha = (dist * 180.0f) / (Mathf.PI * 0.37f);
        inventoryPoint.transform.Rotate(ballRightDir, alpha * inventoryRollSpeed, Space.World);
    }

    public void OpenInventory()
    {
        inventoryCam.depth = 1;
    }

    public void CloseInventory()
    {
        inventoryCam.depth = -1;
    }

    public void SelectItem()
    {
        if (Physics.Raycast(inventoryCam.transform.position, inventoryCam.transform.forward, out inventoryRaySelect, 2.5f, whatIsPickupable))
        {
            equipCtrl.EquipItem(inventoryRaySelect.collider.GetComponent<BasicItemSolver>().itemID, inventoryRaySelect.collider.transform.position);
            Destroy(inventoryRaySelect.collider.gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(inventoryPoint.position, pickupRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(inventoryCam.gameObject.transform.position, inventoryCam.gameObject.transform.position + new Vector3(0, 0, 2.5f));
    }
}
