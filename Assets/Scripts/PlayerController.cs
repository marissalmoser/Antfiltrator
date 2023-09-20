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
    public Rigidbody2D Rb;
    public GameManager GameManager;
    private GameManager gm;
    public GameObject WalkGraphics;
    public GameObject CrawlGraphics;

    //actions
    private InputAction move, jump, head, leg, crawl, changeMov, interact;

    //moving variables
    private bool playerCanMove;
    private bool playerCanCrawl;
    private bool crawlMapEnabled;
    [SerializeField] private float speed;
    private float direction;
    private float rotationSpeed = 3;
    private Vector2 crawlDirection;
    private Vector2 crawlRotation;
    private bool isFacingRight = true;

    //jumping variables
    [SerializeField] private float jumpingPower = 16f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    private bool jumpStart;

    //interacting
    public bool Interact;

    void Start()
    {
        gm = GameManager.GetComponent<GameManager>();

        MyPlayerInput.actions.FindActionMap("PlayerTwoDirectionMovement").Enable();
        MyPlayerInput.actions.FindActionMap("PartSwitching").Enable();

        move = MyPlayerInput.actions.FindActionMap("PlayerTwoDirectionMovement").FindAction("Move");
        jump = MyPlayerInput.actions.FindActionMap("PlayerTwoDirectionMovement").FindAction("Jump");
        crawl = MyPlayerInput.actions.FindActionMap("PlayerCrawlingMovement").FindAction("Crawl");
        head = MyPlayerInput.actions.FindActionMap("PartSwitching").FindAction("Head");
        leg = MyPlayerInput.actions.FindActionMap("PartSwitching").FindAction("Leg");
        changeMov = MyPlayerInput.actions.FindActionMap("PartSwitching").FindAction("SwitchMovementSystem");
        interact = MyPlayerInput.actions.FindActionMap("PartSwitching").FindAction("Interact");

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
    }

    private void Handle_interactCanceled(InputAction.CallbackContext obj)
    {
        Interact = false;
    }
    private void Handle_interactStarted(InputAction.CallbackContext obj)
    {
        Interact = true;
    }

    private void Handle_moveStarted(InputAction.CallbackContext obj)
    {
        playerCanMove = true;
    }
    private void Handle_moveCanceled(InputAction.CallbackContext obj)
    {
        playerCanMove = false;
    }
    private void Handle_jumpStarted(InputAction.CallbackContext obj)
    {
        if (jumpStart == false)
        {
            jumpStart = true;
        }
    }
    private void Handle_jumpCanceled(InputAction.CallbackContext obj)
    {
        jumpStart = false;
    }

    private void Handle_crawlStarted(InputAction.CallbackContext obj)
    {
        playerCanCrawl = true;
    }
    private void Handle_crawlCanceled(InputAction.CallbackContext obj)
    {
        playerCanCrawl = false;
        Rb.velocity = Vector2.zero;
        crawlDirection = Vector2.zero;
    }

    private void SwitchMovementSystem(InputAction.CallbackContext obj)
    {
        //switch to 2D movement system
        if(crawlMapEnabled)
        {
            print("switch to 2D movement system");
            crawlMapEnabled = false;
            Rb.gravityScale = 4;
            MyPlayerInput.actions.FindActionMap("PlayerTwoDirectionMovement").Enable();
            MyPlayerInput.actions.FindActionMap("PlayerCrawlingMovement").Disable();
            CrawlGraphics.SetActive(false);
            WalkGraphics.SetActive(true);
        }
        //switch to crawling movement system
        else
        {
            print("switch to crawling movement system");
            crawlMapEnabled = true;
            Rb.gravityScale = 0;
            MyPlayerInput.actions.FindActionMap("PlayerTwoDirectionMovement").Disable();
            MyPlayerInput.actions.FindActionMap("PlayerCrawlingMovement").Enable();
            CrawlGraphics.SetActive(true);
            WalkGraphics.SetActive(false);
        }
    }

    private void SwitchHeadPart(InputAction.CallbackContext obj)
    {
        print("head");
        gm.BaseHead = !gm.BaseHead;
    }
    private void SwitchLegPart(InputAction.CallbackContext obj)
    {
        print("leg");
        gm.BaseLeg = !gm.BaseLeg;
    }

    private void Update()
    {
        Flip();
        if (playerCanMove == true)
        {
            direction = move.ReadValue<float>();
        }

        if(playerCanCrawl == true)
        {
            crawlDirection = crawl.ReadValue<Vector2>();
        }
        //rotation of the player during movement. Only if in crawl map
        if (crawlDirection != Vector2.zero && crawlMapEnabled)
        {
            Quaternion targetRotation = Quaternion.LookRotation(transform.forward, crawlRotation);
            Quaternion rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            Rb.MoveRotation(rotation);
            //print(rotation);
            //print(crawlRotation);
        }
    }

    private void FixedUpdate()
    {
        //player 2D movement
        if(playerCanMove == true)
        {
            Rb.velocity = new Vector2(speed * direction, Rb.velocity.y);
        }
        else
        {
            Rb.velocity = new Vector2(0, Rb.velocity.y);
        }

        //player jump
        if (IsGrounded() && jumpStart && !crawlMapEnabled)
        {
            //print("jump");
            Rb.velocity = new Vector2(Rb.velocity.x, jumpingPower);
        }
        
        //player crawl
        if(playerCanCrawl == true && crawlMapEnabled)
        {
            Rb.velocity = new Vector2(speed * crawlDirection.x, speed * crawlDirection.y);
        }

    }

    private void Flip()
    {
        if (!isFacingRight && direction < 0f || isFacingRight && direction > 0 && !crawlMapEnabled)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }
}
