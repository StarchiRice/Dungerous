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
    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        ballModel.transform.parent = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (rb.velocity.x != 0 || rb.velocity.z != 0)
        {
            rb.rotation = Quaternion.LookRotation(new Vector3(rb.velocity.x, 0, rb.velocity.z));
        }

        ballModel.transform.position = new Vector3(transform.position.x, transform.position.y + 2.5f, transform.position.z);
        Vector3 ballDir = rb.velocity;
        ballRightDir = Vector3.Cross(Vector3.up, ballDir);

        float dist = rb.velocity.magnitude * Time.deltaTime;
        float alpha = (dist * 180.0f) / (Mathf.PI * 0.37f);
        ballModel.transform.Rotate(ballRightDir, alpha * rollSpeedModifier, Space.World);
    }
}
