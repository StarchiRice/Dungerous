using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    //Variables
    public float moveSpeed;
    public float rollMoveSpeed;
    public float rotateSpeed;
    public float jumpForce;
    public float ballRollDistance;
    public float maxDistancetToBall;

    private float jumpTime;
    public float maxJumpTime;

    public float groundCheckRadius;
    public bool isGrounded;
    public LayerMask whatIsGround;

    public bool isRolling = false;
    public Vector2 moveLInput;
    public Vector2 moveRInput;

    public Vector3 moveDirection, moveDirectionR;
    private bool isMoving;
    private bool isMovingBall;

    public float moveModelLeanAmount;

    public bool checkingInventory;

    //References
    private CharacterController charCtrl;
    private PlayerControls controls;
    public PlayerInput playerInp;
    private Animator anim;
    public GameObject model;
    [HideInInspector]
    public CameraController cam;

    public Transform ballTrans;
    private CharacterController ballCtrl;
    private BallController ballLogic;
    public Transform groundCheck;
    private InventoryController inventoryCtrl;

    private void Awake()
    {
        controls = new PlayerControls();
        playerInp.actions = controls.asset;

        controls.Gameplay.MoveL.performed += ctx => moveLInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.MoveL.canceled += ctx => moveLInput = Vector2.zero;

        controls.Gameplay.LookMoveR.performed += ctx => moveRInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.LookMoveR.canceled += ctx => moveRInput = Vector2.zero;

        controls.Gameplay.ModeToggle.performed += ctx => isRolling = !isRolling;
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
    }

    // Start is called before the first frame update
    void Start()
    {
        charCtrl = GetComponent<CharacterController>();
        ballCtrl = ballTrans.gameObject.GetComponent<CharacterController>();
        ballLogic = ballTrans.gameObject.GetComponent<BallController>();
        inventoryCtrl = ballTrans.gameObject.GetComponent<InventoryController>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, whatIsGround);

        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(ballTrans.position.x, 0, ballTrans.position.z)) > maxDistancetToBall || Vector3.Distance(Vector3.up * transform.position.y, Vector3.up * ballTrans.position.y) > maxDistancetToBall * 0.55f)
        {
            isRolling = false;
        }

        if (!isRolling)
        {
            checkingInventory = false;
            inventoryCtrl.CloseInventory();
            ballLogic.freeRoll = true;
            if(isGrounded)
            {
                //Initialize the jump
                if(controls.Gameplay.Jump.WasPressedThisFrame())
                {
                    anim.SetTrigger("Jump");
                    jumpTime = maxJumpTime;
                }
                //Drop cur item on the floor
                if (controls.Gameplay.Inventory.WasPressedThisFrame())
                {
                    inventoryCtrl.equipCtrl.DropItem(transform.position + (transform.forward * 2.5f));
                }
            }
            
            if(jumpTime > 0)
            {
                Jump();
                jumpTime -= Time.deltaTime;
            }
            isMovingBall = false;
            MoveIndependent();
        }
        else
        {
            //Inventory Toggle
            if(isGrounded)
            {
                if (controls.Gameplay.Inventory.WasPressedThisFrame())
                {
                    if (checkingInventory == false)
                    {
                        checkingInventory = true;
                        inventoryCtrl.OpenInventory();
                    }
                    else
                    {
                        checkingInventory = false;
                        inventoryCtrl.CloseInventory();
                    }
                }
                if (checkingInventory == true)
                {
                    if (controls.Gameplay.Jump.WasPressedThisFrame())
                    {
                        inventoryCtrl.SelectItem();
                    }
                }
            }
            ballLogic.freeRoll = false;
        }
        Animate();
    }

    private void FixedUpdate()
    {
        if(isRolling && checkingInventory == false)
        {
            MoveBall();
        }
    }

    void MoveIndependent()
    {
        moveDirection = cam.pivot.transform.TransformDirection(new Vector3(moveLInput.x, 0, moveLInput.y));
        charCtrl.Move(new Vector3(moveDirection.normalized.x * moveSpeed, Physics.gravity.y, moveDirection.normalized.z * moveSpeed) * Time.deltaTime);
        if (moveDirection != Vector3.zero)
        {
            isMoving = true;
            model.transform.localRotation = Quaternion.Euler(transform.TransformDirection(moveDirection.z * moveModelLeanAmount, 0, moveDirection.x * moveModelLeanAmount));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection), rotateSpeed * Time.deltaTime);
        }
        else
        {
            model.transform.localEulerAngles = Vector3.zero;
            isMoving = false;
        }
    }

    void Jump()
    {
        charCtrl.Move(Vector3.up * jumpForce * jumpTime * Time.deltaTime);
    }

    void MoveBall()
    {
        moveDirection = new Vector3(moveLInput.x, 0, moveLInput.y);
        moveDirectionR = transform.TransformDirection(new Vector3(moveRInput.x, 0, moveRInput.y));
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(ballTrans.position.x - transform.position.x, 0, ballTrans.position.z - transform.position.z)), rotateSpeed * Time.deltaTime);
        model.transform.localEulerAngles = Vector3.zero;
        if (Vector3.Distance(Vector3.up * transform.position.y, Vector3.up * ballTrans.position.y) > ballRollDistance * 0.45f)
        {
            charCtrl.Move((ballTrans.position - transform.position).normalized * rollMoveSpeed * Time.deltaTime);
        }
        else if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(ballTrans.position.x, 0, ballTrans.position.z)) > ballRollDistance)
        {
            charCtrl.Move((ballTrans.position - transform.position).normalized * rollMoveSpeed * Time.deltaTime);
        }
        else if (Vector3.Distance(transform.position, ballTrans.position) < ballRollDistance * 0.8f)
        {
            charCtrl.Move(-(ballTrans.position - transform.position).normalized * rollMoveSpeed * Time.deltaTime);
        }
        if(moveRInput != Vector2.zero)
        {
            isMovingBall = true;
            charCtrl.Move(new Vector3(moveDirectionR.normalized.x * rollMoveSpeed, Physics.gravity.y, moveDirectionR.normalized.z * rollMoveSpeed) * Time.deltaTime);
            ballCtrl.Move(new Vector3(moveDirectionR.normalized.x * rollMoveSpeed, Physics.gravity.y, moveDirectionR.normalized.z * rollMoveSpeed) * Time.deltaTime);
        }
        else
        {
            isMovingBall = false;
            charCtrl.Move(new Vector3(transform.right.x * moveDirection.x * rollMoveSpeed, Physics.gravity.y, transform.right.z * moveDirection.x * rollMoveSpeed) * Time.deltaTime);
            ballCtrl.Move(new Vector3(0, Physics.gravity.y, 0) * Time.deltaTime);
        }
        

    }

    void Animate()
    {
        anim.SetBool("IsMoving", isMoving);
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("RMoveXInput", moveRInput.x);
        anim.SetFloat("RMoveYInput", moveRInput.y);
        anim.SetBool("IsRolling", isRolling);
        anim.SetBool("IsMovingBall", isMovingBall);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
