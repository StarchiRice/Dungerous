using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;

public class Follower : MonoBehaviour
{
    //Variables
    public float followSpeed;
    private float followDistance;
    public float ballFollowDistance;
    public float playerFollowDistance;

    public Vector3 followOffset;
    private Transform followTarget;

    public GameObject pickedUpObj;
    public bool isPickingUp;
    public bool isReturning;
    public Vector3 itemDropDestinationOffset;

    //References
    private PlayerController player;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        player.follower = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (player.GetComponent<EquipmentController>().lastDroppedItem != null)
        {
            isPickingUp = true;
            if (pickedUpObj == null)
            {
                pickedUpObj = player.GetComponent<EquipmentController>().lastDroppedItem;
            }
        }
        else
        {
            isPickingUp = false;
        }

        if (!isPickingUp)
        {
            Follow();
        }
        else
        {
            Pickup();
        }
    }

    void Follow()
    {
        if(player.isRolling)
        {
            followTarget = player.ballTrans;
            followDistance = ballFollowDistance;
        }
        else
        {
            followTarget = player.transform;
            followDistance = playerFollowDistance;
        }

        if(Vector3.Distance(transform.position, followTarget.transform.position + followOffset) > followDistance)
        {
            transform.position = Vector3.Slerp(transform.position, followTarget.transform.position + followOffset, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.RotateAround(followTarget.transform.position + followOffset, transform.up, 90 * Time.deltaTime);
        }
    }

    void Pickup()
    {
        if(player.GetComponent<EquipmentController>().lastDroppedItem != pickedUpObj)
        {
            isReturning = true;
        }
        //Move to pickup dropped item
        if (pickedUpObj.transform.parent != transform)
        {
            if (Vector3.Distance(transform.position, pickedUpObj.transform.position) > 0.25f)
            {
                transform.position = Vector3.Slerp(transform.position, pickedUpObj.transform.position, followSpeed * Time.deltaTime);
            }
            else
            {
                pickedUpObj.transform.parent = transform;
            }
        }
        else if (player.controls.Gameplay.FollowerCommandReturn.WasPerformedThisFrame())
        {
            //Initialize returning item to inventory when commanded
            isReturning = true;
        }
        else if(!isReturning)
        {
            //Float above player while holding item
            if (player.isRolling)
            {
                followTarget = player.ballTrans;
                followDistance = ballFollowDistance;
            }
            else
            {
                followTarget = player.transform;
                followDistance = playerFollowDistance;
            }

            if (Vector3.Distance(transform.position, followTarget.transform.position + followOffset) > followDistance)
            {
                transform.position = Vector3.Slerp(transform.position, followTarget.transform.position + followOffset, followSpeed * Time.deltaTime);
            }
            else
            {
                transform.RotateAround(followTarget.transform.position + followOffset, transform.up, 90 * Time.deltaTime);
            }
        }
        if (isReturning)
        {
            if (Vector3.Distance(transform.position, player.ballTrans.transform.position + itemDropDestinationOffset) > 1)
            {
                transform.position = Vector3.Slerp(transform.position, player.ballTrans.transform.position + itemDropDestinationOffset, followSpeed / 1.5f * Time.deltaTime);
            }
            else
            {
                if (player.GetComponent<EquipmentController>().lastDroppedItem == pickedUpObj)
                {
                    player.GetComponent<EquipmentController>().lastDroppedItem = null;
                }
                pickedUpObj.transform.parent = null;
                pickedUpObj = null;
                isReturning = false;
            }
        }
    }
}
