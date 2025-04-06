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

    public Vector3 lookTarget;
    private Vector3 modelAngleDest;
    public float modelRotateSpeed;

    public GameObject pickedUpObj;
    public bool isPickingUp;
    public bool isReturning;
    public bool isStandby;
    public Vector3 itemDropDestinationOffset;

    //References
    private PlayerController player;
    public TrailRenderer trail;
    public GameObject model;

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
            if(isPickingUp && followTarget == null)
            {
                isPickingUp = false;
            }
            Pickup();
        }
    }

    void Follow()
    {
        trail.emitting = true;
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
        lookTarget = followTarget.position + followOffset;
        if (Vector3.Distance(transform.position, followTarget.transform.position + followOffset) > followDistance)
        {
            transform.position = Vector3.Slerp(transform.position, followTarget.transform.position + followOffset, followSpeed * Time.deltaTime);
            modelAngleDest = Vector3.zero;
        }
        else
        {
            transform.RotateAround(followTarget.transform.position + followOffset, transform.up, 90 * Time.deltaTime);
            modelAngleDest = new Vector3(0, -90, 0);
        }
        transform.LookAt(lookTarget, Vector3.up);
        model.transform.localEulerAngles = new Vector3(0, Mathf.MoveTowardsAngle(model.transform.localEulerAngles.y, modelAngleDest.y, modelRotateSpeed * Time.deltaTime), 0);
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
            lookTarget = pickedUpObj.transform.position;
            modelAngleDest = Vector3.zero;
            trail.emitting = true;
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
            transform.parent = null;
            isReturning = !isReturning;
            isStandby = false;
        }
        else if (player.controls.Gameplay.FollowerCommandStandby.WasPerformedThisFrame())
        {
            transform.parent = null;
            isStandby = !isStandby;
            isReturning = false;
        }
        else if(!isReturning && !isStandby)
        {
            //Float above player while holding item
            transform.parent = null;
            trail.emitting = true;
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
            lookTarget = followTarget.position + followOffset;
            if (Vector3.Distance(transform.position, followTarget.transform.position + followOffset) > followDistance)
            {
                transform.position = Vector3.Slerp(transform.position, followTarget.transform.position + followOffset, followSpeed * Time.deltaTime);
                modelAngleDest = Vector3.zero;
            }
            else
            {
                transform.RotateAround(followTarget.transform.position + followOffset, transform.up, 90 * Time.deltaTime);
                modelAngleDest = new Vector3(0, -90, 0);
            }
        }
        if (isReturning)
        {
            transform.parent = null;
            isStandby = false;
            lookTarget = player.ballTrans.position;
            modelAngleDest = Vector3.zero;
            trail.emitting = true;
            if (Vector3.Distance(transform.position, player.ballTrans.transform.position + itemDropDestinationOffset) > 1)
            {
                transform.position = Vector3.Slerp(transform.position, player.ballTrans.transform.position + itemDropDestinationOffset, followSpeed / 1.5f * Time.deltaTime);
            }
            else
            {
                pickedUpObj.transform.parent = null;
                if (player.GetComponent<EquipmentController>().lastDroppedItem == pickedUpObj)
                {
                    player.GetComponent<EquipmentController>().lastDroppedItem = null;
                }
                pickedUpObj = null;
                isReturning = false;
            }
        }
        else if(isStandby)
        {
            lookTarget = player.cam.followerStandbyPoint.position;
            modelAngleDest = Vector3.zero;
            if (Vector3.Distance(transform.position, player.cam.followerStandbyPoint.position) > 2)
            {
                transform.LookAt(lookTarget, Vector3.up);
                trail.emitting = true;
                transform.position = Vector3.Slerp(transform.position, player.cam.followerStandbyPoint.position, followSpeed * 3 * Time.deltaTime);
            }
            else
            {
                transform.Rotate(Vector3.up, 60 * Time.deltaTime);
                trail.emitting = false;
                transform.parent = player.cam.followerStandbyPoint;
                transform.localPosition = Vector3.zero;
            }
        }
        if (!isStandby)
        {
            transform.LookAt(lookTarget, Vector3.up);
        }
        model.transform.localEulerAngles = new Vector3(0, Mathf.MoveTowardsAngle(model.transform.localEulerAngles.y, modelAngleDest.y, modelRotateSpeed * Time.deltaTime), 0);
    }
}
