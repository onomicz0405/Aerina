using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.VFX;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Max Horizontal Velocity")] public float maxVel = 80;
    [Tooltip("Speed while grounded")] public float runSpeed = 13.33333f;
    [Tooltip("Vertical force applied from jumping")] public float jumpHeight = 8.333333f;
    [Tooltip("Air Acceleration of air-strafing")] public float airAccel = 15;
    [Tooltip("Acceleration of running")] public float runAccelRate = 10;
    [Tooltip("Decceleration of running")] public float runDeccelRate = 5;
    public float slideDeccelRate = 10;
    public float slideAccel = 2;
    [Tooltip("Acceleration of air-drift")] public float driftRate = 0.02f;
    [Tooltip("Force applied from dashing")] public float dashSpeed = 15;
    public float dashCooldown = 0.5f;
    public float kickCooldown = 1f;
    public float wallJumpCooldown = 1f;
    public Vector3 kickVector;
    public GameObject kickHitbox;
    public bool nullMove = true;
    [Tooltip("How much the viewmodel moves from movement")] public float viewmodelSwayFactor = 0.6f;
    [Header("Dev Stuff")]
    public Transform respawnPoint;

    [HideInInspector] public Transform camTarget;
    [HideInInspector] public GameManager GM;
    [HideInInspector] public Transform cam;
    [HideInInspector] public Transform weapon;
    [HideInInspector] public Camera viewModelCam;
    [HideInInspector] public TextMeshProUGUI SpeedText;  
    [HideInInspector] public Transform teleportPoint = null;
    [HideInInspector] public WeaponHandler WH;
    [HideInInspector] public Transform feet;
    [HideInInspector] public Transform speedFX_parent;
    [HideInInspector] public GameObject speedFX;
    [HideInInspector] public bool isJump;
    [SerializeField] public bool grounded;
    [HideInInspector] public Vector3 MoveVector;
    [HideInInspector] public Vector3 TangentVector;
    [HideInInspector] public bool canJump;
    [HideInInspector] public bool isBhop;
    [HideInInspector] public bool canStrafe = true;
    [HideInInspector] public Collider currentGround;
    Rigidbody rb;
    float groundSpeedCap;
    RaycastHit slopeInfo;
    Vector3 normalVector;
    int groundLayerNum = (1 << 9);
    float horizontal_speed;
    Vector3 viewModelCenter;
    int moveX;
    int moveZ;
    float targetViewModelFOV;
    Vector3 targetViewModelPos;
    float targetViewmodelAngle;
    float targetCamAngle;
    Collider Last_Ground;
    List<Collider> capsuleCollisions = new List<Collider>();
    List<Collider> groundCollisions = new List<Collider>();
    bool lerpCam = false;
    float viewmodelFOV;
    bool canDash = true;
    Coroutine currentDashCooldown;
    Coroutine currentDashGroundTime;
    bool canKick = true;
    Coroutine currentKickCooldown;
    float dashViewmodelSway;
    float targetGroundSpeedCap;
    float viewmodelBob_target = 0.05f;
    float viewmodelBreathe_target = 0.05f;
    float viewmodel_lerpRate = 1;
    GameObject currentKickHitbox;
    bool crouched;
    [SerializeField] public bool sliding;
    float baseAirAccel;
    bool canWallJump = true;
    int wallJumpCount = 0; 

    private void Awake()
    {
        //set object references
        camTarget = GameObject.Find("camTarget").transform;
        GM = GameObject.Find("GameManager").GetComponent<GameManager>();
        cam = GameObject.Find("Main Camera").transform;
        weapon = GameObject.Find("ViewModel_parent").transform;
        viewModelCam = GameObject.Find("ViewModel_cam").GetComponent<Camera>();
        SpeedText = GameObject.Find("Speedometer_dev").GetComponent<TextMeshProUGUI>();
        feet = GameObject.Find("feet").transform;
        speedFX_parent = GameObject.Find("speedFX_parent").transform;
        speedFX = GameObject.Find("speed_effect_graph");
        //set Component references
        rb = GetComponent<Rigidbody>();
        WH = GM.GetComponent<WeaponHandler>();
        //set values
        cam.position = camTarget.position;
        viewModelCenter = weapon.localPosition;     
        viewmodelFOV = viewModelCam.fieldOfView;
        groundSpeedCap = runSpeed;
        targetGroundSpeedCap = runSpeed;
        speedFX_parent.gameObject.SetActive(true);
        baseAirAccel = airAccel;
        //start Corountines
        StartCoroutine(ViewmodelBob());
        StartCoroutine(ViewmodelBreathe());
    }

    // Update is called once per frame
    void Update()
    {
        MoveVectorCalculation();

        SlopeCalculation();

        Slide();

        RunCheck();

        AirMovement();       

        ViewModelMove();

        CamTilt();

        GroundSpeedCapLerp();

        MaxVelCap();

        JumpInput();

        DashInput();

        KickInput();

        CrouchInput();

        DevInputs();

        SpeedCameraEffect();
        
        //for speedometer
        StartCoroutine(WaitForFixedUpdate());
    }

    private void LateUpdate()
    {
        if (currentGround != null)
        {
            GroundStick(currentGround);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag == "ground")
        {
            capsuleCollisions.Add(collision.collider);           
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        Vector3 contactPoint = collision.GetContact(0).point;
        RaycastHit normalHit;
        Physics.Raycast(transform.position, contactPoint - transform.position, out normalHit, Mathf.Infinity, groundLayerNum, QueryTriggerInteraction.Ignore);
        Vector3 normal = normalHit.normal;
        if (!grounded && collision.transform.tag == "ground" && normal.y == 0)//90 degree wall check//REPLACE WITH WALL JUMP WALL TAG
        {
            //wall grind
            //cap vertical downwards speed if pressing into wall
            if (rb.velocity.y < -10 && Vector3.Angle(MoveVector,normal) > 135)
            {
                rb.velocity = new Vector3(rb.velocity.x, -10, rb.velocity.z);
            }
            if (canWallJump && GM.jumpKey.input)
            {
                //wall jump           
                WallJump(normal);
                StartCoroutine(WallJumpCooldown());
                StartCoroutine(WallJumpNoStrafe());
            }
        }       
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.transform.tag == "ground")
        {
            capsuleCollisions.Remove(collision.collider);
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        bool isWall = false;
        foreach (Collider c in capsuleCollisions)
        {
            if (c == collision)
            {
                isWall = true;               
            }
        }
        RaycastHit normal;
        Physics.Raycast(collision.ClosestPointOnBounds(transform.position) + Vector3.up, Vector3.down, out normal, Mathf.Infinity, groundLayerNum, QueryTriggerInteraction.Ignore);
        Physics.Raycast(transform.position, Vector3.down, out slopeInfo, Mathf.Infinity, groundLayerNum, QueryTriggerInteraction.Ignore);
        if (normal.collider != null)
        {

            if (normal.collider == collision && !isWall)
            {
                wallJumpCount = 0;
                //bhop
                //force bhop if kicking into ground
                if (collision.transform.tag == "ground" && (GM.jumpKey.input || currentKickHitbox != null) && normal.normal.normalized.y >= 0.7f)
                {
                    groundCollisions.Add(collision);
                    isBhop = true;
                    rb.useGravity = false;
                    isJump = true;
                    Jump();
                    if (currentKickHitbox != null)
                    {
                        Destroy(currentKickHitbox);
                    }                    
                }
                else if (collision.transform.tag == "ground")
                {
                    isBhop = false;                 
                }
            }
            else
            {
                isBhop = false;
            }
            
        }
    }

    private void OnTriggerStay(Collider collision)
    {
        if (isBhop && !GM.jumpKey.input)
        {
            isBhop = false;
        }
        if (collision.transform.tag == "ground" && !isBhop)
        {
            RaycastHit normal;
            Physics.Raycast(collision.ClosestPointOnBounds(transform.position) + Vector3.up, Vector3.down, out normal, Mathf.Infinity, groundLayerNum, QueryTriggerInteraction.Ignore);           
            Physics.Raycast(transform.position, Vector3.down, out slopeInfo, Mathf.Infinity, groundLayerNum, QueryTriggerInteraction.Ignore);
            if (normal.collider != null)
            {
                if (normal.collider == collision)
                {
                    currentGround = collision;
                }
                //land on walkable ground
                if ((collision == normal.collider && normal.normal.normalized.y >= 0.7f && !grounded) || /*jank saftey net check*/(grounded && isJump && rb.velocity.y <= 0))
                {
                    isJump = false;
                    grounded = true;
                    rb.useGravity = false;
                    canJump = true;
                    rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);                    
                    if (currentKickHitbox != null)
                    {
                        Destroy(currentKickHitbox);
                    }
                }
                //land on surf slope
                else if (collision == normal.collider && !(normal.normal.normalized.y >= 0.7f))
                {
                    isJump = false;
                    canJump = false;
                    grounded = false;
                }

            }
            else
            {
                grounded = false;
                rb.useGravity = true;
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.transform.tag == "ground")
        {
            grounded = false;
            rb.useGravity = true;
            if (!isJump && rb.velocity.y > 0)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            }
            groundCollisions.Remove(collision);
        }
    }

    void MoveVectorCalculation()
    {
        //sets move vector    
        if (GM.leftKey.inputDown || GM.rightKey.inputUp)
        {
            moveX = -1;
        }
        if (GM.rightKey.inputDown || GM.leftKey.inputUp)
        {
            moveX = 1;
        }
        if (GM.forwardKey.inputDown || GM.backwardKey.inputUp)
        {
            moveZ = 1;
        }
        if (GM.backwardKey.inputDown || GM.forwardKey.inputUp)
        {
            moveZ = -1;
        }

        if (!(GM.leftKey.input || GM.rightKey.input))
        {
            moveX = 0;
        }
        if (!(GM.forwardKey.input || GM.backwardKey.input))
        {
            moveZ = 0;
        }

        if (nullMove)
        {
            if (GM.leftKey.input && GM.rightKey.input)
            {
                moveX = 0;
            }
            if (GM.forwardKey.input && GM.backwardKey.input)
            {
                moveZ = 0;
            }
        }
        MoveVector = new Vector3(moveX, 0, moveZ);

        //set viewmodel sway values
        ViewModelCalculation();      

        //Movement Vector calculation
        float camDegree = -cam.eulerAngles.y;
        float moveDegree = (Mathf.Acos(MoveVector.normalized.x) / Mathf.PI) * 180;
        if (MoveVector.z < 0)
        {
            moveDegree *= -1;
        }
        float newDegree = camDegree + moveDegree;
        if (MoveVector.magnitude > 0)
        {
            MoveVector = new Vector3(Mathf.Cos(Mathf.PI * newDegree / 180),
              0,
              Mathf.Sin(Mathf.PI * newDegree / 180));

        }
    }

    void JumpInput()
    {
        //jump        
        if (grounded && !isJump && canJump && !isBhop)
        {
            if (GM.jumpKey.input)
            {
                isJump = true;
                Jump();
            }
        }
    }

    void DashInput()
    {
        //Dash
        //resets off cooldown
        if (canDash && GM.dashKey.inputDown && MoveVector.magnitude > 0)
        {
            Dash(MoveVector);
        }
    }

    void KickInput()
    {
        //Kick
        //resets off cooldown
        if (canKick && GM.kickKey.inputDown && !grounded)
        {
            Kick();
        }
    }

    void CrouchInput()
    {
        if (GM.crouchKey.input && !crouched)
        {
            Crouch();
        }
        else if ((GM.crouchKey.inputUp || GM.jumpKey.inputDown) && crouched)
        {
            Uncrouch();
        }

    }

    void Crouch()
    {
        crouched = true;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.6f, transform.localScale.z);
        targetGroundSpeedCap = runSpeed * 0.6f;
    }

    void Uncrouch()
    {
        crouched = false;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y / 0.6f, transform.localScale.z);
        targetGroundSpeedCap = runSpeed;
    }

    void Slide()
    {
        float horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        //start sliding if at least dash speed or going down a slope while faster than crouching
        if(GM.crouchKey.input && grounded && (horizontalVel >= dashSpeed || (TangentVector.normalized.y < -0.17f && horizontalVel > runSpeed * 0.6f/*crouch speed*/)))
        {
            SlideAccel();           
        }
        //keep sliding if still faster than 2 u/s
        else if (GM.crouchKey.input && sliding && horizontalVel > 2 && grounded)
        {
            SlideAccel();
        }
        else
        {
            sliding = false;
            airAccel = baseAirAccel;
        }
    }

    void SlideAccel()
    {
        //slide
        sliding = true;
        airAccel = slideAccel;

        if (TangentVector.normalized.y < -0.17f) // 9.740283 degree angle slope
        {
            //accel
            rb.velocity += TangentVector.normalized * slideAccel * Time.deltaTime * (TangentVector.normalized.y/-0.17f);
        }
        else
        {
            //deccel 
            //check if going up a slope
            if (TangentVector.normalized.y > 0.17f)
            {
                rb.velocity -= rb.velocity.normalized * slideDeccelRate * 2 * Time.fixedDeltaTime;
            }
            else
            {
                rb.velocity -= rb.velocity.normalized * slideDeccelRate * Time.fixedDeltaTime;
            }

            //check if velocity is smaller than the deccel, if so, stop
            if (rb.velocity.magnitude <= (rb.velocity.normalized * slideDeccelRate * Time.fixedDeltaTime).magnitude)
            {
                rb.velocity *= 0;
            }
        }
    }

    void DevInputs()
    {
        //respawn for dev
        if (Input.GetKeyDown(KeyCode.G))
        {
            Respawn();
        }
    }

    void SpeedCameraEffect()
    {
        //speed camera effect
        float speedAlpha = Mathf.Lerp(0, 1, Mathf.Max(0, horizontal_speed - (runSpeed + dashSpeed)) / (40f - (runSpeed + dashSpeed)));
        float alpha = Mathf.Lerp(speedFX.GetComponent<VisualEffect>().GetFloat("alpha"), 0, 10f * Time.deltaTime);
        if (alpha < 0.1f)
        {
            alpha = 0;
        }
        Vector3 horizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z).normalized;
        Vector3 camForward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        Vector3 camRight = new Vector3(cam.right.x, 0, cam.right.z).normalized;
        float offsetAngle = Vector3.Angle(camForward, horizontal);
        if (Vector3.Angle(camRight, horizontal) > 90)
        {
            offsetAngle = -Vector3.Angle(camForward, horizontal);
        }
        speedFX_parent.localEulerAngles = new Vector3(0, cam.localEulerAngles.y, 0);
        speedFX_parent.localEulerAngles += new Vector3(0, offsetAngle, 0);
        if (speedAlpha > 0.1f)
        {
            alpha = speedAlpha;
        }
        speedFX.GetComponent<VisualEffect>().SetFloat("alpha", alpha);
    }

    void SlopeCalculation()
    {
        float horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        if (currentGround != null)
        {
            Physics.Raycast(currentGround.ClosestPointOnBounds(transform.position) + Vector3.up, Vector3.down, out slopeInfo, Mathf.Infinity, groundLayerNum, QueryTriggerInteraction.Ignore);
        }      
        if (slopeInfo.collider != null)
        {
            normalVector = slopeInfo.normal.normalized;
            TangentVector = -Vector3.Cross(normalVector, Vector3.Cross(Vector3.up, new Vector3(rb.velocity.x, 0, rb.velocity.z).normalized).normalized);
            //if not sliding and has movement inputs, return run tangent
            if (!(sliding || (GM.crouchKey.input && GM.crouchKey.input && grounded && (horizontalVel >= dashSpeed || (TangentVector.normalized.y < -0.17f && horizontalVel > runSpeed * 0.6f)))) && MoveVector.magnitude > 0)
            {
                TangentVector = -Vector3.Cross(normalVector, Vector3.Cross(Vector3.up, MoveVector.normalized).normalized);
            }
            
        }
    }

    void RunCheck()
    {
        //running
        if (grounded && !isJump && !sliding)
        {
            Run();
        }
    }

    void Run()
    {
        //deccel
        if (MoveVector.magnitude == 0)
        {
            
            rb.velocity -= TangentVector.normalized * (runSpeed) * runDeccelRate * Time.fixedDeltaTime;
            //check if velocity is smaller than the deccel, if so, stop
            if (rb.velocity.magnitude <= (rb.velocity.normalized * (runSpeed) * runDeccelRate * Time.fixedDeltaTime).magnitude)
            {
                rb.velocity *= 0;
            }
            rb.velocity = TangentVector.normalized * rb.velocity.magnitude;
        }
        //accel
        else
        {
            rb.velocity += (TangentVector.normalized * (runSpeed) * runAccelRate * Time.fixedDeltaTime);
        }

        

        //cap speed
        if (rb.velocity.magnitude > groundSpeedCap)
        {
            rb.velocity = rb.velocity.normalized * (groundSpeedCap);
        }

    }

    void AirMovement()
    {
        //air strafe
        if ((!grounded && MoveVector.magnitude > 0 && canStrafe) || sliding)
        {
            AirAccelerate(MoveVector * runSpeed);
            if (horizontal_speed <= runSpeed && !sliding)
            {
                AirDrift();
            }
        }
    }

    void AirDrift()
    {        
        float Y_value = rb.velocity.y;

        rb.velocity += (MoveVector.normalized * (runSpeed / 10) * runAccelRate * Time.fixedDeltaTime);

        rb.velocity = new Vector3(rb.velocity.x, Y_value,rb.velocity.z);
        Vector3 horizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (horizontal.magnitude >= runSpeed)
        {
           rb.velocity = new Vector3(horizontal.normalized.x * runSpeed, Y_value, horizontal.normalized.z * runSpeed);
        }
    }

    public void Jump()
    {       
        Vector3 flatVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        //resets y velocity
        if (currentKickHitbox != null && GM.jumpKey.input)
        {
            rb.velocity = new Vector3(0, jumpHeight * 1.5f, 0) + flatVel;
        }
        else
        {
            rb.velocity = new Vector3(0, jumpHeight, 0) + flatVel;
        }
        StartCoroutine(WallJumpCooldown());
    }

    void WallJump(Vector3 normal)
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);       
        rb.velocity = new Vector3(0, jumpHeight * Mathf.Max(0, 2 - 0.5f * wallJumpCount), 0) + flatVel + normal * jumpHeight;               
        wallJumpCount++;
    }

    void ViewModelCalculation()
    {
        Vector3 horizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z).normalized;
        Vector3 camForward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        Vector3 camRight = new Vector3(cam.right.x, 0, cam.right.z).normalized;
        dashViewmodelSway = Mathf.Sqrt(Mathf.Max(0, horizontal_speed - runSpeed)) * 2 * viewmodelSwayFactor;
        //Fov
        //input influence
        if (MoveVector.z > 0)
        {
            //forward
            targetViewModelFOV = viewmodelFOV - 3 * viewmodelSwayFactor;
        }
        else if (MoveVector.z < 0)
        {
            //back
            targetViewModelFOV = viewmodelFOV + 3 * viewmodelSwayFactor;
        }
        else
        {
            targetViewModelFOV = viewmodelFOV;
        }
        //velocity influence
        targetViewModelFOV += Mathf.Lerp(-dashViewmodelSway*2, dashViewmodelSway, Vector3.Angle(camForward, horizontal) / 180);

        //Position
        //input influence 
        if (MoveVector.x > 0)
        {
            //Right
            targetViewModelPos = viewModelCenter + new Vector3(-0.2f * viewmodelSwayFactor, 0, 0);
        }
        else if (MoveVector.x < 0)
        {
            //Left
            targetViewModelPos = viewModelCenter + new Vector3(0.2f * viewmodelSwayFactor, 0, 0);
        }
        else
        {
            targetViewModelPos = viewModelCenter;
        }
        //walk bob
        if (grounded && MoveVector.magnitude != 0)
        {
            viewmodel_lerpRate = 0.75f;
            targetViewModelPos += new Vector3(0, viewmodelBob_target, 0);
        }
        //breathe idle
        else if (grounded && MoveVector.magnitude == 0)
        {
            viewmodel_lerpRate = 0.25f;
            targetViewModelPos += new Vector3(0, viewmodelBreathe_target, 0);
        }
        else
        {
            viewmodel_lerpRate = 1f;
            if (rb.velocity.y > 0)
            {
                targetViewModelPos += new Vector3(0, -0.2f, 0);
            }
            else if (rb.velocity.y < 0)
            {
                //bad math, who cares it works
                targetViewModelPos = new Vector3(targetViewModelPos.x, -0.2f + Mathf.Min(0.5f, 0.2f * -rb.velocity.y * 0.1f), targetViewModelPos.z);
            }
            else
            {
                targetViewModelPos = new Vector3(targetViewModelPos.x, 0, targetViewModelPos.z);
            }
        }
        
        //velocity influence
        targetViewModelPos += Vector3.Lerp(-new Vector3(dashViewmodelSway / 15, 0, 0), new Vector3(dashViewmodelSway / 20, 0, 0), Vector3.Angle(camRight, horizontal) / 180);

        //Rotation
        //input influence
        if (MoveVector.x > 0)
        {
            //Right
            targetViewmodelAngle = -10 * viewmodelSwayFactor;
        }
        else if (MoveVector.x < 0)
        {
            //Left
            targetViewmodelAngle = 10 * viewmodelSwayFactor;
        }
        else
        {
            targetViewmodelAngle = 0;
        }
        //velocity influence
        targetViewmodelAngle += Mathf.Lerp(-dashViewmodelSway, dashViewmodelSway, Vector3.Angle(camRight, horizontal) / 180);
       
    }

    void ViewModelMove()
    {    
        viewModelCam.fieldOfView = Mathf.Lerp(viewModelCam.fieldOfView, targetViewModelFOV, 10f * Time.deltaTime);
        weapon.localPosition = Vector3.Lerp(weapon.localPosition, targetViewModelPos, 6f * Time.deltaTime * viewmodel_lerpRate);
        weapon.localRotation = Quaternion.Lerp(weapon.localRotation, Quaternion.Euler(0, 0, targetViewmodelAngle), 10f * Time.deltaTime);     
    }

    void CamTilt()
    {
        //cam tilt
        if (targetCamAngle != 0)
        {
            if (Mathf.Abs(Mathf.Lerp(cam.GetComponent<CamMovement>().tilt, targetCamAngle, 19f * Time.deltaTime)) > 0.1f)
            {
                cam.GetComponent<CamMovement>().tilt = Mathf.Lerp(cam.GetComponent<CamMovement>().tilt, targetCamAngle, 10f * Time.deltaTime);
            }
            else
            {
                cam.GetComponent<CamMovement>().tilt = targetCamAngle;
            }
        }
        else
        {
            if (Mathf.Abs(Mathf.Lerp(cam.GetComponent<CamMovement>().tilt, targetCamAngle, 4f * Time.deltaTime)) > 0.1f)
            {
                cam.GetComponent<CamMovement>().tilt = Mathf.Lerp(cam.GetComponent<CamMovement>().tilt, targetCamAngle, 4f * Time.deltaTime);
            }
            else
            {
                cam.GetComponent<CamMovement>().tilt = targetCamAngle;
            }
        }
    }

    void GroundSpeedCapLerp()
    {
        //ground speed cap
        groundSpeedCap = Mathf.Lerp(groundSpeedCap, targetGroundSpeedCap, 10f * Time.deltaTime);
        if (groundSpeedCap < targetGroundSpeedCap + 0.1f)
        {
            groundSpeedCap = targetGroundSpeedCap;
        }
    }

    void MaxVelCap()
    {
        //maxvel
        if (horizontal_speed > maxVel)
        {
            rb.velocity = new Vector3(rb.velocity.normalized.x * maxVel, rb.velocity.y, rb.velocity.normalized.z * maxVel);
        }
    }

    public void Respawn()
    {
        rb.velocity *= 0;
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
        }

    }

    public void Teleport()
    {
        rb.velocity *= 0;
        transform.position = teleportPoint.position;
    }

    void GroundStick(Collider collider)
    {
        //level player collision box to the ground
        RaycastHit normal;
        float raycastDistance = GetComponent<BoxCollider>().size.y - GetComponent<BoxCollider>().center.y + 0.15f;
        //this sucks
        Physics.Raycast(collider.ClosestPointOnBounds(transform.position) + Vector3.up, Vector3.down, out normal, raycastDistance, groundLayerNum, QueryTriggerInteraction.Ignore);
        if (normal.collider != null)
        {
            if (grounded && !isJump && collider == normal.collider)
            {
                if (!sliding)
                {
                    StartCoroutine(WaitForLerpCam());
                }
                Vector3 distance = transform.position - feet.position;
                transform.position = new Vector3(transform.position.x, normal.point.y + distance.y, transform.position.z);
            }
            
        }
        
        if (lerpCam)
        {
            cam.position = new Vector3(camTarget.position.x, Mathf.Lerp(cam.position.y, camTarget.position.y, 16f * Time.deltaTime), camTarget.position.z);
        }
        else
        {
            cam.position = camTarget.position;
        }
       
    }

    void AirAccelerate(Vector3 MoveDir)
    {
        Vector3 horizontal_vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float wish_speed, current_speed, add_speed, accel_speed;

        wish_speed = MoveDir.magnitude/5; //make wish_speed smaller to get less speed gain from strafing

        current_speed = Vector3.Dot(horizontal_vel, MoveDir);
        add_speed = wish_speed - current_speed;

        if (add_speed <= 0)
        {
            return;
        }

        if (sliding)
        {
            accel_speed = airAccel * runSpeed * 0.4f * Time.fixedDeltaTime;
        }
        else
        {
            accel_speed = airAccel * runSpeed * Time.fixedDeltaTime;
        }
        
        //caps accel
        if (accel_speed > add_speed)
        {
            accel_speed = add_speed;
        }

        rb.velocity += MoveDir.normalized * accel_speed;
    }

    void Dash(Vector3 MoveDir)
    {
        //apply dash
        canDash = false;
        if (currentDashCooldown != null)
        {
            StopCoroutine(currentDashCooldown);
        }
        if (currentDashGroundTime != null)
        {
            StopCoroutine(DashGroundTime());
        }       
        currentDashCooldown = StartCoroutine(DashCooldown());
        currentDashGroundTime = StartCoroutine(DashGroundTime());
        ApplyMovement(MoveDir * dashSpeed, runSpeed + dashSpeed);
        //cam tilt
        cam.GetComponent<CamMovement>().tilt = 0;
        Vector3 horizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z).normalized;
        Vector3 camForward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        Vector3 camRight = new Vector3(cam.right.x, 0, cam.right.z).normalized;
        if (Vector3.Angle(camForward, horizontal) / 180 < .9f && Vector3.Angle(camForward,horizontal) / 180 > .1f)
        {
            targetCamAngle = Mathf.Lerp(-5, 5, Vector3.Angle(camRight, horizontal)/180);
            if (targetCamAngle > 0)
            {
                targetCamAngle = 5;
            }
            else if (targetCamAngle < 0)
            {
                targetCamAngle = -5;
            }
        }
    }

    void Kick()
    {
        canKick = false;
        if (currentKickCooldown != null)
        {
            StopCoroutine(currentKickCooldown);
        }
        currentKickCooldown = StartCoroutine(KickCooldown());
        Vector3 currentKickVector = new Vector3(cam.forward.x * kickVector.z, kickVector.y, cam.forward.z * kickVector.z);
        ApplyMovement(currentKickVector, 35f);
        //spawn hitbox, destroy it when it hits an enemy or player becomes grounded or bhops, or after 1 second
        currentKickHitbox = Instantiate(kickHitbox, gameObject.transform, false);
        Destroy(currentKickHitbox, 1f);
    }

    void ApplyMovement(Vector3 force, float speedCap)
    {
        if (force.magnitude == 0)
        {
            return;
        }
        Vector3 currentVel = rb.velocity;
        Vector3 newVel = new Vector3(0, 0, 0);

        //if the currentVel and the force of the source are going the same direction, add them together, else, just set it equal to the force; check for each axis
        if (Vector3.Angle(force,currentVel) < 90)
        {
            newVel = force + currentVel;
        }
        else
        {
            newVel = force;
        }
        if (newVel.y * currentVel.y > 0)
        {
            newVel.y = force.y + currentVel.y;
        }
        else if (force.y != 0) 
        {
            newVel.y = force.y;
        }

        //horizontal speed cap
        Vector3 horizontal = new Vector3(newVel.x, 0, newVel.z);
        if (horizontal.magnitude > (horizontal.normalized * speedCap).magnitude)
        {
            newVel = new Vector3(horizontal.normalized.x * speedCap, newVel.y, horizontal.normalized.z * speedCap);
            //if speed cap results in slowing, only apply vertical movement
            if (newVel.magnitude < rb.velocity.magnitude)
            {
                rb.velocity = new Vector3(rb.velocity.x, newVel.y, rb.velocity.z);
                return;
            }
        }
        rb.velocity = newVel;
    }

    IEnumerator WaitForFixedUpdate()
    {
        yield return new WaitForFixedUpdate();
        
        //speed text UI
        horizontal_speed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        SpeedText.text = horizontal_speed.ToString("F3");
    }

    IEnumerator WaitForLerpCam()
    {   
        lerpCam = true;
        yield return new WaitUntil(() => (cam.position - camTarget.position).magnitude < 0.1f);
        lerpCam = false;
    }

    IEnumerator DashGroundTime()
    {
        //dash cap is runspeed + dashspeed
        if (crouched)
        {
            groundSpeedCap = (runSpeed + dashSpeed) * 0.6f;
            targetGroundSpeedCap = (runSpeed + dashSpeed) * 0.6f;
        }
        else
        {
            groundSpeedCap = runSpeed + dashSpeed;
            targetGroundSpeedCap = runSpeed + dashSpeed;
        }
        speedFX.GetComponent<VisualEffect>().SetFloat("alpha", 1);
        yield return new WaitForSeconds(.25f);
        if (crouched)
        {
            targetGroundSpeedCap = runSpeed * 0.6f;
        }
        else
        {
            targetGroundSpeedCap = runSpeed;
        }
        targetCamAngle = 0;
    }

    IEnumerator DashCooldown()
    {
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    IEnumerator KickCooldown()
    {
        yield return new WaitForSeconds(kickCooldown);
        canKick = true;
    }

    IEnumerator WallJumpCooldown()
    {
        canWallJump = false;
        yield return new WaitForSeconds(wallJumpCooldown);
        canWallJump = true;
    }

    IEnumerator WallJumpNoStrafe()
    {
        canStrafe = false;
        yield return new WaitForSeconds(0.25f);
        canStrafe = true;
    }

    IEnumerator ViewmodelBob()
    {
        yield return new WaitForSeconds(0.3f);
        viewmodelBob_target *= -1;
        StartCoroutine(ViewmodelBob());
    }

    IEnumerator ViewmodelBreathe()
    {
        yield return new WaitForSeconds(1.25f);
        viewmodelBreathe_target *= -1;
        StartCoroutine(ViewmodelBreathe());
    }
}
