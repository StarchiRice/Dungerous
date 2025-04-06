using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlat : MonoBehaviour
{
    //Variables
    public float moveSpeed, waitTime;
    public int curDestIndex;
    private float curWaitTime;

    public Transform rideDetectPoint;
    public LayerMask whatCanRide;
    public Vector3 extents;
    public Collider playerCol;

    //References
    public Transform[] destinationPoints;
    public Rigidbody platform;
    public GameObject playerPlat;

    // Start is called before the first frame update
    void Start()
    {
        playerPlat.transform.parent = null;
    }

    // Update is called once per frame
    void Update()
    {
        MovePlatform();
    }

    public void MovePlatform()
    {
        if (Physics.CheckBox(rideDetectPoint.position, extents, Quaternion.identity, whatCanRide))
        {
            Collider[] ridingColliders = Physics.OverlapBox(rideDetectPoint.position, extents, Quaternion.identity, whatCanRide);
            if (ridingColliders.Length > 0)
            {
                playerCol = ridingColliders[0];
                playerCol.transform.parent = playerPlat.transform;
            }
        }
        else if(playerCol != null)
        {
            playerCol.transform.parent = null;
            playerCol = null;
        }
        if (Vector3.Distance(platform.transform.position, destinationPoints[curDestIndex].position) <= 0.1f)
        {
            if (curWaitTime > 0)
            {
                curWaitTime -= Time.deltaTime;
            }
            else
            {
                if (curDestIndex < destinationPoints.Length-1)
                {
                    curDestIndex++;
                }
                else
                {
                    curDestIndex = 0;
                }
                curWaitTime = 0;
            }
        }
        else
        {
            platform.MovePosition(platform.position + (destinationPoints[curDestIndex].position - platform.position).normalized * Time.fixedDeltaTime * moveSpeed);
            playerPlat.transform.position = Vector3.MoveTowards(playerPlat.transform.position, platform.position, Time.deltaTime * moveSpeed);
            curWaitTime = waitTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(platform.transform.position, destinationPoints[curDestIndex].position);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(rideDetectPoint.position, extents * 2);
    }
}
