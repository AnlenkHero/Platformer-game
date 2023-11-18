using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("General properties")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    private Vector2 _vecGravity;
    private bool isFacingRight = true;
    
    [Header("Movement properties")]
    [SerializeField] private float maxHorizontalSpeed;
    [SerializeField] private float accelerationTime;
    [SerializeField] private float brakingTime;
    private float _currentHorizontalSpeed;
    private float horizontal;

    
    [Header("Jump properties")]
    [SerializeField] private float jumpForce;
    [SerializeField] private int maxAdditionalJumps = 2;
    [SerializeField] private float fallMultiplier;
    [SerializeField] private float jumpMultiplier;
    [SerializeField] private float jumpTime;
    private bool _isJumping;
    private float _jumpCounter;
    private bool _isJump;
    private int _remainingJumps;
    
    [Header("Wall Jump properties")]
    [SerializeField] private float wallJumpingTime = 0.2f;
    [SerializeField] private float wallJumpingDuration = 0.3f;
    [SerializeField] private Vector2 wallJumpingPower = new Vector2(8f, 16f);
    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingCounter;
    
    [Header("Wall sliding properties")]
    private bool isWallSliding;
    [SerializeField] private float wallSlidingSpeed = 2f;


    void Start()
    {
        _vecGravity = new Vector2(0, -Physics2D.gravity.y);
    }

    private void Update()
    {
        horizontal = Input.GetAxisRaw("Horizontal");

        if (IsGrounded() && !Input.GetKeyDown(KeyCode.Space))
        {
            _remainingJumps = maxAdditionalJumps;
        }

        if (Input.GetKeyDown(KeyCode.Space) && (_remainingJumps > 0 || IsGrounded()) && !isWallSliding &&
            !isWallJumping && wallJumpingCounter < 0f)
        {
            _isJump = true;
            _isJumping = true;
            _jumpCounter = 0;
            _remainingJumps--;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            _isJumping = false;
            _jumpCounter = 0;
        }

        if (Input.GetKeyDown(KeyCode.Space) && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }

        WallSlide();
        WallJump();

        if (!isWallJumping)
        {
            Flip();
        }
    }

    private void FixedUpdate()
    {
        if (!isWallJumping)
        {
            if (IsGrounded())
            {
                if (horizontal != 0)
                {
                    float speedIncrement = maxHorizontalSpeed / accelerationTime * Time.fixedDeltaTime;
                    float targetSpeed = horizontal * maxHorizontalSpeed;
                    _currentHorizontalSpeed = Mathf.MoveTowards(_currentHorizontalSpeed, targetSpeed, speedIncrement);
                }
                else
                {
                    _currentHorizontalSpeed = Mathf.MoveTowards(_currentHorizontalSpeed, 0, brakingTime);
                }
            }
            else
            {
                _currentHorizontalSpeed = maxHorizontalSpeed * horizontal;
            }

            rb.velocity = new Vector2(_currentHorizontalSpeed, rb.velocity.y);
        }


        if (_isJump)
        {
            Jump();
        }

        if (rb.velocity.y > 0 && _isJumping)
        {
            _jumpCounter += Time.deltaTime;
            if (_jumpCounter > jumpTime) _isJumping = false;

            float jumpFraction = _jumpCounter / jumpTime;
            float currentJumpMultiplier = jumpMultiplier;

            if (jumpFraction > 0.5f)
            {
                currentJumpMultiplier = jumpMultiplier * (1 - jumpFraction);
            }

            rb.velocity += _vecGravity * (currentJumpMultiplier * Time.deltaTime);
        }

        if (!isWallJumping)
        {
            if (rb.velocity.y > 0 && !_isJumping)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.6f);
            }

            if (rb.velocity.y < 0)
            {
                rb.velocity -= _vecGravity * (fallMultiplier * Time.deltaTime);
            }
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private bool IsWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
    }

    private void WallSlide()
    {
        if (IsWalled() && !IsGrounded() && horizontal != 0f)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void Jump()
    {
        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        _isJump = false;
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;

            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) && !IsGrounded() && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            if (transform.localScale.x != wallJumpingDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private void Flip()
    {
        if (isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }
}