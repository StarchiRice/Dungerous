using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    //Variables
    public float rollSpeedModifier;

    private Vector3 ballRightDir;

    //References
    public GameObject ballModel;
    private CharacterController ctrl;

    // Start is called before the first frame update
    void Start()
    {
        ctrl = GetComponent<CharacterController>();
        ballModel.transform.parent = null;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (ctrl.velocity.x != 0 || ctrl.velocity.z != 0)
        {
            ctrl.transform.rotation = Quaternion.LookRotation(new Vector3(ctrl.velocity.x, 0, ctrl.velocity.z));
        }

        ballModel.transform.position = new Vector3(transform.position.x, transform.position.y + 2.5f, transform.position.z);
        Vector3 ballDir = ctrl.velocity;
        ballRightDir = Vector3.Cross(Vector3.up, ballDir);

        float dist = ctrl.velocity.magnitude * Time.deltaTime;
        float alpha = (dist * 180.0f) / (Mathf.PI * 0.37f);
        ballModel.transform.Rotate(ballRightDir, alpha * rollSpeedModifier, Space.World);
        ctrl.Move(new Vector3(0, Physics.gravity.y, 0) * Time.deltaTime);
    }
}
