/**********************************************************************************

// File Name :         PlayerController.cs
// Author :            Marissa Moser
// Creation Date :     September 13, 2023
//
// Brief Description : 

**********************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    //components and GOs
    public PlayerInput MyPlayerInput;
    private Rigidbody2D rb;
    private PlayerBehavior pb;
    public GameObject WalkGraphics;
    public GameObject CrawlGraphics;
    private Animator walkingAnim;
    private Animator crawlingAnim;

    //actions
    private InputAction move, jump, head, leg, crawl, changeMov, interact, spawnWeb, pause, nextLevel;
    public static Action BeeVision, WebShooterUI, ErrorMessage, PlatformCountUI, GamePaused;

    //moving variables
    [Header("Player Movement")]
    [SerializeField] private bool playerCanMove;        //starts and stops player movement regardless of system
    public static bool PlayerCanCrawl;                        //on start and cancel
    public bool CrawlMapEnabled;
    [SerializeField] private float speed;
    private float direction;
    private Vector2 crawlDirection;
    private bool isFacingRight = true;
    [SerializeField] private LayerMask climbableWalls;
    private float angle;
    private int canMove = 1;

    //jumping variables
    [Header("Jumping")]
    [SerializeField] private float jumpingPower = 16f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    private bool jumpStart;

    //interacting
    [HideInInspector] public bool Interact;
    private Vector3 mousePosition;
    private Vector2 mouseWorldPosition;
    [SerializeField] private GameObject webCrosshair;
    [SerializeField] private GameObject errorSparks;
    private GameObject sparks;

    [Header("Bug Parts")]
    [SerializeField] private GameObject beeMaskWalk;
    [SerializeField] private GameObject beeMaskCrawl;
    [SerializeField] private GameObject webShooterWalk;

    [Header("Camera")]
    [SerializeField] private Camera cam;
    private Vector3 camOffset = new Vector3(0, 1, -10);
    private Vector3 camPos;

    void Awake()
    {
        //gm = GameManager.Instance;

        pb = gameObject.GetComponent<PlayerBehavior>();
        rb = gameObject.GetComponent<Rigidbody2D>();
        walkingAnim = WalkGraphics.GetComponent<Animator>();
        crawlingAnim = CrawlGraphics.GetComponent<Animator>();

        MyPlayerInput.actions.FindActionMap("PlayerTwoDirectionMovement").Enable();
        MyPlayerInput.actions.FindActionMap("PartSwitching").Enable();

        move = MyPlayerInput.actions.FindActionMap("PlayerTwoDirectionMovement").FindAction("Move");
        jump = MyPlayerInput.actions.FindActionMap("PlayerTwoDirectionMovement").FindAction("Jump");
        crawl = MyPlayerInput.actions.FindActionMap("PlayerCrawlingMovement").FindAction("Crawl");
        head = MyPlayerInput.actions.FindActionMap("PartSwitching").FindAction("Head");
        leg = MyPlayerInput.actions.FindActionMap("PartSwitching").FindAction("Leg");
        changeMov = MyPlayerInput.actions.FindActionMap("PartSwitching").FindAction("SwitchMovementSystem");
        interact = MyPlayerInput.actions.FindActionMap("PartSwitching").FindAction("Interact");
        spawnWeb = MyPlayerInput.actions.FindActionMap("PartSwitching").FindAction("SpawnWebPlatform");
        pause = MyPlayerInput.actions.FindActionMap("PartSwitching").FindAction("Pause");
        nextLevel = MyPlayerInput.actions.FindActionMap("PartSwitching").FindAction("NextLevelKB");

        move.started += Handle_moveStarted;
        move.canceled += Handle_moveCanceled;
        jump.started += Handle_jumpStarted;
        jump.canceled += Handle_jumpCanceled;
        head.started += SwitchHeadPart;
        leg.started += SwitchLegPart;
        crawl.started += Handle_crawlStarted;
        crawl.canceled += Handle_crawlCanceled;
        changeMov.started += SwitchMovementSystem;
        interact.started += Handle_interactStarted;
        interact.canceled += Handle_interactCanceled;
        spawnWeb.started += SpawnWebStarted;
        pause.started += PauseGame;
        nextLevel.started += SkipToNextLevel;

        WallBehavior.WallTriggered += SwitchToWalk;
    }

    private void SkipToNextLevel(InputAction.CallbackContext obj)
    {
        StartCoroutine(GameManager.Instance.NextLevel());
    }
    private void PauseGame(InputAction.CallbackContext obj)
    {
        GamePaused?.Invoke();
    }

    private void Handle_interactStarted(InputAction.CallbackContext obj)
    {
        Interact = true;
        if(!GameManager.GameIsPaused)
        {
            pb.BreakObject();
            pb.PickUpObject();
        }
    }
    private void Handle_interactCanceled(InputAction.CallbackContext obj)
    {
        Interact = false;
    }

    private void Handle_moveStarted(InputAction.CallbackContext obj)
    {
        if (!GameManager.GameIsPaused)
        {
            playerCanMove = true;

            if (canMove == 0)
            {
                BeeVisionError();
            }
            else
            {
                walkingAnim.SetBool("isWalking", true);
            }
        }
    }
    private void Handle_moveCanceled(InputAction.CallbackContext obj)
    {
        playerCanMove = false;

        walkingAnim.SetBool("isWalking", false);
    }
    private void Handle_jumpStarted(InputAction.CallbackContext obj)
    {
        if (jumpStart == false && !GameManager.GameIsPaused)
        {
            jumpStart = true;
        }
        if(canMove == 0)
        {
            BeeVisionError();
        }
    }
    private void Handle_jumpCanceled(InputAction.CallbackContext obj)
    {
        jumpStart = false;
    }

    private void Handle_crawlStarted(InputAction.CallbackContext obj)
    {
        if (!GameManager.GameIsPaused)
        {
            PlayerCanCrawl = true;

            if (canMove == 0)
            {
                BeeVisionError();
            }
            else
            {
                crawlingAnim.SetBool("isCrawling", true);
            }
        }
    }
    private void Handle_crawlCanceled(InputAction.CallbackContext obj)
    {
        PlayerCanCrawl = false;

        crawlingAnim.SetBool("isCrawling", false);

        rb.velocity = Vector2.zero;
        crawlDirection = Vector2.zero;
    }

    private void SwitchMovementSystem(InputAction.CallbackContext obj)
    {
        //switch to crawling movement system
        if (!CrawlMapEnabled && canMove == 1 && !GameManager.GameIsPaused && WallBehavior.OnClimbableWall) //this broke it??
        {
            if (!GameManager.Instance.BaseLeg)
            {
                //print("web off from crawl");
                webShooterWalk.SetActive(false);
                GameManager.Instance.BaseLeg = true;
                WebShooterUI?.Invoke();
                AudioManager.Instance.Play("WebShooterOff");
            }

            SwitchToCrawl();
            AudioManager.Instance.Play("WalkToCrawl");
        }

        //switch to 2D movement system
        else if (CrawlMapEnabled && canMove == 1 && !GameManager.GameIsPaused)
        {
            SwitchToWalk();
            AudioManager.Instance.Play("CrawlToWalkSwitch");
        }

        else if (!GameManager.Instance.BaseHead && !GameManager.GameIsPaused)
        {
            //trying to move with beevision
            BeeVisionError();
        }
        else
        {
            //print("what?");
        }
    }
    public void SwitchToWalk()
    {
        //print("switch to 2D movement system");
        CrawlMapEnabled = false;
        rb.gravityScale = 4;
        transform.rotation = Quaternion.AngleAxis(0, Vector3.forward);
        MyPlayerInput.actions.FindActionMap("PlayerTwoDirectionMovement").Enable();
        MyPlayerInput.actions.FindActionMap("PlayerCrawlingMovement").Disable();
        CrawlGraphics.SetActive(false);
        WalkGraphics.SetActive(true);
        if(pb.pickedUpObject != null)
        {
            pb.pickedUpObject.transform.rotation = Quaternion.Euler(0,0,0);
        }
    }
    public void SwitchToCrawl()
    {
        //print("switch to crawling movement system");
        CrawlMapEnabled = true;
        rb.gravityScale = 0;
        rb.velocity = Vector2.zero;
        MyPlayerInput.actions.FindActionMap("PlayerTwoDirectionMovement").Disable();
        MyPlayerInput.actions.FindActionMap("PlayerCrawlingMovement").Enable();
        CrawlGraphics.SetActive(true);
        WalkGraphics.SetActive(false);
    }

    private void SwitchHeadPart(InputAction.CallbackContext obj)
    {
        GameManager.Instance.BaseHead = !GameManager.Instance.BaseHead;

        //Bee Vision turned on
        if(!GameManager.Instance.BaseHead)
        {
            //changes the sprite
            beeMaskCrawl.SetActive(true);
            beeMaskWalk.SetActive(true);

            //vision on
            BeeVision?.Invoke();
            
            //stop movement
            canMove = 0;

            //sound
            AudioManager.Instance.Play("BeeVisionOn");
            AudioManager.Instance.Play("BeeVisionElectricity");
        }

        //Bee vision turned off
        if (GameManager.Instance.BaseHead)
        {
            //changes the sprite
            beeMaskCrawl.SetActive(false);
            beeMaskWalk.SetActive(false);

            //vision off
            BeeVision?.Invoke();

            //resume movement
            canMove = 1;

            //sound
            AudioManager.Instance.Play("BeeVisionOff");
            AudioManager.Instance.Stop("BeeVisionElectricity");
        }

        
    }
    private void SwitchLegPart(InputAction.CallbackContext obj)
    {

        ///web on
        if (GameManager.Instance.BaseLeg && !PlayerCanCrawl)
        {
            ///with crawl
            if(CrawlMapEnabled)
            {
                SwitchToWalk();
            }
      
            //print("web on");
            webShooterWalk.SetActive(true);
            GameManager.Instance.BaseLeg = !GameManager.Instance.BaseLeg;
            WebShooterUI?.Invoke();
            AudioManager.Instance.Play("WebShooterOn");
            StartCoroutine(MoveWebCrossHair());
        }
        ///disables web shooter
        else
        {
            //print("web off");
            webShooterWalk.SetActive(false);
            GameManager.Instance.BaseLeg = !GameManager.Instance.BaseLeg;
            WebShooterUI?.Invoke();
            AudioManager.Instance.Play("WebShooterOff");
        }
    }

    private void SpawnWebStarted(InputAction.CallbackContext obj)
    {
        mouseWorldPosition = Vector2.zero;
        mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        WebPlatformBehavior.MousePosition = mouseWorldPosition;

        pb.SpawnWebPlatform();
    }
    IEnumerator MoveWebCrossHair()
    {
        webCrosshair.SetActive(true);
        while(!GameManager.Instance.BaseLeg)
        {
            mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            webCrosshair.transform.position = mouseWorldPosition;
            yield return null;
        }
        webCrosshair.SetActive(false);
        
    }

    private void Update()
    {
        Flip();

        if (playerCanMove == true)
        {
            direction = move.ReadValue<float>();
        }
        if(PlayerCanCrawl == true)
        {
            crawlDirection = crawl.ReadValue<Vector2>();
        }

        //rotation of the player during movement. Only if in crawl map and bee vision off
        if (crawlDirection != Vector2.zero && CrawlMapEnabled && canMove == 1)
        {
            angle = Mathf.Atan2(crawlDirection.y, crawlDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);

        }
    }

    private void FixedUpdate()
    {
        //player 2D movement
        if(playerCanMove == true)
        {
            rb.velocity = new Vector2(speed * direction * canMove, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        //player jump
        if (IsGrounded() && jumpStart && !CrawlMapEnabled && rb.velocity.y < 0.5f && canMove == 1)
        {
            //print("jump");
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
            AudioManager.Instance.Play("Jump");
        }
        
        //player crawl
        if(PlayerCanCrawl && CrawlMapEnabled) 
        {
            rb.velocity = new Vector2(crawlDirection.x, crawlDirection.y) * speed * canMove;
            //print(CanClimb());
        }

        if(sparks != null)
        {
            sparks.transform.position = transform.position;
        }

        //camera movement
        camPos = transform.position + camOffset;
        cam.transform.position = camPos;

    }

    private void Flip()
    {
        if (isFacingRight && direction < 0f || !isFacingRight && direction > 0 && !CrawlMapEnabled)
        {
            if(canMove == 1)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1;
                transform.localScale = localScale;
            }
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private void BeeVisionError()
    {
        ErrorMessage?.Invoke();
        AudioManager.Instance.Play("ErrorSpark");
        sparks = Instantiate(errorSparks, transform.position, transform.rotation);
    }

    public void OnDestroy()
    {
        move.started -= Handle_moveStarted;
        move.canceled -= Handle_moveCanceled;
        jump.started -= Handle_jumpStarted;
        jump.canceled -= Handle_jumpCanceled;
        head.started -= SwitchHeadPart;
        leg.started -= SwitchLegPart;
        crawl.started -= Handle_crawlStarted;
        crawl.canceled -= Handle_crawlCanceled;
        changeMov.started -= SwitchMovementSystem;
        interact.started -= Handle_interactStarted;
        interact.canceled -= Handle_interactCanceled;
        spawnWeb.started -= SpawnWebStarted;
        pause.started -= PauseGame;
        WallBehavior.WallTriggered -= SwitchToWalk;
        nextLevel.started -= SkipToNextLevel;
    }
}
