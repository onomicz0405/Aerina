using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.VFX;

public enum WEAPONS { NONE, ROCKET, RAILGUN, BALL, SWORD, SCATTERGUN };
public class WeaponHandler : MonoBehaviour
{   
    [Serializable] 
    public class SwordSettingsHolder
    {
        public float Sword_fireRate;
        public float SwordProjectileSpeed;
        public float SwordDashSpeed;
        public float Sword_damage;
    }
    [Header("Weapon Settings")]
    public SwordSettingsHolder SwordSettings = new SwordSettingsHolder();
    [Serializable]
    public class ScattergunSettingsHolder
    {
        public float SG_fireRate = .75f;
        public float SG_damage = 18;
        public float ThornProjectileSpeed = 60;
    }
    public ScattergunSettingsHolder ScattergunSettings = new ScattergunSettingsHolder();
    [Serializable]
    public class RailgunSettingsHolder
    {
        public float RG_damage = 80;
    }
    public RailgunSettingsHolder RailgunSettings = new RailgunSettingsHolder();
    [Serializable]
    public class OtherSettingsHolder
    {
        public int Weapon_charge_time = 42;
    }
    public OtherSettingsHolder OtherSettings = new OtherSettingsHolder();
    

    [HideInInspector] public bool canFire = true;
    [HideInInspector] public bool isFire;
    [HideInInspector] public float rocketSpeed;
    [HideInInspector] public float RL_fireRate;
    [HideInInspector] public float RG_fireRate;
    [HideInInspector] public float railForce;
    [HideInInspector] public float Item_fireRate;
    [HideInInspector] public float ItemThrowForce;
    [HideInInspector] public float pushUpScale;
    [HideInInspector] public LayerMask layerMask; //layers for registering weapon attacks
    [HideInInspector] public int noneIndex = 0;
    [HideInInspector] public int swordIndex = 1;
    [HideInInspector] public int scattergunIndex = 2;
    [HideInInspector] public int railgunIndex = 3;
    [HideInInspector] public float Weapon_charge_frames_count;


    [Header("Dev Stuff")]
    public List<string> weaponInventory = new List<string>();
    public float airAnimBlendRate = 0.1f;

    [HideInInspector] public GameManager GM;
    [HideInInspector] public Transform Player;
    [HideInInspector] public WEAPONS currentWeapon = WEAPONS.NONE;
    [HideInInspector] public string currentWeapon_Name;    
    public List<Animator> AnimationControllers = new List<Animator>();
    [HideInInspector] public List<Transform> bulletOrigins = new List<Transform>();
    [HideInInspector] public List<GameObject> viewmodels = new List<GameObject>();
    [HideInInspector] public Transform currentBulletOrigin;
    [HideInInspector] public GameObject RailLaser;
    [HideInInspector] public GameObject SG_tracer;
    [HideInInspector] public GameObject RailImpactVFX;
    [HideInInspector] public GameObject SGImpactVFX;
    [HideInInspector] public GameObject SwordImpactVFX;
    [HideInInspector] public GameObject Rocket;
    [HideInInspector] public GameObject Sword_Projectile;
    [HideInInspector] public GameObject Thorn_Projectile;
    [HideInInspector] public GameObject Sword_Charged_Hitbox;
    [HideInInspector] public Transform cam;
    [HideInInspector] public GameObject SwordBar;
    [HideInInspector] public Animator viewModelAnim = null;

    public GameObject FireballProjectile;
    public float Fireball_speed;
    public GameObject Crystal;
    public GameObject currentReticle;
    Vector3 targetReticleScale = new Vector3(0.3f,0.3f,0.3f);

    bool Weapon_charged;
    int weaponIndex;
    int swordChainAttack_Index;
    Coroutine currentAnimWait;

    Rigidbody rb;
    RaycastHit hitInfo;
    WEAPONS lastWeapon = WEAPONS.NONE;

    private void Awake()
    {
        //set object references
        GM = GetComponent<GameManager>();
        Player = GameObject.Find("Player").transform;
        bulletOrigins[0] = GameObject.Find("railorigin").transform;
        viewmodels[0] = GameObject.Find("Fists_parent");
        viewmodels[1] = GameObject.Find("Sword_parent");
        viewmodels[2] = GameObject.Find("Scattergun_parent");
        viewmodels[3] = GameObject.Find("Railgun_parent");
        cam = GameObject.Find("Main Camera").transform;
        SwordBar = GameObject.Find("SwordBar");
        //set values
        rb = Player.GetComponent<Rigidbody>();
        SetNone();
    }

    private void Update()
    {      
        //regular fire
        if (GM.fireKey.input && canFire && Weapon_charge_frames_count == 0)
        {
            switch (currentWeapon)
            {
                case WEAPONS.NONE:
                    Fire_None();
                    break;
                case WEAPONS.SWORD:
                    Fire_Sword();
                    break;
                case WEAPONS.SCATTERGUN:
                    Fire_Scattergun();
                    break;
                case WEAPONS.RAILGUN:
                    Fire_Railgun();
                    break;               
            }
        }
              
        //charged inputs
        if ((GM.altFireKey.inputUp || GM.fireKey.input) && canFire && currentWeapon != WEAPONS.NONE)
        {
            switch (currentWeapon)
            {
                case WEAPONS.SWORD:
                    Fire_Sword();
                    break;
                case WEAPONS.SCATTERGUN:
                    Fire_Scattergun();
                    break;
            }
            Weapon_charged = false;
            Weapon_charge_frames_count = 0;
            SwordBar.GetComponent<Slider>().value = 0;
        }

        //charged input tracker
        if (GM.altFireKey.input && canFire)
        {
            if (Weapon_charge_frames_count < OtherSettings.Weapon_charge_time)
            {
                Weapon_charge_frames_count += Time.deltaTime * 60;
                if (Weapon_charge_frames_count > 10)
                {
                    SwordBar.GetComponent<Slider>().value = Weapon_charge_frames_count / OtherSettings.Weapon_charge_time;
                    //play charge anim
                    if (!viewModelAnim.GetCurrentAnimatorStateInfo(0).IsName("Charge"))
                    {
                        viewModelAnim.Play("Charge", 0);
                    }                    
                }
            }
            else
            {
                Weapon_charged = true;
            }
        }
        else
        {
            Weapon_charged = false;
            Weapon_charge_frames_count = 0;
            SwordBar.GetComponent<Slider>().value = 0;
        }

        //weapon swap buttons
        if (GM.fistsSwapKey.inputDown && currentWeapon != WEAPONS.NONE)
        {
            SetNone();
        }
        if (GM.swordSwapKey.inputDown && currentWeapon != WEAPONS.SWORD && weaponInventory.Contains("Sword"))
        {
            SetSword();
        }
        if (GM.scattergunSwapKey.inputDown && currentWeapon != WEAPONS.SCATTERGUN && weaponInventory.Contains("Scattergun"))
        {
            SetScattergun();
        }
        if (GM.railgunSwapKey.inputDown && currentWeapon != WEAPONS.RAILGUN && weaponInventory.Contains("Railgun"))
        {
            SetRailgun();
        }
               
        //weapon cycle
        if ((GM.cycleWeaponKey.input || GM.cycleWeaponBackKey.input) && weaponInventory.Count > 1)
        {
            
            //check order
            switch (currentWeapon)
            {
                case WEAPONS.NONE:
                    weaponIndex = noneIndex;
                    break;
                case WEAPONS.SWORD:
                    weaponIndex = swordIndex;
                    break;
                case WEAPONS.SCATTERGUN:
                    weaponIndex = scattergunIndex;
                    break;
                case WEAPONS.RAILGUN:
                    weaponIndex = railgunIndex;
                    break;
            }

            weaponIndex += (GM.cycleWeaponKey.input ? 1 : 0) - (GM.cycleWeaponBackKey.input ? 1 : 0);

            if (weaponIndex > weaponInventory.Count)
            {
                weaponIndex = 0;
            }
            else if (weaponIndex < 0)
            {
                weaponIndex = weaponInventory.Count;
            }

            //set weapon
            if (weaponIndex == noneIndex && currentWeapon != WEAPONS.NONE)
            {
                SetNone();
            }
            if (weaponIndex == swordIndex && currentWeapon != WEAPONS.SWORD)
            {
                SetSword();
            }
            if (weaponIndex == scattergunIndex && currentWeapon != WEAPONS.SCATTERGUN)
            {
                SetScattergun();
            }
            if (weaponIndex == railgunIndex && currentWeapon != WEAPONS.RAILGUN)
            {
                SetRailgun();
            }

        }

        
        //set idle anim of viewmodel
        if (!isFire && viewModelAnim != null && Weapon_charge_frames_count == 0)
        {
            if (Player.GetComponent<Movement>().grounded)
            {
                viewModelAnim.SetFloat("Ground_blend", Mathf.Lerp(viewModelAnim.GetFloat("Ground_blend"), 1f, airAnimBlendRate * Time.deltaTime));
            }
            else
            {
                viewModelAnim.SetFloat("Ground_blend", Mathf.Lerp(viewModelAnim.GetFloat("Ground_blend"), 0f, airAnimBlendRate * Time.deltaTime));
                Vector3 Move = Player.GetComponent<Movement>().MoveVector;
                float diff = 0.5f;
                if (Move.magnitude != 0)
                {
                    diff = Vector3.Angle(-cam.right, Move) / 180;
                }            
                viewModelAnim.SetFloat("Air_blend", Mathf.Lerp(viewModelAnim.GetFloat("Air_blend"), diff, airAnimBlendRate * Time.deltaTime));               
            }
            viewModelAnim.Play("Idle", 0);
        }
        else
        {
            viewModelAnim.SetFloat("Air_blend", 0.5f);
            viewModelAnim.SetFloat("Ground_blend", 1f);
        }

        //relax reticle to idle
        currentReticle.transform.localScale = Vector3.Lerp(currentReticle.transform.localScale, targetReticleScale, 6 * Time.deltaTime);
        //velocity influence on reticle scale
        targetReticleScale = new Vector3(0.3f,0.3f,0.3f) * (Mathf.Min(0.25f, rb.velocity.magnitude / 112) + 1);
    }

    public void SetNone()
    {
        for (int x = 0; x < viewmodels.Count; x++)
        {           
            viewmodels[x].SetActive(false);           
        }
        viewmodels[0].SetActive(true);
        currentWeapon = WEAPONS.NONE;
        currentWeapon_Name = "None";
        viewModelAnim = AnimationControllers[0];
        SetAnimBlend();
        Player.GetComponent<Movement>().viewmodelSwayFactor = 0.2f;
        isFire = false;
        Crystal.GetComponent<Crystal_Anims>().ResetValues();
    }

    public void SetSword()
    {
        for (int x = 0; x < viewmodels.Count; x++)
        {
            viewmodels[x].SetActive(false);
        }
        viewmodels[1].SetActive(true);
        currentWeapon = WEAPONS.SWORD;
        currentWeapon_Name = "Sword";
        currentBulletOrigin = bulletOrigins[0];
        viewModelAnim = AnimationControllers[1];
        Player.GetComponent<Movement>().viewmodelSwayFactor = 0.3f;
        isFire = false;
    }

    public void SetScattergun()
    {
        for (int x = 0; x < viewmodels.Count; x++)
        {
            viewmodels[x].SetActive(false);
        }
        viewmodels[2].SetActive(true);
        currentWeapon = WEAPONS.SCATTERGUN;
        currentWeapon_Name = "Scattergun";
        currentBulletOrigin = bulletOrigins[0];
        viewModelAnim = AnimationControllers[2];
        Player.GetComponent<Movement>().viewmodelSwayFactor = 0.6f;
    }

    public void SetRailgun()
    {
        for (int x = 0; x < viewmodels.Count; x++)
        {
            viewmodels[x].SetActive(false);
        }
        viewmodels[3].SetActive(true);
        currentWeapon = WEAPONS.RAILGUN;
        currentWeapon_Name = "Railgun";
        currentBulletOrigin = bulletOrigins[0];
        viewModelAnim = AnimationControllers[3];
        Player.GetComponent<Movement>().viewmodelSwayFactor = 0.6f;
    }

    public void Fire_None()
    {
        //play animation
        viewModelAnim.Play("Fire_Cast", 0);
        Crystal.GetComponent<Crystal_Anims>().CastSpellAnim();
        if (currentAnimWait != null)
        {
            StopCoroutine(currentAnimWait);
        }
        currentAnimWait = StartCoroutine(WaitForAnim(viewModelAnim));
        //vfx
        VisualEffect vfx = GameObject.Find("fire_flash").GetComponent<VisualEffect>();
        vfx.enabled = true;
        //cast spell
        GameObject fireball = Instantiate(FireballProjectile, new Vector3(cam.position.x, cam.position.y - 0.2f, cam.position.z) + (cam.right * 1f), Quaternion.identity);
        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        fireball.GetComponent<Rigidbody>().velocity = (cam.forward + cam.up*0.2f + -cam.right *0.025f)* Fireball_speed + (cam.forward * Mathf.Max(0, Vector3.Dot(cam.forward, horizontalVel)) / 2);
        StartCoroutine(DestroyObj(fireball, 5));
        StartCoroutine(WaitForVFX(vfx, 0.6f));
        //fire rate
        StartCoroutine(FireRate(1f));
    }
     
    void Fire_Sword()
    {        
        switch (swordChainAttack_Index)
        {
            case 0:
                AnimationControllers[1].Play("Sword_Fire", 0);
                swordChainAttack_Index++;
                break;
            case 1:
                AnimationControllers[1].Play("Sword_Fire_2", 0);
                swordChainAttack_Index = 0;
                break;
            case 2:
                //3rd anim
                AnimationControllers[1].Play("Sword_Fire_2", 0);
                swordChainAttack_Index = 0;
                break;
        }
        if (currentAnimWait != null)
        {
            StopCoroutine(currentAnimWait);
        }
        currentAnimWait = StartCoroutine(WaitForAnim(AnimationControllers[1]));
        StartCoroutine(SwordSlashDelay(Weapon_charged));
        StartCoroutine(FireRate(SwordSettings.Sword_fireRate));
        currentReticle.transform.localScale *= 1.25f;
    }

    void Fire_Scattergun()
    {
        AnimationControllers[2].Play("Scattergun_Fire", 0);
        StartCoroutine(WaitForAnim(AnimationControllers[2]));
        Scattergun_Shot(Weapon_charged);
    }

    void Fire_Railgun()
    {
        AnimationControllers[3].Play("Railgun_Fire", 0);
        StartCoroutine(WaitForAnim(AnimationControllers[3]));
        RailShot();
    }

    void Fire_Rocket()
    {
        RocketLaunch();
    }

    void Fire_Item()
    {
        AnimationControllers[0].Play("Item_Fire", 0);
        StartCoroutine(WaitForAnim(AnimationControllers[0]));
        ItemThrow();
    }

    void SwordSlash(bool chargedSlash)
    {
        //charge slash to launch player forward
        if (chargedSlash)
        {
            float dashSpeed = SwordSettings.SwordDashSpeed;
            Vector3 camForward = rb.GetComponent<Movement>().cam.forward;
            //check if it will make player faster horizontally, if so, limit speed gain
            Vector3 horizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            Vector3 dashInfluence = new Vector3();
            //if dashing upwards or downwards, cut vertical force in half
            if (camForward.y != 0)
            {
                dashInfluence = new Vector3(camForward.x, camForward.y * 0.5f, camForward.z);
            }
            else
            {
                dashInfluence = camForward;
            }
            ApplyMovement(dashSpeed * dashInfluence);
            if (!rb.GetComponent<Movement>().isJump)
            {
                rb.GetComponent<Movement>().isJump = true;
                rb.GetComponent<Movement>().Jump();
            }
            //add hitbox to player
            GameObject hitbox = Instantiate(Sword_Charged_Hitbox, Player, false);
            StartCoroutine(DestroyObj(hitbox, 1f));
        }     
        //basic attack
        else
        {            
            //spawn projectile            
            Quaternion rotation = Quaternion.identity;
            rotation.eulerAngles = cam.rotation.eulerAngles + new Vector3(0, 0, 0);
            GameObject projectile = Instantiate(Sword_Projectile, new Vector3(cam.position.x, cam.position.y - 0.1f, cam.position.z), rotation);
            projectile.GetComponent<Rigidbody>().velocity = cam.forward * SwordSettings.SwordProjectileSpeed;
            StartCoroutine(DestroyObj(projectile, 0.5f));               
            //sword attack
            if (Physics.Raycast(cam.position, cam.forward, out hitInfo, 7f, layerMask, QueryTriggerInteraction.Collide))
            {
                GameObject target = hitInfo.collider.gameObject;
                if (hitInfo.collider.tag == "hurtbox")
                {
                    EnemyData enemy = target.GetComponent<EnemyData>();
                    //register hit 
                    enemy.TakeDamage(SwordSettings.Sword_damage);
                }
                //play impact anim
                Instantiate(SwordImpactVFX, hitInfo.point, Quaternion.identity);
            }
        }
             
    }

    void Scattergun_Shot(bool chargedShot)
    {
        //charged shot, aoe thorns
        if (chargedShot)
        {
            //spawn projectile
            Quaternion rotation = Quaternion.identity;
            rotation.eulerAngles = cam.rotation.eulerAngles + new Vector3(0, 0, 0);
            GameObject projectile = Instantiate(Thorn_Projectile, new Vector3(cam.position.x, cam.position.y - 0.1f, cam.position.z), rotation);
            projectile.GetComponent<Rigidbody>().velocity = cam.forward * ScattergunSettings.ThornProjectileSpeed;
            StartCoroutine(DestroyObj(projectile, 15f));
            //projectile spawns thorns
        }
        //basic attack
        else
        {
            List<GameObject> targetsHit = new List<GameObject>();
            List<Vector3> tracer_spreads = new List<Vector3>
        {
        (-cam.right + cam.up) * .5f, cam.up * .7f, (cam.right + cam.up) * .5f,
        -cam.right, new Vector3(0,0,0), cam.right,
        (-cam.right - cam.up) * .5f, -cam.up * .7f, (cam.right - cam.up) * .5f,
        };
            //draw tracers
            for (int x = 0; x < 9; x++)
            {
                GameObject t = Instantiate(SG_tracer, currentBulletOrigin.position, Quaternion.identity);
                //register hit
                if (Physics.Raycast(cam.position, (cam.forward + tracer_spreads[x] / 10), out hitInfo, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide))
                {
                    GameObject target = hitInfo.collider.gameObject;
                    //hit enemy
                    if (hitInfo.collider.tag == "hurtbox")
                    {
                        if (!targetsHit.Contains(target))
                        {
                            targetsHit.Add(target);
                        }
                        EnemyData enemy = target.GetComponent<EnemyData>();
                        enemy.TakeDamage(ScattergunSettings.SG_damage);
                        if (enemy.health > 0 && (target.transform.position - cam.position).magnitude < 8)
                        {
                            ApplyKnockback(target, 10, 0, cam.forward);
                        }
                        t.GetComponent<LineRenderer>().SetPosition(0, currentBulletOrigin.position - t.transform.position);
                        t.GetComponent<LineRenderer>().SetPosition(1, ((cam.forward + tracer_spreads[x] / 10) * 5) + cam.position - t.transform.position);
                        t.GetComponent<Rigidbody>().velocity = (cam.forward + tracer_spreads[x] / 10) * 50;
                    }
                    //hit wall
                    else
                    {
                        Instantiate(SGImpactVFX, hitInfo.point, Quaternion.identity);
                        t.GetComponent<LineRenderer>().SetPosition(0, currentBulletOrigin.position - t.transform.position);
                        t.GetComponent<LineRenderer>().SetPosition(1, ((cam.forward + tracer_spreads[x] / 10) * 5) + cam.position - t.transform.position);
                        t.GetComponent<Rigidbody>().velocity = (cam.forward + tracer_spreads[x] / 10) * 50;
                    }
                    if (hitInfo.collider.tag == "Ball")
                    {
                        PushObject(hitInfo.collider.gameObject, hitInfo.point);
                    }

                }
                //nothing
                else
                {
                    t.GetComponent<LineRenderer>().SetPosition(0, currentBulletOrigin.position - t.transform.position);
                    t.GetComponent<LineRenderer>().SetPosition(1, ((cam.forward + tracer_spreads[x] / 10) * 5) + cam.position - t.transform.position);
                    t.GetComponent<Rigidbody>().velocity = (cam.forward + tracer_spreads[x] / 10) * 50;
                }
                StartCoroutine(DestroyObj(t, 0.25f));
            }
            //play impact anim  
            foreach (GameObject x in targetsHit)
            {
                Instantiate(SGImpactVFX, x.transform.position, Quaternion.identity).transform.localScale *= 2;
            }
        }
        
        StartCoroutine(FireRate(ScattergunSettings.SG_fireRate));
    }

    void RocketLaunch()
    {
        Quaternion rotation = Quaternion.identity;
        rotation.eulerAngles = cam.rotation.eulerAngles + new Vector3(90, 0, 0);
        GameObject rocket = Instantiate(Rocket, new Vector3(cam.position.x, cam.position.y - 0.1f, cam.position.z), rotation);


        rocket.GetComponent<Rigidbody>().velocity = cam.forward * rocketSpeed;
        StartCoroutine(DestroyObj(rocket, 15));
        StartCoroutine(FireRate(RL_fireRate));
    }

    void RailShot()
    {
        //draw laser  
        GameObject laser = Instantiate(RailLaser, currentBulletOrigin.position, Quaternion.identity);
        //register hit 
        if (Physics.Raycast(cam.position, cam.forward, out hitInfo, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide))
        {
            GameObject target = hitInfo.collider.gameObject;

            laser.GetComponent<LineRenderer>().SetPosition(0, currentBulletOrigin.position);
            laser.GetComponent<LineRenderer>().SetPosition(1, hitInfo.point);
            if (hitInfo.collider.tag == "hurtbox")
            {
                EnemyData enemy = target.GetComponent<EnemyData>();

                enemy.TakeDamage(RailgunSettings.RG_damage);
            }
            if (hitInfo.collider.tag == "Ball")
            {
                PushObject(hitInfo.collider.gameObject, hitInfo.point);
            }
            //play impact anim
            Instantiate(RailImpactVFX, hitInfo.point, Quaternion.identity);
        }
        else
        {
            laser.GetComponent<LineRenderer>().SetPosition(0, currentBulletOrigin.position);
            laser.GetComponent<LineRenderer>().SetPosition(1, (cam.forward * 100) + cam.position);
        }
        StartCoroutine(DestroyObj(laser, 0.5f));
        StartCoroutine(FireRate(RG_fireRate));
    }

    void ItemThrow()
    {
        //throw current selected item        
    }

    void PushObject(GameObject pushed, Vector3 hitSpot)
    {
        Vector3 force = ((pushed.transform.position - hitSpot).normalized * railForce) + (Vector3.up * pushUpScale);
        Vector3 currentVel = pushed.GetComponent<BallLogic>().parent.GetComponent<Rigidbody>().velocity;
        Vector3 newVel = new Vector3(0, 0, 0);

        if (currentVel.x * force.x > 0)
        {
            newVel.x = currentVel.x + force.x;
        }
        else
        {
            newVel.x = force.x;
        }

        if (currentVel.z * force.z > 0)
        {
            newVel.z = currentVel.z + force.z;
        }
        else
        {
            newVel.z = force.z;
        }

        if (currentVel.y * force.y > 0)
        {
            newVel.y = currentVel.y + force.y;
        }
        else
        {
            newVel.y = force.y;
        }

        pushed.GetComponent<BallLogic>().parent.GetComponent<Rigidbody>().velocity = newVel;
    }

    void ApplyMovement(Vector3 force)
    {
        Vector3 currentVel = rb.velocity;
        Vector3 newVel = new Vector3(0,0,0);

        //if the currentVel and the force of the source are going the same direction, add them together, else, just set it equal to the force; check for each axis
        if (Vector3.Angle(force, currentVel) < 90)
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

        rb.velocity = newVel;
    }

    void ApplyKnockback(GameObject other, float knockback, float upwardForce, Vector3 forceDir)
    {      
        Vector3 force = new Vector3();
        Vector3 currentVel = new Vector3();
        //apply force based on if agent is active or not
        if (other.GetComponent<NavMeshAgent>().enabled)
        {
            currentVel = other.GetComponent<NavMeshAgent>().velocity;
            force += new Vector3(forceDir.normalized.x * knockback, upwardForce, forceDir.normalized.z * knockback);
        }
        else
        {
            currentVel = other.GetComponent<Rigidbody>().velocity;
            force += (forceDir * knockback) / 4;
        }
        Vector3 newVel = new Vector3();
        //if the currentVel and the force of the source are going the same direction, add them together, else, just set it equal to the force; check for each axis
        if (Vector3.Angle(force, currentVel) < 90)
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

        if (other.GetComponent<NavMeshAgent>().enabled)
        {
            other.GetComponent<Rigidbody>().velocity *= 0;
            other.GetComponent<NavMeshAgent>().velocity = newVel;
        }
        else
        {
            other.GetComponent<NavMeshAgent>().velocity *= 0;
            other.GetComponent<Rigidbody>().velocity = newVel;
        }
        other.GetComponent<NavMeshAgent>().acceleration = 0;
        StartCoroutine(WaitForKnockBack(other));
    }

    void SetAnimBlend()
    {
        if (Player.GetComponent<Movement>().grounded)
        {
            viewModelAnim.SetFloat("Ground_blend", 1);
            viewModelAnim.SetFloat("Air_blend", 0.5f);
        }
        else
        {
            viewModelAnim.SetFloat("Ground_blend", 0);
            Vector3 Move = Player.GetComponent<Movement>().MoveVector;
            float diff = 0.5f;
            if (Move.magnitude != 0)
            {
                diff = Vector3.Angle(-cam.right, Move) / 180;
            }
            viewModelAnim.SetFloat("Air_blend", diff);
        }
        viewModelAnim.Play("Air_BlendTree", 0);
    }

    IEnumerator WaitForAnim(Animator currentAnim)
    {       
        isFire = true;       
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(currentAnim.GetCurrentAnimatorStateInfo(0).length);
        isFire = false;          
    }

    IEnumerator WaitForVFX(VisualEffect vfx, float t)
    {
        yield return new WaitForSeconds(t);
        vfx.enabled = false;
    }

    IEnumerator FireRate(float t)
    {   
        canFire = false;
        yield return new WaitForSeconds(t);
        canFire = true;
    }

    IEnumerator DestroyObj(GameObject obj, float t)
    {
        yield return new WaitForSeconds(t);
        Destroy(obj);
    }

    IEnumerator WaitForKnockBack(GameObject other)
    {      
        yield return new WaitForSeconds(.5f);
        if (other != null)
        {
            other.GetComponent<NavMeshAgent>().acceleration = other.GetComponent<EnemyPathfinding>().trueAccel;
        }
    }

    IEnumerator SwordSlashDelay(bool weapon_charged)
    {
        if (weapon_charged)
        {
            SwordSlash(Weapon_charged);
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
            SwordSlash(Weapon_charged);
        }
    }

}
