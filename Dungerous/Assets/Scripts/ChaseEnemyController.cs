using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChaseEnemyController : MonoBehaviour
{
    //Variables
    public Transform target;
    public float detectRadius, ballDetectRadius;

    private float chaseToDistance;
    public float playerChaseToDistance, ballChaseToDistance;

    public float chaseSpeed;
    public float rotateSpeed;

    public bool playerOriented;

    //References
    public LayerMask whatIsTarget, whatIsBall;
    private CharacterController charCtrl;
    private EnemyHealth healthCtrl;

    // Start is called before the first frame update
    void Start()
    {
        charCtrl = GetComponent<CharacterController>();
        healthCtrl = GetComponent<EnemyHealth>();
    }

    // Update is called once per frame
    void Update()
    {
        Collider[] detectCol = Physics.OverlapSphere(transform.position, detectRadius, whatIsTarget);
        Collider[] ballCol = Physics.OverlapSphere(transform.position, ballDetectRadius, whatIsBall);

        
        if(detectCol.Length > 0 && ballCol.Length > 0 && !playerOriented)
        {
            //Determines the target based on distance when no target preference specified
            if(Vector3.Distance(transform.position, detectCol[0].transform.position) < Vector3.Distance(transform.position, ballCol[0].transform.position))
            {
                target = detectCol[0].transform;
                chaseToDistance = playerChaseToDistance;
            }
            else
            {
                target = ballCol[0].transform;
                chaseToDistance = ballChaseToDistance;
            }
        }
        else if (detectCol.Length > 0)
        {
            //Prioritizes targeting player when set to player oriented
            target = detectCol[0].transform;
            chaseToDistance = playerChaseToDistance;
        }
        else
        {
            if (ballCol.Length > 0)
            {
                target = ballCol[0].transform;
                chaseToDistance = ballChaseToDistance;
            }
            else
            {
                target = null;
            }
            
        }

        if(healthCtrl.curHitStun <= 0 && healthCtrl.curHealth > 0)
        {
            if (target != null)
            {
                Chase();
            }
            else
            {
                Idle();
            }
        }
        else
        {
            if (healthCtrl.lastHitOrigin != null)
            {
                Vector3 hitOriginVector = (healthCtrl.lastHitOrigin.position - transform.position).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(hitOriginVector.x, 0, hitOriginVector.z)), rotateSpeed * Time.deltaTime);
            }
        }
    }

    public void Chase()
    {
        Vector3 chaseDir = (target.position - transform.position).normalized;
        if (Vector3.Distance(transform.position, target.position) > chaseToDistance)
        {
            charCtrl.Move(new Vector3(chaseDir.x * chaseSpeed, Physics.gravity.y, chaseDir.z * chaseSpeed) * Time.deltaTime);
        }
        else
        {
            //When at min chase distance
            Idle();
        }
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(chaseDir.x, 0, chaseDir.z)), rotateSpeed * Time.deltaTime);
    }

    public void Idle()
    {
        charCtrl.Move(Physics.gravity * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ballDetectRadius);
    }
}
