using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.EventSystems;

public class EvasiveEnemyController : MonoBehaviour
{
    //Variables
    public Transform target, groundCheckPoint;
    public float detectRadius, ballDetectRadius, groundCheckRadius;

    public bool isGrounded;

    private float chaseToDistance;
    public float playerChaseToDistance, ballChaseToDistance;

    public float hangTimeGravityMod, hangTimeRateChange;

    public float roamSpeed;
    public float leapSpeed;
    public float rotateSpeed;

    public bool playerOriented;
    public int behaveState, evadeCount;
    //0: Idle
    //1: Roam
    //2: Evade
    //3: Aggro

    public float maxStateChangeTime, maxLeapTime, minRoamTime, maxRoamTime;
    private float curStateChangeTime, curLeapTime, curRoamTime;

    public Vector3 roamDir, leapDir;
    private int surroundFacing = 1;

    private bool isLeaping;

    //References
    public LayerMask whatIsTarget, whatIsBall, whatIsGround;
    private CharacterController charCtrl;
    private EnemyHealth healthCtrl;

    private Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        charCtrl = GetComponent<CharacterController>();
        healthCtrl = GetComponent<EnemyHealth>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Collider[] detectCol = Physics.OverlapSphere(transform.position, detectRadius, whatIsTarget);
        Collider[] ballCol = Physics.OverlapSphere(transform.position, ballDetectRadius, whatIsBall);


        if (detectCol.Length > 0 && ballCol.Length > 0 && !playerOriented)
        {
            //Determines the target based on distance when no target preference specified
            if (Vector3.Distance(transform.position, detectCol[0].transform.position) < Vector3.Distance(transform.position, ballCol[0].transform.position))
            {
                if(target == null && behaveState <= 1)
                {
                    curStateChangeTime = 0;
                }
                target = detectCol[0].transform;
                chaseToDistance = playerChaseToDistance;
            }
            else
            {
                if (target == null)
                {
                    curStateChangeTime = 0;
                }
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
                if (target != null)
                {
                    behaveState = Random.Range(0, 2);
                    if (behaveState == 1)
                    {
                        roamDir = new Vector3(Random.Range(-1, 1), 0, Random.Range(-1, 1)).normalized;
                        curRoamTime = Random.Range(minRoamTime, maxRoamTime);
                    }
                }
                target = null;
            }

        }

        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, whatIsGround);

        if (healthCtrl.curHitStun <= 0 && healthCtrl.curHealth > 0)
        {
            StateChange();
            if (behaveState == 0)
            {
                //Idle
                Idle();
            }
            else if(behaveState == 1)
            {
                //Roam
                Roam();
            }
            else if(behaveState == 2)
            {
                //Evade
                EvadeLeap();
            }
            else if(behaveState == 3)
            {
                //Aggro
                AggroLeap();
            }
        }
        else
        {
            if (healthCtrl.lastHitOrigin != null)
            {
                Vector3 hitOriginVector = (healthCtrl.lastHitOrigin - transform.position).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(hitOriginVector.x, 0, hitOriginVector.z)), rotateSpeed * Time.deltaTime);
            }
        }

        Animate();
    }

    public void AggroLeap()
    {
        Vector3 lookDir = (target.position - transform.position).normalized;
        if (curLeapTime > 0)
        {
            charCtrl.Move(new Vector3(leapDir.x * leapSpeed, leapSpeed * curLeapTime, leapDir.z * leapSpeed) * Time.deltaTime);
            curRoamTime = Random.Range(minRoamTime, maxRoamTime);
        }
        else
        {
            roamDir = transform.right * surroundFacing;
            if (!isGrounded)
            {
                Idle();
            }
            else
            {
                Roam();
            }
        }
        if (healthCtrl.curHitStun <= 0)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(lookDir.x, 0, lookDir.z)), rotateSpeed * Time.deltaTime);
        }
    }

    public void EvadeLeap()
    {
        Vector3 lookDir = (target.position - transform.position).normalized;
        if (curLeapTime > 0)
        {
            charCtrl.Move(new Vector3(leapDir.x * leapSpeed, leapSpeed * curLeapTime, leapDir.z * leapSpeed) * Time.deltaTime);
            curRoamTime = Random.Range(minRoamTime, maxRoamTime);
        }
        else
        {
            roamDir = transform.right * surroundFacing;
            if (!isGrounded)
            {
                Idle();
            }
            else
            {
                Roam();
            }
        }
        if (healthCtrl.curHitStun <= 0)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(lookDir.x, 0, lookDir.z)), rotateSpeed * Time.deltaTime);
        }
    }

    public void Idle()
    {
        if (!isGrounded)
        {
            hangTimeGravityMod = Mathf.Lerp(hangTimeGravityMod, 1, hangTimeRateChange * Time.deltaTime);
            charCtrl.Move(new Vector3(leapDir.x * leapSpeed, Physics.gravity.y * hangTimeGravityMod, leapDir.z * leapSpeed) * Time.deltaTime);
        }
        else
        {
            isLeaping = false;
            hangTimeGravityMod = 0;
            charCtrl.Move(Physics.gravity * Time.deltaTime);
        }
        if (healthCtrl.curHitStun <= 0 && target != null)
        {
            Vector3 lookDir = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(lookDir.x, 0, lookDir.z)), rotateSpeed * Time.deltaTime);
        }
    }

    public void Roam()
    {
        if (curRoamTime > 0)
        {
            curRoamTime -= Time.deltaTime;
            if (!isGrounded)
            {
                hangTimeGravityMod = Mathf.Lerp(hangTimeGravityMod, 1, hangTimeRateChange * Time.deltaTime);
                charCtrl.Move(Physics.gravity * hangTimeGravityMod * Time.deltaTime);
            }
            else
            {
                isLeaping = false;
                hangTimeGravityMod = 0;
                charCtrl.Move(Physics.gravity * Time.deltaTime);
            }
            charCtrl.Move(new Vector3(roamDir.x * roamSpeed, 0, roamDir.z * roamSpeed) * Time.deltaTime);
        }
        else
        {
            //When at min chase distance
            Idle();
        }
        if (healthCtrl.curHitStun <= 0)
        {
            if (target == null)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(roamDir.x, 0, roamDir.z)), rotateSpeed * Time.deltaTime);
            }
            else
            {
                Vector3 lookDir = (target.position - transform.position).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(lookDir.x, 0, lookDir.z)), rotateSpeed * Time.deltaTime);
            }
        }
    }

    public void StateChange()
    {
        if(curStateChangeTime <= 0)
        {
            curStateChangeTime = maxStateChangeTime;
            surroundFacing *= -1;
            if(target != null)
            {
                if (evadeCount < 3)
                {
                    behaveState = Random.Range(2, 4);
                }
                else
                {
                    behaveState = 3;
                }
                if(behaveState == 2)
                {
                    //Evade
                    curLeapTime = maxLeapTime;
                    evadeCount++;
                    leapDir = new Vector3(Random.Range(-1, 2), 0, Random.Range(-1, 2)).normalized;
                }
                else if(behaveState == 3)
                {
                    //Aggro
                    curLeapTime = maxLeapTime;
                    evadeCount = 0;
                    leapDir = (target.position - transform.position).normalized;
                }
            }
            else
            {
                behaveState = Random.Range(0, 2);
                if(behaveState == 1)
                {
                    roamDir = new Vector3(Random.Range(-1, 2), 0, Random.Range(-1, 2)).normalized;
                    curRoamTime = Random.Range(minRoamTime, maxRoamTime);
                }
            }
        }
        else
        {
            curStateChangeTime -= Time.deltaTime;
        }

        if(curLeapTime > 0)
        {
            isLeaping = true;
            curLeapTime -= Time.deltaTime;
        }
    }

    public void Animate()
    {
        anim.SetBool("IsLeaping", isLeaping);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ballDetectRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}
