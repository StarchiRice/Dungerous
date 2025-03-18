using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentController : MonoBehaviour
{
    //Variables
    public int curItemID;
    //0: none
    //1: sword

    public GameObject curEquip;

    public GameObject[] itemPrefabs;

    public Vector3 lastItemPosition;

    public Transform pickupPoint;
    public float pickupRadius;
    public LayerMask whatIsPickupable;

    //References
    public Transform itemPoint;
    private PlayerController player;

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if(curEquip == null)
        {
            curItemID = 0;
        }

        if (Physics.CheckSphere(pickupPoint.position, pickupRadius, whatIsPickupable))
        {
            if (curEquip == null)
            {
                Collider pickupItem = Physics.OverlapSphere(pickupPoint.position, pickupRadius, whatIsPickupable)[0];
                if(pickupItem.transform.parent == null)
                {
                    EquipItem(pickupItem.GetComponent<BasicItemSolver>().itemID, lastItemPosition);
                    Destroy(pickupItem.gameObject);
                }
            }
        }
    }

    public void UseItem()
    {
        if (curItemID == 0)
        {
            //Shove
            player.Shove();
        }
        else if (curItemID == 1)
        {
            //Swing Sword
            player.SwingSword(false);
        }
        else if(curItemID == 2)
        {
            //Throw Bomb
            player.StartCoroutine(player.ThrowBomb());
        }
    }

    public void EquipItem(int itemID, Vector3 lastItemPos)
    {
        lastItemPosition = lastItemPos;
        DropItem(lastItemPosition);

        curEquip = Instantiate(itemPrefabs[itemID], itemPoint, false);
        curItemID = itemID;
        Debug.Log(itemPrefabs[curItemID].name + " WAS EQUIPPED!");
    }

    public void DropItem(Vector3 dropPosition)
    {
        if (curItemID != 0)
        {
            Instantiate(curEquip.GetComponent<BasicEqupmentSolver>().dropItem, dropPosition, Quaternion.identity, null);
            Destroy(curEquip);
            curItemID = 0;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pickupPoint.position, pickupRadius);
    }
}
