using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;
using UnityEngine.Rendering.Universal;

public class PlayerController : MonoBehaviour
{
    //Variables
    public float moveSpeed, runSpeed, altRunSpeed;
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

    private float groundHeight;
    public float groundCheckRadius;
    public bool isGrounded, isOnBall;
    public LayerMask whatIsGround;
    public LayerMask whatIsBall;
    public Vector3 hitNormal;
    public float slopeLimit;
    public float currentSlope;
    public float slideFriction;

    public bool isRolling = false, isRiding = false, isRunning, runAlternating;
    public float maxAltWindowTime, curAltWindowTime;
    public int alternateIndex, alternateCount; // 0 None, 1 Left, 2 Right
    public Vector2 moveLInput;
    public Vector2 moveRInput;

    public Vector3 moveDirection, moveDirectionR, storedDirection;
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
    public GameObject model, modelPivot;
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

    public DecalProjector shadowProject;
    public ParticleSystem runParticleEffect, altRunParticleEffect;

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

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        hitNormal = hit.normal;
    }

    void Update()
    {
        RaycastHit groundHit;
        if (Physics.Raycast(groundCheck.position, -transform.up, out groundHit, groundCheckRadius, whatIsGround))
        {
            Debug.DrawRay(groundCheck.position, -transform.up * groundCheckRadius, Color.red);
            currentSlope = groundHit.normal.y * 90;
        }
        else
        {
            currentSlope = 0;
        }

        //Set Shadow Position
        RaycastHit shadowHit;
        if (Physics.Raycast(shadowProject.transform.position, -transform.up, out shadowHit, float.PositiveInfinity, whatIsGround))
        {
            Debug.DrawRay(shadowProject.transform.position, -transform.up * Vector3.Distance(shadowProject.transform.position, shadowHit.point), Color.black);
            groundHeight = Vector3.Distance(shadowProject.transform.position, shadowHit.point) + 1;
            shadowProject.size = new Vector3(shadowProject.size.x, shadowProject.size.y, groundHeight);
            shadowProject.pivot = new Vector3(0, 0, groundHeight / 2);
        }

        //Control ground check by sphere and normal slope
        if (Physics.CheckSphere(groundCheck.position, groundCheckRadius, whatIsBall))
        {
            isOnBall = true;
            isGrounded = true;
        }
        else if (Physics.CheckSphere(groundCheck.position, groundCheckRadius, whatIsGround))
        {
            isOnBall = false;
            if (currentSlope > 0)
            {
                isGrounded = true;
            }
            else
            {
                isGrounded = (Vector3.Angle(Vector3.up, hitNormal) <= slopeLimit);
            }
        }
        else
        {
            isOnBall = false;
            isGrounded = false;
        }

        if (isGrounded)
        {
            if (currentSlope >= slopeLimit)
            {
                modelPivot.transform.localRotation = Quaternion.Slerp(modelPivot.transform.localRotation, Quaternion.FromToRotation(Vector3.up, transform.InverseTransformDirection(groundHit.normal)), rotateSpeed / 2 * Time.deltaTime);
            }
            else
            {
                modelPivot.transform.localRotation = Quaternion.Slerp(modelPivot.transform.localRotation, Quaternion.identity, rotateSpeed / 2 * Time.deltaTime);
            }

            if (controls.Gameplay.Run.IsPressed())
            {
                isRunning = true;
            }
            else
            {
                isRunning = false;
            }
        }
        else
        {
            modelPivot.transform.localRotation = Quaternion.Slerp(modelPivot.transform.localRotation, Quaternion.identity, rotateSpeed / 2 * Time.deltaTime);
        }

        //Toggle riding the ball when on top
        if (ballLogic.canRide && highJumpReady == false)
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
        AlternateRun();
        //Character sliding of surfaces
        if (!isGrounded)
        {
            charCtrl.Move(new Vector3((1f - hitNormal.y) * hitNormal.x * (1f - slideFriction), 0, (1f - hitNormal.y) * hitNormal.z * (1f - slideFriction)) * Time.deltaTime);
        }

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
            if (runAlternating)
            {
                anim.SetFloat("RunSpeed", 1.4f);
                charCtrl.Move(new Vector3(moveDirection.normalized.x * altRunSpeed, 0, moveDirection.normalized.z * altRunSpeed) * Time.deltaTime);
            }
            else if(isRunning)
            {
                anim.SetFloat("RunSpeed", 1.2f);
                charCtrl.Move(new Vector3(moveDirection.normalized.x * runSpeed, 0, moveDirection.normalized.z * runSpeed) * Time.deltaTime);
            }
            else
            {
                anim.SetFloat("RunSpeed", 1f);
                charCtrl.Move(new Vector3(moveDirection.normalized.x * moveSpeed, 0, moveDirection.normalized.z * moveSpeed) * Time.deltaTime);
            }
        }

        if (moveDirection != Vector3.zero && wallShoveTime <= moveOutOfShoveOffTime && curSwingBuffer <= 0)
        {
            isMoving = true;
            storedDirection = moveDirection;
            if (curSwordSpinDuration <= 0.3f)
            {
                if (isGrounded)
                {
                    Vector3 findDirection = transform.TransformDirection(moveDirection.z, 0, moveDirection.x);
                    if (runAlternating)
                    {
                        if (altRunParticleEffect.isPlaying == false)
                        {
                            altRunParticleEffect.Play();
                            runParticleEffect.Stop();
                        }
                        model.transform.localRotation = Quaternion.Slerp(model.transform.localRotation, Quaternion.Euler(new Vector3(findDirection.x * (moveModelLeanAmount * 1.5f), 0, findDirection.z * (-moveModelLeanAmount * 1.5f))), rotateSpeed * 1.5f * Time.deltaTime);
                    }
                    else if(isRunning)
                    {
                        if (runParticleEffect.isPlaying == false)
                        {
                            runParticleEffect.Play();
                            altRunParticleEffect.Stop();
                        }
                        model.transform.localRotation = Quaternion.Slerp(model.transform.localRotation, Quaternion.Euler(new Vector3(findDirection.x * moveModelLeanAmount, 0, findDirection.z * -moveModelLeanAmount)), rotateSpeed * Time.deltaTime);
                    }
                    else
                    {
                        if (runParticleEffect.isPlaying == true || altRunParticleEffect.isPlaying == true)
                        {
                            runParticleEffect.Stop();
                            altRunParticleEffect.Stop();
                        }
                        model.transform.localRotation = Quaternion.Slerp(model.transform.localRotation, Quaternion.Euler(new Vector3(findDirection.x * -moveModelLeanAmount, 0, findDirection.z * -moveModelLeanAmount)), rotateSpeed / 3 * Time.deltaTime);
                    }
                }
                else
                {
                    if (runParticleEffect.isPlaying == true || altRunParticleEffect.isPlaying == true)
                    {
                        runParticleEffect.Stop();
                        altRunParticleEffect.Stop();
                    }
                    model.transform.localRotation = Quaternion.Slerp(model.transform.localRotation, Quaternion.Euler(model.transform.TransformDirection(moveDirection.z * moveModelLeanAmount, 0, moveDirection.x * moveModelLeanAmount)), rotateSpeed / 3 * Time.deltaTime);
                }
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection), rotateSpeed / 2 * Time.deltaTime);
            }
        }
        else if(curSwingBuffer > 0)
        {
            if (isGrounded)
            {
                model.transform.localRotation = Quaternion.Slerp(model.transform.localRotation, Quaternion.Euler(-transform.TransformDirection(moveDirection.normalized.z * moveModelLeanAmount / 2, 0, moveDirection.normalized.x * moveModelLeanAmount / 2)), rotateSpeed / 2 * Time.deltaTime);
            }
            else
            {
                if(moveDirection != Vector3.zero)
                {
                    model.transform.localRotation = Quaternion.Slerp(model.transform.localRotation, Quaternion.Euler(transform.TransformDirection(moveDirection.normalized.z * moveModelLeanAmount * 3, 0, moveDirection.normalized.x * moveModelLeanAmount * 3)), rotateSpeed / 2 * Time.deltaTime);
                    storedDirection = moveDirection;
                }
                else
                {
                    model.transform.localRotation = Quaternion.Slerp(model.transform.localRotation, Quaternion.Euler(transform.TransformDirection(storedDirection.normalized.z * moveModelLeanAmount * 3, 0, storedDirection.normalized.x * moveModelLeanAmount * 3)), rotateSpeed / 2 * Time.deltaTime);
                }
            }
            if (moveDirection != Vector3.zero)
            {
                isMoving = true;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection), rotateSpeed * Time.deltaTime);
            }
            else
            {
                isMoving = false;
            }
        }
        else
        {
            if (runParticleEffect.isPlaying == true || altRunParticleEffect.isPlaying == true)
            {
                runParticleEffect.Stop();
                altRunParticleEffect.Stop();
            }
            model.transform.localRotation = Quaternion.Slerp(model.transform.localRotation, Quaternion.identity, rotateSpeed * Time.deltaTime);
            isMoving = false;
        }
    }

    void Jump()
    {
        charCtrl.Move(Vector3.up * jumpForce * jumpTime * Time.deltaTime);
    }

    void MoveBall()
    {
        if (runParticleEffect.isPlaying == true || altRunParticleEffect.isPlaying == true)
        {
            runParticleEffect.Stop();
            altRunParticleEffect.Stop();
        }
        ballLogic.isRide = false;
        moveDirectionR = new Vector3(moveRInput.x, 0, moveRInput.y);
        moveDirection = transform.TransformDirection(new Vector3(moveLInput.x, 0, moveLInput.y));
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(ballTrans.position.x - transform.position.x, 0, ballTrans.position.z - transform.position.z)), rotateSpeed * Time.deltaTime);
        model.transform.localEulerAngles = Vector3.zero;

        RaycastHit ballDistHit;
        if (Physics.Raycast(transform.position + (Vector3.up * charCtrl.height * 1.25f), (ballTrans.position - transform.position + (Vector3.up * charCtrl.height * 1.25f)).normalized, out ballDistHit, Vector3.Distance(transform.position + (Vector3.up * charCtrl.height * 1.25f), ballTrans.position), whatIsBall))
        {
            Debug.DrawRay(transform.position + (Vector3.up * charCtrl.height * 1.25f), (ballTrans.position - transform.position + (Vector3.up * charCtrl.height * 1.25f)).normalized * Vector3.Distance(transform.position + (Vector3.up * charCtrl.height * 1.25f), ballDistHit.point), Color.cyan);
        }

        Vector3 correctedDistanceMaintainVector = ballDistHit.point + ((ballDistHit.point - ballTrans.position).normalized * ballRollDistance);

        charCtrl.Move((correctedDistanceMaintainVector - (transform.position + (Vector3.up * charCtrl.height * 1.25f))).normalized * rollMoveSpeed * Time.deltaTime);

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
        if (runParticleEffect.isPlaying == true || altRunParticleEffect.isPlaying == true)
        {
            runParticleEffect.Stop();
            altRunParticleEffect.Stop();
        }
        moveDirection = cam.pivot.transform.TransformDirection(new Vector3(moveLInput.x, 0, moveLInput.y));
        
        ballLogic.BallRide();
        transform.position = Vector3.Lerp(transform.position, ballLogic.ballRidePoint.position, rideModeFollowSpeed * Time.deltaTime);

        if (ballLogic.isGrounded)
        {
            if (ballLogic.rb.velocity.y < -7)
            {
                ballLogic.slopePowerTime += 1.2f * Time.deltaTime;
                ballLogic.rb.velocity = (Vector3.MoveTowards(ballLogic.rb.velocity, new Vector3(moveDirection.normalized.x * rideMoveSpeed * 10 * Time.fixedDeltaTime, ballLogic.rb.velocity.y * 10, moveDirection.normalized.z * rideMoveSpeed * 10 * Time.fixedDeltaTime), ballRideAcceleration * 6 * Time.deltaTime));
            }
            else if(ballLogic.slopePowered)
            {
                ballLogic.slopePowerTime -= Time.deltaTime;
                ballLogic.rb.velocity = (Vector3.MoveTowards(ballLogic.rb.velocity, new Vector3(moveDirection.normalized.x * rideMoveSpeed * 8 * Time.fixedDeltaTime, ballLogic.rb.velocity.y * 8, moveDirection.normalized.z * rideMoveSpeed * 8 * Time.fixedDeltaTime), ballRideAcceleration * 7 * Time.deltaTime));
            }
            else
            {
                if(Mathf.Abs(ballLogic.rb.velocity.x) < 3 && Mathf.Abs(ballLogic.rb.velocity.z) < 3)
                {
                    ballLogic.rb.velocity = (Vector3.MoveTowards(ballLogic.rb.velocity, new Vector3(moveDirection.normalized.x * rideMoveSpeed * Time.fixedDeltaTime, ballLogic.rb.velocity.y, moveDirection.normalized.z * rideMoveSpeed * Time.fixedDeltaTime), 7 * Time.deltaTime));
                }
                else
                {
                    ballLogic.rb.velocity = (Vector3.MoveTowards(ballLogic.rb.velocity, new Vector3(moveDirection.normalized.x * rideMoveSpeed * Time.fixedDeltaTime, ballLogic.rb.velocity.y, moveDirection.normalized.z * rideMoveSpeed * Time.fixedDeltaTime), ballRideAcceleration * Time.deltaTime));
                }
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
                            boostJumping = false;
                            RaycastHit shoveHit;
                            if (Physics.Raycast(shovePoint.position - (shovePoint.forward * shoveRadius), transform.forward, out shoveHit, shoveRadius * 3, whatCanShove))
                            {
                                GameObject shoveEft = Instantiate(shoveEffect, shoveHit.point - (shoveHit.transform.TransformDirection(shoveHit.transform.forward * 0.25f)), Quaternion.LookRotation(-shoveHit.normal), null);
                                Destroy(shoveEft, 1f);
                            }
                        }
                        else
                        {
                            shoveCol[i].GetComponent<Rigidbody>().velocity += ((shoveCol[i].transform.position - transform.position) * shoveForce / shoveCol[i].GetComponent<Rigidbody>().mass);
                            RaycastHit shoveHit;
                            if (Physics.Raycast(shovePoint.position - (shovePoint.forward * shoveRadius), transform.forward, out shoveHit, shoveRadius * 3, whatCanShove))
                            {
                                GameObject shoveEft = Instantiate(shoveEffect, shoveHit.point - (shoveHit.transform.TransformDirection(shoveHit.transform.forward * 0.2f)), Quaternion.LookRotation(-shoveHit.normal), null);
                                Destroy(shoveEft, 1f);
                            }
                        }
                    }
                    if (shoveCol[i].GetComponent<EnemyHealth>())
                    {
                        if (boostJumping)
                        {
                            shoveCol[i].GetComponent<EnemyHealth>().TakeDamage(0, 1.5f, (shoveCol[i].transform.position - transform.position).normalized, attackOutputID, transform.position);
                            boostJumping = false;
                        }
                        else
                        {
                            shoveCol[i].GetComponent<EnemyHealth>().TakeDamage(0, 1, (shoveCol[i].transform.position - transform.position).normalized, attackOutputID, transform.position);
                        }
                        GameObject shoveEft = Instantiate(shoveEffect, shovePoint.position + (transform.forward * 0.25f), shovePoint.rotation, null);
                    }
                }
            }
            else if (Physics.CheckSphere(shovePoint.position, shoveRadius, whatCanShoveOff))
            {
                Collider shoveOffCol = Physics.OverlapSphere(shovePoint.position, shoveRadius, whatCanShoveOff)[0];
                if (shoveOffCol != null && isGrounded == false)
                {
                    if (boostJumping)
                    {
                        wallShoveTime = maxWallShoveTime * 1.2f;
                        boostJumping = false;
                    }
                    else
                    {
                        wallShoveTime = maxWallShoveTime;
                    }
                    shoveCooldownTime = startShoveCooldownTime / 3;
                    RaycastHit shoveHit;
                    if (Physics.Raycast(shovePoint.position - (shovePoint.forward * shoveRadius), transform.forward, out shoveHit, shoveRadius * 3, whatCanShoveOff))
                    {
                    }
                    else if(Physics.Raycast(shovePoint.position - (shovePoint.forward * shoveRadius), -transform.right, out shoveHit, shoveRadius * 3, whatCanShoveOff))
                    {
                    }
                    else if (Physics.Raycast(shovePoint.position - (shovePoint.forward * shoveRadius), transform.right, out shoveHit, shoveRadius * 3, whatCanShoveOff))
                    {   
                    }
                    else if (Physics.Raycast(shovePoint.position, -transform.up, out shoveHit, shoveRadius * 3, whatCanShoveOff))
                    {
                    }
                    transform.rotation = Quaternion.LookRotation(new Vector3(-shoveHit.normal.x, 0, -shoveHit.normal.z));
                    GameObject shoveEft = Instantiate(shoveEffect, shoveHit.point - (shoveHit.transform.TransformDirection(shoveHit.transform.forward * 0.25f)), Quaternion.LookRotation(-shoveHit.normal), null);
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

    public void AlternateRun()
    {
        if (controls.Gameplay.LLegAction.WasReleasedThisFrame() && (alternateIndex == 0 || alternateIndex == 2))
        {
            alternateIndex = 1;
            curAltWindowTime = maxAltWindowTime;
        }
        if (controls.Gameplay.RLegAction.WasReleasedThisFrame() && (alternateIndex == 0 || alternateIndex == 1))
        {
            alternateIndex = 2;
            curAltWindowTime = maxAltWindowTime;
        }

        if (curAltWindowTime > 0)
        {
            if (controls.Gameplay.LLegAction.WasPressedThisFrame() && alternateIndex == 2)
            {
                alternateCount++;
            }
            else if (controls.Gameplay.RLegAction.WasPressedThisFrame() && alternateIndex == 1)
            {
                alternateCount++;
            }
            curAltWindowTime -= Time.deltaTime;
        }
        else
        {
            alternateCount = 0;
        }

        if (isGrounded)
        {
            if (alternateCount > 1)
            {
                runAlternating = true;
            }
            else
            {
                runAlternating = false;
            }
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

    public void ParticleOffset(int footID)
    {
        if (runAlternating)
        {
            altRunParticleEffect.transform.localPosition = new Vector3(0.15f * footID, 0, 0);
        }
        else if (isRunning)
        {
            runParticleEffect.transform.localPosition = new Vector3(0.15f * footID, 0, 0);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(shovePoint.position, shoveRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawRay(shovePoint.position - (shovePoint.forward * shoveRadius), transform.forward * shoveRadius * 2);

        if (isAttacking)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(swordHitBoxPoint.position, swordHitBoxRadius);
        }
    }
}
