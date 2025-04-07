using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

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
    private float jumpTimeCutMod;

    public float wallShoveForce;
    public float maxWallShoveTime;
    private float wallShoveTime;

    public float shoveHighJumpForce;
    public float maxShoveHighJumpTime;
    private float shoveHighJumpTime;
    public bool highJumpReady;
    public float maxHighJumpBuffer;
    private float highJumpBuffer;
    public int highJumpLimit;

    public float shoveBoostJumpForce;
    public bool boostJumping;
    public float boostJumpHeightMod;

    public float hangTimeGravityMod;
    public float hangTimeRateChange;

    public float groundCheckRadius;
    public bool isGrounded;
    public LayerMask whatIsGround;

    public bool isRolling = false, isRiding = false;
    public Vector2 moveLInput;
    public Vector2 moveRInput;

    public Vector3 moveDirection, moveDirectionR;
    private bool isMoving;
    private bool isMovingBall;

    public float moveModelLeanAmount;

    public float rideMoveSpeed;
    public float ballRideAcceleration;
    public float rideModeFollowSpeed;

    public bool checkingInventory;

    private float shoveCooldownTime;
    public float startShoveCooldownTime;
    public Transform shovePoint;
    public float shoveRadius, shoveForce;
    public LayerMask whatCanShove;
    public LayerMask whatCanShoveOff;
    public float moveOutOfShoveOffTime;

    ///Sword Vars
    public float swordHitBoxRadius;
    public Transform swordHitBoxPoint;
    public LayerMask whatCanSwordHit;
    public float swordSwingBuffer;
    public float hardSwingBuffer;
    public float curSwingBuffer;
    public bool isAttacking;
    public int attackStage;
    public int maxAttackStage;
    public float attackStageResetBuffer;
    public float curAttackStageResetBuffer;
    private bool followUpAttackDone;

    public bool swordSpinReady;
    public float swordSpinDuration;
    public float curSwordSpinDuration;
    public float swordSpinCooldown;
    private float curSwordSpinCooldown;
    public float spinSpeed;
    public float spinMoveSpeed;
    public int attackOutputID;
    public float swordSpinIDReset;
    private float curSwordSpinIDReset;

    public bool draftLifted;

    public float hammerSwingBuffer;
    public bool hammerAirDrop;
    public int hammerSlamStage, maxSlamStage, storedSlamStage;
    private float curHammerStageUpTime;
    public float hammerStageUpTime;
    private float curHammerSlamAnimTime;
    public float hammerSlamAnimTime;

    //References
    private CharacterController charCtrl;
    [HideInInspector]
    public PlayerControls controls;
    public PlayerInput playerInp;
    private Animator anim;
    public GameObject model;
    [HideInInspector]
    public CameraController cam;

    public Transform armatureTorso;

    public Transform ballTrans;
    private CharacterController ballCtrl;
    [HideInInspector]
    public BallController ballLogic;
    public Transform groundCheck;
    private InventoryController inventoryCtrl;
    private EquipmentController equipCtrl;

    public Follower follower;

    //Prefab Effects
    public GameObject boostJumpEffect, highJumpEffect, shoveEffect;

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
        equipCtrl = GetComponent<EquipmentController>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, whatIsGround);

        //Toggle riding the ball when on top
        if(ballLogic.canRide && highJumpReady == false)
        {
            if(controls.Gameplay.ModeToggle.WasPerformedThisFrame())
            {
                if (!isRiding)
                {
                    isRiding = true;
                }
                else
                {
                    isRiding = false;
                }
            }
        }

        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(ballTrans.position.x, 0, ballTrans.position.z)) > maxDistancetToBall || Vector3.Distance(Vector3.up * transform.position.y, Vector3.up * ballTrans.position.y) > maxDistancetToBall * 0.55f || highJumpReady)
        {
            isRolling = false;
        }

        if (!isRiding)
        {
            if (!isRolling)
            {
                checkingInventory = false;
                inventoryCtrl.CloseInventory();
                ballLogic.freeRoll = true;
                if (!highJumpReady)
                {
                    if (isGrounded)
                    {
                        //Initialize the jump
                        if (controls.Gameplay.Jump.WasPressedThisFrame())
                        {
                            draftLifted = false;
                            anim.SetTrigger("Jump");
                            jumpTime = maxJumpTime;
                            jumpTimeCutMod = 0;
                        }
                        //Drop cur item on the floor
                        if (controls.Gameplay.Inventory.WasPressedThisFrame())
                        {
                            inventoryCtrl.equipCtrl.DropItem(transform.position + (transform.forward * 2.5f));
                        }
                    }
                    if (controls.Gameplay.Jump.WasReleasedThisFrame() && jumpTime > 0)
                    {
                        jumpTimeCutMod = jumpTime;
                    }
                }

                //High Jump Logic
                if (controls.Gameplay.HoldUseItem.WasPerformedThisFrame() && isGrounded == false && highJumpReady == false && wallShoveTime <= 0 && shoveHighJumpTime <= 0 && highJumpLimit <= 0 && equipCtrl.curItemID == 0)
                {
                    highJumpReady = true;
                    highJumpBuffer = maxHighJumpBuffer;
                }
                else if(controls.Gameplay.HoldUseItem.WasReleasedThisFrame() && isGrounded == true && highJumpReady && equipCtrl.curItemID == 0)
                {
                    //Perfect Boost Jump
                    highJumpReady = false;
                    boostJumping = true;
                    GameObject boostEffect = Instantiate(boostJumpEffect, transform.position + (Vector3.up * 0.75f), transform.rotation, null);
                    Destroy(boostEffect, 1f);
                    highJumpLimit++;
                    shoveHighJumpTime = maxShoveHighJumpTime;
                }
                else if(highJumpReady && highJumpBuffer <= 0 && isGrounded)
                {
                    //Standard High Jump
                    highJumpReady = false;
                    boostJumping = false;
                    GameObject highShoveJumpEffect = Instantiate(highJumpEffect, transform.position, transform.rotation, null);
                    Destroy(highShoveJumpEffect, 1f);
                    highJumpLimit++;
                    shoveHighJumpTime = maxShoveHighJumpTime;
                }

                if (highJumpBuffer > 0 && isGrounded)
                {
                    highJumpBuffer -= Time.deltaTime;
                }

                //Manage Jumps
                if (jumpTime > 0)
                {
                    Jump();
                    jumpTime -= (1 + jumpTimeCutMod) * Time.deltaTime;
                }
                if(wallShoveTime > 0)
                {
                    WallShove();
                    jumpTime = 0;
                    highJumpReady = false;
                    shoveHighJumpTime = 0;
                    wallShoveTime -= Time.deltaTime;
                }
                if(shoveHighJumpTime > 0)
                {
                    ShoveHighJump();
                    shoveCooldownTime = 0;
                    jumpTime = 0;
                    shoveHighJumpTime -= Time.deltaTime;
                }
                else if(isGrounded)
                {
                    boostJumping = false;
                    highJumpLimit = 0;
                }

                //Sword Spin Logic
                if (controls.Gameplay.HoldUseItem.WasPerformedThisFrame() && equipCtrl.curItemID == 1 && curSwordSpinCooldown <= 0)
                {
                    swordSpinReady = true;
                }
                else if (controls.Gameplay.HoldUseItem.WasReleasedThisFrame() && isGrounded == true && swordSpinReady && equipCtrl.curItemID == 1)
                {
                    curSwordSpinDuration = swordSpinDuration;
                    curSwordSpinCooldown = swordSpinCooldown;
                    swordSpinReady = false;
                }

                if(curSwordSpinDuration > 0)
                {
                    SwordSpin();
                    curSwordSpinDuration -= Time.deltaTime;
                }
                else
                {
                    curSwordSpinDuration = 0;
                    if (isGrounded && draftLifted)
                    {
                        draftLifted = false;
                    }
                }

                if(draftLifted)
                {
                    DraftLiftEffected();
                }

                isMovingBall = false;
                MoveIndependent();

                //Initialize item use
                if (controls.Gameplay.UseItem.WasPressedThisFrame())
                {
                    equipCtrl.UseItem();
                }
                shoveCooldownTime -= Time.deltaTime;
            }
            else
            {
                //Inventory Toggle
                if (isGrounded)
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
        }
        else
        {
            RideBall();
        }

        //Tick down swing buffer
        if(curSwingBuffer > 0)
        {
            curSwingBuffer -= Time.deltaTime;
        }

        //Manage Follow Up Sword Swing/Sword Spin
        if (equipCtrl.curItemID == 1 || equipCtrl.curItemID == 3)
        {
            if (curAttackStageResetBuffer > 0)
            {
                curAttackStageResetBuffer -= Time.deltaTime;
            }
            else if (curSwordSpinDuration <= 0)
            {

                if (equipCtrl.curItemID == 1 || equipCtrl.curItemID == 3)
                {
                    equipCtrl.curEquip.GetComponentInChildren<TrailRenderer>().emitting = false;
                }
                isAttacking = false;
                if (attackStage > 1)
                {
                    if (followUpAttackDone == false)
                    {
                        SwingSword(true);
                        followUpAttackDone = true;
                    }
                }
                else
                {
                    attackStage = 0;
                }
            }

            if (curSwordSpinDuration <= 0)
            {
                curSwordSpinIDReset = 0;
                if (curSwordSpinCooldown > 0)
                {
                    curSwordSpinCooldown -= Time.deltaTime;
                }
                else
                {
                    curSwordSpinCooldown = 0;
                }
            }
            else if (curSwordSpinDuration > 0.3f)
            {
                if (equipCtrl.curItemID == 1)
                {
                    equipCtrl.curEquip.GetComponentInChildren<TrailRenderer>().emitting = true;
                }
                isAttacking = true;
            }
            else
            {
                if (equipCtrl.curItemID == 1)
                {
                    equipCtrl.curEquip.GetComponentInChildren<TrailRenderer>().emitting = false;
                }
                isAttacking = false;
            }
        }

        //Manage Hammer Air Drop
        if(equipCtrl.curItemID == 4)
        {
            if(isGrounded)
            {
                curHammerStageUpTime = hammerStageUpTime;
            }
            if(curHammerSlamAnimTime > 0)
            {
                curHammerSlamAnimTime -= Time.deltaTime;
            }
            else
            {
                curHammerSlamAnimTime = 0;
            }

            if(hammerAirDrop)
            {
                if (!isGrounded)
                {
                    if (curHammerStageUpTime < 0)
                    {
                        curHammerStageUpTime = hammerStageUpTime;
                        if (hammerSlamStage < maxSlamStage)
                        {
                            hammerSlamStage++;
                        }
                    }
                    else
                    {
                        curHammerStageUpTime -= Time.deltaTime;
                    }
                }
                else
                {
                    HammerSlam(true);
                }
            }
        }

        //Deal damage when attacking
        if(isAttacking)
        {
            DealDamage();
        }

        Animate();
    }

    private void FixedUpdate()
    {
        if(isRolling && checkingInventory == false && highJumpReady == false)
        {
            MoveBall();
        }
    }

    void MoveIndependent()
    {
        ballLogic.isRide = false;
        if (wallShoveTime <= moveOutOfShoveOffTime)
        {
            moveDirection = cam.pivot.transform.TransformDirection(new Vector3(moveLInput.x, 0, moveLInput.y));
        }

        if (jumpTime <= 0 && shoveHighJumpTime <= 0 && wallShoveTime <= 0 && !isGrounded)
        {
            hangTimeGravityMod = Mathf.Lerp(hangTimeGravityMod, 1, hangTimeRateChange * Time.deltaTime);
            charCtrl.Move((Vector3.up * Physics.gravity.y * hangTimeGravityMod) * Time.deltaTime);
        }
        else if(jumpTime <= 0 && shoveHighJumpTime <= 0 && wallShoveTime <= 0 && isGrounded)
        {
            charCtrl.Move((Vector3.up * Physics.gravity.y) * Time.deltaTime);
            hangTimeGravityMod = 0;
        }
        if(isGrounded == true && highJumpReady)
        {
            charCtrl.Move(Vector3.zero);
        }
        else if(curSwordSpinDuration > 0)
        {
            charCtrl.Move(new Vector3(moveDirection.normalized.x * spinMoveSpeed, 0, moveDirection.normalized.z * spinMoveSpeed) * Time.deltaTime);
        }
        else if (wallShoveTime <= moveOutOfShoveOffTime)
        {
            charCtrl.Move(new Vector3(moveDirection.normalized.x * moveSpeed, 0, moveDirection.normalized.z * moveSpeed) * Time.deltaTime);
        }

        if (moveDirection != Vector3.zero)
        {
            isMoving = true;
            if (curSwordSpinDuration <= 0.3f)
            {
                model.transform.localRotation = Quaternion.Euler(transform.TransformDirection(moveDirection.z * moveModelLeanAmount, 0, moveDirection.x * moveModelLeanAmount));
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection), rotateSpeed * Time.deltaTime);
            }
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
        ballLogic.isRide = false;
        moveDirectionR = new Vector3(moveRInput.x, 0, moveRInput.y);
        moveDirection = transform.TransformDirection(new Vector3(moveLInput.x, 0, moveLInput.y));
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
        if(moveLInput != Vector2.zero)
        {
            isMovingBall = true;
            charCtrl.Move(new Vector3(moveDirection.normalized.x * rollMoveSpeed, Physics.gravity.y, moveDirection.normalized.z * rollMoveSpeed) * Time.deltaTime);
            charCtrl.Move(new Vector3(transform.right.x * moveDirectionR.x * rollMoveSpeed, 0, transform.right.z * moveDirectionR.x * rollMoveSpeed) * Time.deltaTime);
            ballCtrl.Move(new Vector3(moveDirection.normalized.x * rollMoveSpeed, Physics.gravity.y, moveDirection.normalized.z * rollMoveSpeed) * Time.deltaTime);
        }
        else
        {
            isMovingBall = false;
            charCtrl.Move(new Vector3(transform.right.x * moveDirectionR.x * rollMoveSpeed, Physics.gravity.y, transform.right.z * moveDirectionR.x * rollMoveSpeed) * Time.deltaTime);
            ballCtrl.Move(new Vector3(0, Physics.gravity.y, 0) * Time.deltaTime);
        }
    }

    void RideBall()
    {
        moveDirection = cam.pivot.transform.TransformDirection(new Vector3(moveLInput.x, 0, moveLInput.y));
        
        ballLogic.BallRide();
        transform.position = Vector3.Lerp(transform.position, ballLogic.ballRidePoint.position, rideModeFollowSpeed * Time.deltaTime);

        if (ballLogic.isGrounded)
        {
            if (ballLogic.rb.velocity.y < -7)
            {
                ballLogic.slopePowerTime += 1 * Time.deltaTime;
                ballLogic.rb.velocity = (Vector3.MoveTowards(ballLogic.rb.velocity, new Vector3(moveDirection.normalized.x * rideMoveSpeed * 10 * Time.fixedDeltaTime, ballLogic.rb.velocity.y * 10, moveDirection.normalized.z * rideMoveSpeed * 10 * Time.fixedDeltaTime), ballRideAcceleration * 6 * Time.deltaTime));
            }
            else if(ballLogic.slopePowered)
            {
                ballLogic.slopePowerTime -= Time.deltaTime;
                ballLogic.rb.velocity = (Vector3.MoveTowards(ballLogic.rb.velocity, new Vector3(moveDirection.normalized.x * rideMoveSpeed * 7 * Time.fixedDeltaTime, ballLogic.rb.velocity.y * 7, moveDirection.normalized.z * rideMoveSpeed * 7 * Time.fixedDeltaTime), ballRideAcceleration * 7 * Time.deltaTime));
            }
            else
            {
                ballLogic.rb.velocity = (Vector3.MoveTowards(ballLogic.rb.velocity, new Vector3(moveDirection.normalized.x * rideMoveSpeed * Time.fixedDeltaTime, ballLogic.rb.velocity.y, moveDirection.normalized.z * rideMoveSpeed * Time.fixedDeltaTime), ballRideAcceleration * Time.deltaTime));
            }
        }
        else
        {
            if(ballLogic.slopePowered)
            {
                ballLogic.slopePowerTime -= Time.deltaTime;
            }
        }
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

    public void Shove()
    {
        if (shoveCooldownTime <= 0)
        {
            attackOutputID = Random.Range(1, 1000);
            anim.SetTrigger("Shove");
            shoveCooldownTime = startShoveCooldownTime;
            if (Physics.CheckSphere(shovePoint.position, shoveRadius, whatCanShove))
            {
                Collider[] shoveCol = Physics.OverlapSphere(shovePoint.position, shoveRadius, whatCanShove);
                for (int i = 0; i < shoveCol.Length; i++)
                {
                    if (shoveCol[i].GetComponent<Rigidbody>())
                    {
                        if (boostJumping)
                        {
                            shoveCol[i].GetComponent<Rigidbody>().velocity += ((shoveCol[i].transform.position - transform.position) * shoveForce * 2 / shoveCol[i].GetComponent<Rigidbody>().mass);
                            shoveCooldownTime = startShoveCooldownTime * 1.5f;
                        }
                        else
                        {
                            shoveCol[i].GetComponent<Rigidbody>().velocity += ((shoveCol[i].transform.position - transform.position) * shoveForce / shoveCol[i].GetComponent<Rigidbody>().mass);
                        }
                    }
                    if (shoveCol[i].GetComponent<EnemyHealth>())
                    {
                        if (boostJumping)
                        {
                            shoveCol[i].GetComponent<EnemyHealth>().TakeDamage(0, 1.5f, (shoveCol[i].transform.position - transform.position).normalized, attackOutputID, transform.position);
                        }
                        else
                        {
                            shoveCol[i].GetComponent<EnemyHealth>().TakeDamage(0, 1, (shoveCol[i].transform.position - transform.position).normalized, attackOutputID, transform.position);
                        }
                    }
                }
                GameObject shoveEft = Instantiate(shoveEffect, shovePoint.position - (shovePoint.forward * 0.25f), shovePoint.rotation, null);
                Destroy(shoveEft, 1f);
            }
            else if(Physics.CheckSphere(shovePoint.position, shoveRadius, whatCanShoveOff))
            {
                Collider shoveOffCol = Physics.OverlapSphere(shovePoint.position, shoveRadius, whatCanShoveOff)[0];
                if (shoveOffCol != null && isGrounded == false)
                {
                    wallShoveTime = maxWallShoveTime;
                    shoveCooldownTime = startShoveCooldownTime / 3;
                    GameObject shoveEft = Instantiate(shoveEffect, shovePoint.position - (shovePoint.forward * 0.25f), shovePoint.rotation, null);
                    Destroy(shoveEft, 1f);
                }
            }
        }
    }

    public void WallShove()
    {
        hangTimeGravityMod = 0;
        charCtrl.Move(transform.TransformDirection(new Vector3(0, 1.2f, -1.2f)) * wallShoveForce * wallShoveTime * Time.deltaTime);
    }

    public void ShoveHighJump()
    {
        if (boostJumping)
        {
            charCtrl.Move((transform.forward + (Vector3.up * boostJumpHeightMod)) * shoveBoostJumpForce * shoveHighJumpTime * Time.deltaTime);
        }
        else
        {
            charCtrl.Move(Vector3.up * shoveHighJumpForce * shoveHighJumpTime * Time.deltaTime);
        }
    }

    public void SwingSword(bool followUpSwing)
    {
        if (curSwingBuffer <= 0 || (followUpSwing && !followUpAttackDone))
        {
            attackOutputID = Random.Range(1, 1000);
            if (!followUpSwing)
            {
                curSwingBuffer = swordSwingBuffer;
                attackStage++;
                followUpAttackDone = false;
            }
            else
            {
                curSwingBuffer = hardSwingBuffer;
            }
            curAttackStageResetBuffer = attackStageResetBuffer;
            
            if(followUpSwing)
            {
                attackStage = 0;
            }
            else
            {
                anim.SetTrigger("SwingWeapon");
            }
        }
        else if(!followUpAttackDone && attackStage < maxAttackStage && curAttackStageResetBuffer > 0)
        {
            attackStage++;
        }
    }

    public void SwordSpin()
    {
        if (curSwordSpinIDReset <= 0)
        {
            curSwordSpinIDReset = swordSpinIDReset;
            attackOutputID = Random.Range(1, 1000);
        }
        else
        {
            curSwordSpinIDReset -= Time.deltaTime;
        }
        charCtrl.transform.Rotate(Vector3.up * spinSpeed * Mathf.Clamp(curSwordSpinDuration, 0, swordSpinDuration) * Time.deltaTime);
    }

    public void SwordAttacking(int isAttackState)
    {
        if(isAttackState == 1)
        {
            isAttacking = true;
            equipCtrl.curEquip.GetComponentInChildren<TrailRenderer>().emitting = true;
        }
        else if(isAttackState == 0)
        {
            isAttacking = false;
            equipCtrl.curEquip.GetComponentInChildren<TrailRenderer>().emitting = false;
        }
    }

    public void HammerAttacking(int isAttackState)
    {
        if (isAttackState == 1)
        {
            attackOutputID = Random.Range(1, 1000);
            isAttacking = true;
        }
        else if (isAttackState == 0)
        {
            isAttacking = false;
        }
    }

    public void HammerSlam(bool bypass)
    {
        if (curSwingBuffer <= 0 || bypass)
        {
            curSwingBuffer = hammerSwingBuffer;
            if (isGrounded)
            {
                curHammerStageUpTime = hammerStageUpTime;
                if (hammerSlamStage <= 0)
                {
                    hammerSlamStage = 1;
                }
                curHammerSlamAnimTime = hammerSlamAnimTime;
                if (hammerAirDrop == false)
                {
                    anim.SetTrigger("SlamHammer");
                }
                else
                {
                    anim.SetTrigger("AirSlamHammer");
                }
                Debug.Log("Performed HAMMER SLAM Stage " + hammerSlamStage);
                storedSlamStage = hammerSlamStage;
                hammerSlamStage = 0;
                hammerAirDrop = false;
                curHammerStageUpTime = 0;
            }
            else if(hammerAirDrop == false)
            {
                hammerSlamStage = 0;
                curHammerStageUpTime = hammerStageUpTime;
                hammerAirDrop = true;
            }
        }
    }

    public void SlamJump()
    {
        if (isGrounded)
        {
            anim.SetTrigger("Jump");
            if (storedSlamStage == 1)
            {
                jumpTime = 0.4f * storedSlamStage;
            }
            else
            {
                jumpTime = 0.3f * storedSlamStage;
            }
            jumpTimeCutMod = 0;
        }
        storedSlamStage = 0;
    }

    public void DealDamage()
    {
        if(equipCtrl.curItemID == 1 || equipCtrl.curItemID == 3 || equipCtrl.curItemID == 4)
        {
            Collider[] dmgCol = Physics.OverlapSphere(swordHitBoxPoint.position, swordHitBoxRadius, whatCanSwordHit);
            if(dmgCol.Length > 0)
            {
                for (int i = 0; i < dmgCol.Length; i++)
                {
                    if (dmgCol[i].GetComponent<EnemyHealth>().lastHitID != attackOutputID)
                    {
                        dmgCol[i].GetComponent<EnemyHealth>().TakeDamage(2, 1, (dmgCol[i].transform.position - transform.position).normalized, attackOutputID, transform.position);
                        Debug.Log("Dealt DAMAGE");
                    }
                }
            }
        }
    }

    public void DraftLiftEffected()
    {
        charCtrl.Move(Vector3.up * -Physics.gravity.y * Mathf.Clamp(curSwordSpinDuration, 0, 1.15f) * Time.deltaTime);
    }

    public IEnumerator ThrowBomb()
    {
        equipCtrl.curEquip.GetComponent<ProjectileItem>().ShootProjectile(transform.TransformDirection(new Vector3(0, 1.75f, 1f)));
        yield return new WaitForSeconds(0);
    }

    void Animate()
    {
        anim.SetBool("IsMoving", isMoving);
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("LMoveXInput", moveLInput.x);
        anim.SetFloat("LMoveYInput", moveLInput.y);
        anim.SetBool("IsRolling", isRolling);
        anim.SetBool("IsMovingBall", isMovingBall);
        anim.SetBool("IsRiding", isRiding);
        anim.SetFloat("RideSpeed", Mathf.Clamp( new Vector3(ballLogic.rb.velocity.x, 0, ballLogic.rb.velocity.z).magnitude * 0.5f, 0.1f, rideMoveSpeed));
        anim.SetInteger("CurItem", equipCtrl.curItemID);
        anim.SetBool("HighJumpReady", highJumpReady);
        anim.SetInteger("AttackStage", attackStage);
        anim.SetFloat("SwordSpinDur", curSwordSpinDuration);
        anim.SetBool("HammerAirDrop", hammerAirDrop);
        anim.SetFloat("HammerSlamTime", curHammerSlamAnimTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(shovePoint.position, shoveRadius);

        if (isAttacking)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(swordHitBoxPoint.position, swordHitBoxRadius);
        }
    }
}
