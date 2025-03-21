using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DraftLiftController : MonoBehaviour
{
    //Variables
    public float liftSpeed;

    public float liftRadius;
    public float liftHeight;
    private Vector3 liftHeightMax;

    public float flightDuration;
    private float curLingerTime;
    private Collider playerCol;

    //References
    public LayerMask whatToLift;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        liftHeightMax = new Vector3(transform.position.x, transform.position.y + liftHeight, transform.position.z);
        Collider[] liftCol = Physics.OverlapCapsule(transform.position, liftHeightMax, liftRadius, whatToLift);
        
        if(liftCol.Length > 0)
        {
            playerCol = liftCol[0];
            playerCol.GetComponent<PlayerController>().draftLifted = false;
            curLingerTime = flightDuration;
            if (liftCol[0].GetComponent<PlayerController>().curSwordSpinDuration > 0)
            {
                
                    liftCol[0].GetComponent<CharacterController>().Move(Vector3.up * liftSpeed * Time.deltaTime);
                
                liftCol[0].GetComponent<PlayerController>().curSwordSpinDuration = flightDuration;
            }
        }
        else if(playerCol != null)
        {
            playerCol.GetComponent<PlayerController>().draftLifted = true;
            curLingerTime -= Time.deltaTime;
            if(curLingerTime <= 0)
            {
                playerCol = null;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y + liftHeight, transform.position.z), liftRadius);
    }
}
