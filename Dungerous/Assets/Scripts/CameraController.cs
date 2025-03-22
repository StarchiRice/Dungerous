using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    //Variables
    public Transform target;
    public Transform pivot;
    public Transform obstructCheck;
    public Transform followerStandbyPoint;

    public float freeCamSpeed;

    public float followSpeed;
    private Vector3 startPos;
    private Vector3 startEuler;

    public Vector3 ballFocusOffset, playerFocusOffset, ballAdjustedFocusOffset;
    public float maxZoom, minZoom, zoomSpeed, startZoom;

    public float playerToBallHeightVary;

    public Vector3 obstructTargetOffset;
    public LayerMask whatIsObstruct;
    public bool isObstructed;
    public bool isCamObstructed;

    //References
    private PlayerController player;
    private GameObject ball;
    [HideInInspector]
    public Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        startPos = pivot.transform.position;
        startEuler = transform.localEulerAngles;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        player.cam = this;
        cam = GetComponent<Camera>();
        ball = player.ballTrans.gameObject;
        startZoom = cam.fieldOfView;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        CamMove();
        CamFocus();
        CheckObstruct();
    }

    void CamFocus()
    {
        if(player.isRolling)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, startZoom, zoomSpeed * Time.deltaTime);
            target = ball.transform;
            pivot.transform.parent = player.transform;
            pivot.transform.position = target.position;
            pivot.transform.localEulerAngles = Vector3.Slerp(pivot.transform.localEulerAngles, Vector3.zero, followSpeed * Time.deltaTime);
            if (Mathf.Abs(player.transform.position.y - ball.transform.position.y) > playerToBallHeightVary)
            {
                obstructCheck.localPosition = ballAdjustedFocusOffset;
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, ballAdjustedFocusOffset, followSpeed * Time.deltaTime);
            }
            else if (isObstructed == false)
            {
                obstructCheck.localPosition = ballFocusOffset;
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, ballFocusOffset, followSpeed * Time.deltaTime);
            }
            else if(isCamObstructed)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, Vector3.zero, followSpeed * 3 * Time.deltaTime);
            }
            transform.LookAt(Vector3.Lerp(transform.position, target.position, followSpeed * Time.deltaTime));
        }
        else
        {
            target = player.transform;
            obstructCheck.localPosition = playerFocusOffset;
            pivot.transform.parent = null;
            pivot.transform.position = Vector3.Slerp(pivot.transform.position, player.transform.position, followSpeed * Time.deltaTime);
            if (isObstructed == false)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, playerFocusOffset, followSpeed * Time.deltaTime);
            }
            else if(isCamObstructed)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, Vector3.zero, followSpeed * 3 * Time.deltaTime);
            }
            transform.localEulerAngles = startEuler;
        }
        
    }

    void CheckObstruct()
    {
        if(player.isRolling)
        {
            obstructTargetOffset = Vector3.up * 4;
        }
        else
        {
            obstructTargetOffset = Vector3.up * 2;
        }

        Vector3 obstructTarget = target.transform.position + obstructTargetOffset;

        if (Physics.Raycast(transform.position, obstructTarget - transform.position, Vector3.Distance(transform.position, obstructTarget), whatIsObstruct))
        {
            isCamObstructed = true;
        }
        else
        {
            isCamObstructed = false;
        }

        if (Physics.Raycast(obstructCheck.position, obstructTarget - obstructCheck.position, Vector3.Distance(obstructCheck.position, obstructTarget), whatIsObstruct))
        {
            isObstructed = true;
        }
        else
        {
            isObstructed = false;
        }
    }

    void CamMove()
    {
        if(!player.isRolling)
        {
            Vector3 lookRotation = new Vector3(0, player.moveRInput.x, 0);
            pivot.transform.eulerAngles += lookRotation * freeCamSpeed * Time.deltaTime;

            if (Mathf.Abs(player.moveRInput.y) > 0.3f)
            {
                cam.fieldOfView += -player.moveRInput.y * zoomSpeed * Time.deltaTime;
            }
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, maxZoom, minZoom);
        }
    }
}
