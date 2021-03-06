﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VinPower : MonoBehaviour
{
    #region particles
    [SerializeField] ParticleSystem [] shieldWaves;
    [SerializeField] ParticleSystem powerWave;
    [SerializeField] ParticleSystem healWave;
    ParticleSystem controlledAlienLaser;
    #endregion

    [Header("Sounds")]
    [SerializeField] AudioSource controlSound;
    [SerializeField] AudioSource moveSound;
    [SerializeField] AudioSource shieldSound;
    AudioSource actualSound;

    [Header("Variables de poder")]
    [SerializeField] GameObject target;
    [SerializeField] float forceImp = 2.0f;
    [SerializeField] float powerRange = 3.0f;
    bool move = false;
    bool control = false;
    bool shield = false;
    bool cast = false;
    bool heal = false;
    int layerMask;
    Rigidbody rb;
    VinMov mov;
    [SerializeField]SphereCollider shieldCollider;
    VinCamera1 mainCam;

    [SerializeField] float maxSpeed = 100.0f;
    [SerializeField] float aceleration = 20.0f;
    Vector2 direction = new Vector2(0f, 0f);
    Vector2 input = new Vector2(0f, 0f);
    bool shoot;

    VinLife VL;

    void Start()
    {
        VL = GetComponent<VinLife>();
        //sounds = GetComponents<AudioSource>();
        layerMask = LayerMask.GetMask("controllable");
        mov = GetComponent<VinMov>();
        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<VinCamera1>();
    }

    private void LateUpdate()
    {
        RaycastHit hitInfo = new RaycastHit();
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, Mathf.Infinity, layerMask)){
            Outline ol = hitInfo.transform.gameObject.GetComponent<Outline>();
            if (ol)
            {
                ol.enabled = true;
            }
        } else if (target)
        {
            mov.noiseSphere.radius = mov.powerUseNoise;
            if (target.tag != "Vin")
            {
                Outline ol = target.GetComponent<Outline>();
                if (ol)
                {
                    ol.enabled = true;
                }
            }
            if (heal) mov.noiseSphere.radius = mov.healNoise;
        }
    }

    void Update()
    {
        shield =    Input.GetKey("e") && !move && !control && !heal;
        heal =      Input.GetKey("f") && !move && !control && !shield;
        cast =      Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetKeyDown("e") || Input.GetKeyDown("f");
        move =      Input.GetMouseButton(0) && !shield && !control && !heal;
        control =   Input.GetMouseButton(1) && !move && !shield && !heal;
        input =     new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        shoot =     Input.GetButtonDown("Fire1");

        if (mov.freezeInput)
        {
            shield = false;
            heal = false;
            cast = false;
            move = false;
            control = false;
            input = new Vector2(0f, 0f);
            shoot = false;
        }

        if (cast) castObject();
        if (target)
        {
            float distanceTarget = (target.transform.position - transform.position).magnitude;
            if ((!shield && !move && !control && !heal) ||  distanceTarget > powerRange || mov.freezeInput)
            {
                VL.stopHealing();
                powerWave.Stop();
                healWave.Stop();
                if (!(target.tag == "Vin"))
                {
                    target.GetComponent<theScriptThatMakesYouExplodeWhenUrTooFast>().powerWave.Stop();
                }
                foreach (ParticleSystem shieldWave in shieldWaves)
                {
                    shieldWave.Stop();
                }
                mov.usingPower = false;
                target = null;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                mainCam.setDefaultTarget();

                //SOUND
                if(actualSound != null)
                {
                    actualSound.Stop();
                    actualSound = null;
                }
            }
            else
            {
                mov.usingPower = true;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Confined;
            }
            shieldCollider.enabled = false;
            if (shield)
            {
                mov.usingPower = true;
                shieldCollider.enabled = true;
            } else if (control && shoot)
            {
                mov.usingPower = true;
                controlledAlienLaser.Play();
            } else if (heal)
            {
                mov.usingPower = true;

            }
        }
    }

    void castObject()
    {
        if (!shield && !heal)
        {
            RaycastHit hitInfo = new RaycastHit();
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, Mathf.Infinity, layerMask))
            {
                target = hitInfo.transform.gameObject;
                EventTriggererScript targetEvent = target.GetComponent<EventTriggererScript>();
                rb = target.GetComponent<Rigidbody>();
                if ((target.transform.position - transform.position).magnitude < powerRange)
                    mov.usePower();
                if(control && target.tag == "Alien")
                {

                    if (actualSound != null) actualSound.Stop();
                    actualSound = controlSound;
                    actualSound.Play();

                    controlledAlienLaser = target.GetComponent<theScriptThatMakesYouExplodeWhenUrTooFast>().alienLaser;
                    mainCam.changeTarget(target.transform);
                    powerWave.Play();
                    target.GetComponent<theScriptThatMakesYouExplodeWhenUrTooFast>().powerWave.Play();
                    
                    if(targetEvent)
                        targetEvent.OnPowerControlEnter();
                }
                else if(move)
                {
                    if(actualSound != null) actualSound.Stop();
                    actualSound = moveSound;
                    actualSound.Play();
                    powerWave.Play();
                    if (targetEvent)
                        targetEvent.OnPowerMoveEnter();
                }
            }
        }
        else if (shield)
        {

            if (actualSound != null) actualSound.Stop();
            actualSound = shieldSound;
            actualSound.Play();

            foreach (ParticleSystem shieldWave in shieldWaves)
            {
                shieldWave.Play();
            }
            powerWave.Play();            
            target = gameObject;
            mov.usePower();

        } else if (heal)
        {
            if (actualSound != null) actualSound.Stop();
            actualSound = moveSound;
            actualSound.Play();

            powerWave.Play();
            healWave.Play();
            target = gameObject;
            mov.usePower();
            VL.startHealing();
        } 
    }

    private void FixedUpdate()
    {
        if (target)
        {
            if (move)
            {
                moveObject();
            }
            else if (control && target.tag == "Alien")
            {
                controlObject();
            }
        }
    }

    private void moveObject()
    {
        float multiplier = powerRange - Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up).magnitude;
        Vector3 force = new Vector3(Input.GetAxis("Mouse X") * forceImp * multiplier,
            0f,
            Input.GetAxis("Mouse Y")*forceImp * multiplier) * Time.deltaTime;
        rb.velocity = new Vector3(force.x,
            transform.position.y - target.transform.position.y,
            force.z);
    }
    private void controlObject()
    {
        //Operations
        if (maxSpeed > 0.0f)
        {
            direction += (input.normalized - direction / maxSpeed) * aceleration;
            rb.velocity = new Vector3(direction.x * Time.deltaTime, rb.velocity.y, direction.y * Time.deltaTime);
            Vector3 look = Vector3.ProjectOnPlane(rb.velocity, Vector3.up);
            if (look.magnitude > 0.01)
                target.transform.LookAt(target.transform.position + look);
        }
    }

    public bool isShielding()
    {
        return shield;
    }
}
