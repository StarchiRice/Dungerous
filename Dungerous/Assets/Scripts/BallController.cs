using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    //Variables
    public float rollSpeedModifier;

    private Vector3 ballRightDir;

    public bool freeRoll;

    //References
    public GameObject ballModel;
    private CharacterController ctrl;
    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        ctrl = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        ballModel.transform.parent = null;
    }

    private void Update()
    {
        if (ctrl.velocity.x != 0 || ctrl.velocity.z != 0)
        {
            ctrl.transform.rotation = Quaternion.LookRotation(new Vector3(ctrl.velocity.x, 0, ctrl.velocity.z));
        }

        ballModel.transform.position = new Vector3(transform.position.x, transform.position.y + 2.5f, transform.position.z);
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if (freeRoll == false)
        {
            ctrl.enabled = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            Vector3 ballDir = ctrl.velocity;
            ballRightDir = Vector3.Cross(Vector3.up, ballDir);
            float dist = ctrl.velocity.magnitude * Time.deltaTime;
            float alpha = (dist * 180.0f) / (Mathf.PI * 0.37f);
            ballModel.transform.Rotate(ballRightDir, alpha * rollSpeedModifier, Space.World);
            ctrl.Move(new Vector3(0, Physics.gravity.y, 0) * Time.deltaTime);
        }
        else
        {
            ctrl.enabled = false;
            rb.useGravity = true;
            Vector3 ballDir = rb.velocity;
            ballRightDir = Vector3.Cross(Vector3.up, ballDir);
            float dist = rb.velocity.magnitude * Time.deltaTime;
            float alpha = (dist * 180.0f) / (Mathf.PI * 0.37f);
            ballModel.transform.Rotate(ballRightDir, alpha * rollSpeedModifier, Space.World);
        }
    }
}
