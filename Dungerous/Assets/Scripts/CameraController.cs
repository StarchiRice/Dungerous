using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    //Variables
    public Transform target;
    public Transform pivot;

    public float freeCamSpeed;

    public float followSpeed;
    private Vector3 startPos;
    private Vector3 startEuler;

    public Vector3 ballFocusOffset, playerFocusOffset;
    public float maxZoom, minZoom, zoomSpeed;

    //References
    private PlayerController player;
    private GameObject ball;
    private Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        startPos = pivot.transform.position;
        startEuler = transform.localEulerAngles;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        player.cam = this;
        cam = GetComponent<Camera>();
        ball = player.ballTrans.gameObject;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        CamFocus();
        CamMove();
    }

    void CamFocus()
    {
        if(player.isRolling)
        {
            target = ball.transform;
            pivot.transform.parent = player.transform;
            pivot.transform.position = target.position;
            pivot.transform.localEulerAngles = Vector3.Slerp(pivot.transform.localEulerAngles, Vector3.zero, followSpeed * Time.deltaTime);
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, ballFocusOffset, followSpeed * Time.deltaTime);
            transform.LookAt(Vector3.Lerp(transform.position, target.position, followSpeed * Time.deltaTime));
        }
        else
        {
            target = player.transform;
            pivot.transform.parent = null;
            pivot.transform.position = Vector3.Slerp(pivot.transform.position, player.transform.position, followSpeed * Time.deltaTime);
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, playerFocusOffset, followSpeed * Time.deltaTime);
            transform.localEulerAngles = startEuler;
        }
    }

    void CamMove()
    {
        if(!player.isRolling)
        {
            Vector3 lookRotation = new Vector3(0, player.moveRInput.x, 0);
            pivot.transform.eulerAngles += lookRotation * freeCamSpeed * Time.deltaTime;

            if (player.moveRInput.y > 0.3f || player.moveRInput.y < -0.3f)
            {
                cam.fieldOfView += -player.moveRInput.y * zoomSpeed * Time.deltaTime;
            }
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, maxZoom, minZoom);
        }
    }
}
