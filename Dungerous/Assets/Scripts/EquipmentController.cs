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

    //References
    public Transform itemPoint;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
}
