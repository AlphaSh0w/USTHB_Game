using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public bool canMove { get; private set; } = true;
    public bool IsSprinting => canSprint && Input.GetKey(sprintKey);
    public bool ShouldJump => Input.GetKeyDown(jumpKey) && CharacterController.isGrounded;
    private bool ShouldCrouch => Input.GetKeyDown(crouchKey) && CharacterController.isGrounded && !duringCrouchAnimation;

    [Header("Functional options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;

    [Header("Controls")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.C;


    [Header("Move parameters")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float sprintSpeed = 6.0f;

    [Header("Look parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 180)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 180)] private float lowerLookLimit = 80.0f;

    [Header("Jumping parameters")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Crouch parameters")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standupHeight = 2.0f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0,0.5f,0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0,0,0);
    private bool isCrouching;
    private bool duringCrouchAnimation;

    private Camera playerCamera;
    private CharacterController CharacterController;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float rotationX = 0.0f;
    // Start is called before the first frame update
    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        CharacterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (canMove)
        {
            HandleMovementInput();
            HandleMouseLock();
            if(canJump)
            {
                HandleJump();
            }
            if(canCrouch)
            {
                HandleCrouch();
            }
            ApplyFinalMovements();
        }
    }

    private void HandleMovementInput()
    {
        float moveSpeed;
        if(isCrouching)
        {
            moveSpeed = crouchSpeed;
        }
        else
        {
            moveSpeed = IsSprinting ? sprintSpeed : walkSpeed;
        }
        currentInput = new Vector2(moveSpeed * Input.GetAxis("Vertical"),
                                   moveSpeed * Input.GetAxis("Horizontal"));
        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;
    }
    private void HandleJump()
    {
        if (ShouldJump)
            moveDirection.y = jumpForce;
    }

    private void HandleCrouch()
    {
        if(ShouldCrouch)
        {
            //Handles the crouch/stand animation concurrently to simulate a smooth animation.
            StartCoroutine(CrouchStand());
        }
    }
    private void HandleMouseLock()
    {
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
    }
    private void ApplyFinalMovements()
    {
        if(!CharacterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
        if (CharacterController.velocity.y < -1 && CharacterController.isGrounded)
            moveDirection.y = 0;
        CharacterController.Move(moveDirection * Time.deltaTime);
    }
    private IEnumerator CrouchStand()
    {
        //If an obstacle is above the crouching player, cannot stand up.
        if(isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1))
            yield break;

        duringCrouchAnimation = true;

        float timeElapsed = 0;
        //Cache the current and target values for lerping purposes.
        float targetHeight = isCrouching ? standupHeight : crouchHeight;
        float currentHeight = CharacterController.height;
        Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = CharacterController.center;

        //Lerp
        while(timeElapsed < timeToCrouch)
        {
            CharacterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
            CharacterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
            timeElapsed += Time.deltaTime;
            yield return null;

        }

        //Make sure to fix any floating point error by directly assigning the target values at the end of the lerp.
        CharacterController.height = targetHeight;
        CharacterController.center = targetCenter;

        isCrouching = !isCrouching;

        duringCrouchAnimation = false;
    }
}
