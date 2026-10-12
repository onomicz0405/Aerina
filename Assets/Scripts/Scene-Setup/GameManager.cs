using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public KeyCode right { get; set; }
    public KeyCode left { get; set; }
    public KeyCode forward { get; set; }
    public KeyCode backward { get; set; }
    public KeyCode jump { get; set; }
    public KeyCode fistsSwap { get; set; }
    public KeyCode swordSwap { get; set; }
    public KeyCode rocketSwap { get; set; }
    public KeyCode railgunSwap { get; set; }
    public KeyCode scattergunSwap { get; set; }
    public KeyCode cycleWeapon { get; set; }
    public KeyCode cycleWeaponBack { get; set; }
    public KeyCode fire { get; set; }
    public KeyCode altFire { get; set; }
    public KeyCode quit { get; set; }
    public KeyCode turnLeft { get; set; }
    public KeyCode turnRight { get; set; }
    public KeyCode dash { get; set; }
    public KeyCode kick { get; set; }
    public KeyCode crouch { get; set; }
    public KeyCode pause { get; set; }


    //all this just for scrollwheel input bindings
    [HideInInspector] public CustomInput rightKey = new CustomInput();
    [HideInInspector] public CustomInput leftKey = new CustomInput();
    [HideInInspector] public CustomInput forwardKey = new CustomInput();
    [HideInInspector] public CustomInput backwardKey = new CustomInput();
    [HideInInspector] public CustomInput jumpKey = new CustomInput();
    [HideInInspector] public CustomInput fistsSwapKey = new CustomInput();
    [HideInInspector] public CustomInput swordSwapKey = new CustomInput();
    [HideInInspector] public CustomInput rocketSwapKey = new CustomInput();
    [HideInInspector] public CustomInput railgunSwapKey = new CustomInput();
    [HideInInspector] public CustomInput scattergunSwapKey = new CustomInput();
    [HideInInspector] public CustomInput cycleWeaponKey = new CustomInput();
    [HideInInspector] public CustomInput cycleWeaponBackKey = new CustomInput();
    [HideInInspector] public CustomInput fireKey = new CustomInput();
    [HideInInspector] public CustomInput altFireKey = new CustomInput();
    [HideInInspector] public CustomInput quitKey = new CustomInput();
    [HideInInspector] public CustomInput turnLeftKey = new CustomInput();
    [HideInInspector] public CustomInput turnRightKey = new CustomInput();
    [HideInInspector] public CustomInput dashKey = new CustomInput();
    [HideInInspector] public CustomInput kickKey = new CustomInput();
    [HideInInspector] public CustomInput crouchKey = new CustomInput();
    [HideInInspector] public CustomInput pauseKey = new CustomInput();

    List<CustomInput> inputs = new List<CustomInput>();
    List<KeyCode> keysCodes = new List<KeyCode>();

    [HideInInspector] public int scrollWheelUpInputIndex = 10;
    [HideInInspector] public int scrollWheelDownInputIndex = 11;
    [HideInInspector] public bool scrollWheelToggle; //force this on for movement keys and force off for weapon cycle
    bool scrollWheelUp;
    bool scrollWheelDown;
    [HideInInspector] public bool pauseInputs;
    bool paused;

    GameObject Hud;
    GameObject Pause_Menu;

    private void Awake()
    {
        //set object references
        Hud = GameObject.Find("HUD");
        Pause_Menu = GameObject.Find("Pause_Menu");

        //set values
        Pause_Menu.SetActive(false);

        //i fucking hate unity
        rightKey = new CustomInput();
        leftKey = new CustomInput();
        forwardKey = new CustomInput();
        backwardKey = new CustomInput();
        jumpKey = new CustomInput();
        fistsSwapKey = new CustomInput();
        swordSwapKey = new CustomInput();
        rocketSwapKey = new CustomInput();
        railgunSwapKey = new CustomInput();
        scattergunSwapKey = new CustomInput();
        cycleWeaponKey = new CustomInput();
        cycleWeaponBackKey = new CustomInput();
        fireKey = new CustomInput();
        altFireKey = new CustomInput();
        quitKey = new CustomInput();
        turnLeftKey = new CustomInput();
        turnRightKey = new CustomInput();
        dashKey = new CustomInput();
        kickKey = new CustomInput();
        crouchKey = new CustomInput();
        pauseKey = new CustomInput();

        right = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("rightKey", "D"));
        left = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("leftKey", "A"));
        forward = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("forwardKey", "W"));
        backward = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("backwardKey", "S"));
        jump = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("jumpKey", "Space"));
        fistsSwap = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("weapon1Key", "Alpha1"));
        swordSwap = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("weapon2Key", "Q"));
        rocketSwap = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("weapon3Key", "Alpha3"));
        railgunSwap = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("weapon4Key", "Alpha4"));
        scattergunSwap = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("weapon5Key", "E"));
        cycleWeapon = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("cycleWeaponKey", "P"));
        cycleWeaponBack = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("cycleWeaponBack", "L"));       
        fire = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("fireKey", "Mouse0"));
        altFire = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("altFireKey", "Mouse1"));
        quit = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("quitKey", "P"));
        turnLeft = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("turnLeftKey", "Mouse4"));
        turnRight = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("turnRightKey", "Mouse3"));
        dash = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("dashKey", "CapsLock"));
        kick = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("kickKey", "F"));
        crouch = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("crouchKey", "LeftShift"));
        pause = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("pauseKey", "Escape"));

        inputs = new List<CustomInput>()
        {
            rightKey, leftKey, forwardKey, backwardKey, jumpKey, fistsSwapKey, swordSwapKey, rocketSwapKey, railgunSwapKey, scattergunSwapKey, cycleWeaponKey, 
            cycleWeaponBackKey, fireKey, altFireKey, quitKey, turnLeftKey, turnRightKey, dashKey, kickKey, crouchKey, pauseKey
        };

        keysCodes = new List<KeyCode>()
        {
            right, left, forward, backward, jump, fistsSwap, swordSwap, rocketSwap, railgunSwap, scattergunSwap, cycleWeapon,
            cycleWeaponBack, fire, altFire, quit, turnLeft, turnRight, dash, kick, crouch, pause
        };

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;              
    }

    private void Update()
    {
        Time.fixedDeltaTime = Time.deltaTime;

        if (!pauseInputs)
        {
            CheckInput();
        }

        if (quitKey.input)
        {
            Application.Quit();
        }
        if (Input.GetKeyDown(pause))
        {
            if (!paused)
            {
                PauseGame();
            }
            else
            {
                UnpauseGame();
            }
            
        }
    }

    void PauseGame()
    {
        paused = true;
        Time.timeScale = 0f;
        Pause_Menu.SetActive(true);
        Hud.SetActive(false);
        PauseInputs();
    }

    void UnpauseGame()
    {
        paused = false;
        Time.timeScale = 1f;
        Pause_Menu.SetActive(false);
        Hud.SetActive(true);
        UnPauseInputs();
    }

    void PauseInputs()
    {
        pauseInputs = true;
        //set all inputs to false
        for (int x = 0; x < inputs.Count; x++)
        {
            inputs[x].input = false;
            inputs[x].inputDown = false;
            inputs[x].inputUp = false;
        }
    }

    void UnPauseInputs()
    {
        pauseInputs = false;
    }

    void CheckInput()
    {
        //keycode input       
        for (int x = 0; x < inputs.Count; x++)
        {
            if (x != scrollWheelUpInputIndex && x != scrollWheelDownInputIndex)
            {
                inputs[x].inputDown = false;
                inputs[x].inputUp = false;
                if (inputs[x].toggleInput && Input.GetKeyDown(keysCodes[x]))
                {
                    inputs[x].input = !inputs[x].input;
                }
                else if (!inputs[x].toggleInput)
                {
                    inputs[x].input = Input.GetKey(keysCodes[x]);
                }
                
                if (inputs[x].lastInput != inputs[x].input)
                {
                    if (inputs[x].input)
                    {
                        inputs[x].inputDown = true;
                    }
                    else
                    {
                        inputs[x].inputUp = true;
                    }
                }
                inputs[x].lastInput = inputs[x].input;
            }          
        }

        //check for scroll wheel input
        ScrollWheelInput();
    }

    void ScrollWheelInput()
    {
        if (!scrollWheelToggle)
        {
            scrollWheelUp = false;
            scrollWheelDown = false;
        }

        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
        {
            if (scrollWheelToggle && scrollWheelUp)
            {
                scrollWheelUp = false;
            }
            else
            {
                scrollWheelUp = true;
            }
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
        {
            if (scrollWheelToggle && scrollWheelDown)
            {
                scrollWheelDown = false;
            }
            else
            {
                scrollWheelDown = true;
            }
        }
        CustomInput CI = inputs[scrollWheelUpInputIndex];
        CustomInput CI2 = inputs[scrollWheelDownInputIndex];

        CI.inputDown = false;
        CI.inputUp = false;
        CI.input = scrollWheelUp;
        if (CI.lastInput != CI.input)
        {
            if (CI.input)
            {
                CI.inputDown = true;
            }
            else
            {
                CI.inputUp = true;
            }
        }
        CI.lastInput = CI.input;

        CI2.inputDown = false;
        CI2.inputUp = false;
        CI2.input = scrollWheelDown;
        if (CI2.lastInput != CI2.input)
        {
            if (CI2.input)
            {
                CI2.inputDown = true;
            }
            else
            {
                CI2.inputUp = true;
            }
        }
        CI2.lastInput = CI2.input;
    }

    public class CustomInput
    {
        public bool input;
        public bool inputDown;
        public bool inputUp;
        public bool lastInput;

        public bool toggleInput; 
    }
}


