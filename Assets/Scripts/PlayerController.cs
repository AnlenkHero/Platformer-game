using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("General properties")] 
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask ladderLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform ladderCheck;
    private Vector2 _vecGravity;
    private bool _isFacingRight = true;
    private float _originalGravity;

    [Header("Movement properties")] 
    [SerializeField] private float maxHorizontalSpeed=10f;
    [SerializeField] private float accelerationTime=0.15f;
    [SerializeField] private float brakingTime=0.5f;
    private float _currentHorizontalSpeed;
    private float _horizontal;

    [Header("Dash properties")] 
    [SerializeField] private float dashingPower=24f;
    [SerializeField] private float dashingTime=0.2f;
    [SerializeField] private float dashingCooldown=3f;
    [SerializeField] private int maxAdditionalDashes=3;
    [SerializeField] private int diagonalDashCost=2;
    private float _recoverDashTime;
    private int _remainingDashes;
    private bool _canDash = true;
    private bool _isDashing;


    [Header("Jump properties")] 
    [SerializeField] private float jumpForce=18f;
    [SerializeField] private float fallMultiplier=5f;
    [SerializeField] private float jumpMultiplier=2.5f;
    [SerializeField] private float jumpTime=0.2f;
    [SerializeField] private int maxAdditionalJumps = 2;
    private bool _isJumping;
    private bool _isJump;
    private float _jumpCounter;
    private int _remainingJumps;

    [Header("Wall Jump properties")] 
    [SerializeField] private float wallJumpingTime = 0.2f;
    [SerializeField] private float wallJumpingDuration = 0.3f;
    [SerializeField] private Vector2 wallJumpingPower = new Vector2(8f, 16f);
    private bool _isWallJumping;
    private float _wallJumpingDirection;
    private float _wallJumpingCounter;

    [Header("Wall sliding properties")] 
    [SerializeField] private float wallSlidingSpeed = 2f;
    private bool _isWallSliding;
    
    
    [Header("Ladder climbing properties")] 
    [SerializeField] private float ladderClimbingUpSpeed=6f;
    [SerializeField] private float ladderClimbingDownSpeed=3f;
    private bool _isLadderClimbing;

    void Start()
    {
        _originalGravity = rb.gravityScale;
        _remainingDashes = maxAdditionalDashes;
        _vecGravity = new Vector2(0, -Physics2D.gravity.y);
    }

    private void Update()
    {


        if (_remainingDashes != maxAdditionalDashes)
        {
            _recoverDashTime += Time.deltaTime;
            if (_recoverDashTime >= dashingCooldown)
            {
                _remainingDashes++;
                _recoverDashTime = 0;
            }
        }

        _horizontal = Input.GetAxisRaw("Horizontal");

        if (IsGrounded() && !Input.GetKeyDown(KeyCode.Space))
        {
            _remainingJumps = maxAdditionalJumps;
        }

        if (Input.GetKeyDown(KeyCode.Space) && (_remainingJumps > 0 || IsGrounded()) && !_isWallSliding &&
            !_isLadderClimbing && !_isWallJumping && _wallJumpingCounter < 0f)
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

        if (Input.GetKeyDown(KeyCode.LeftShift) && _canDash && !_isLadderClimbing && !_isWallSliding && _remainingDashes > 0)
        {
            if (Input.GetKey(KeyCode.W) && _remainingDashes >= diagonalDashCost)
            {
                _remainingDashes -= diagonalDashCost;
                StartCoroutine(Dash(0.75f));
            }
            else if (!Input.GetKey(KeyCode.W))
            {
                _remainingDashes--;
                StartCoroutine(Dash(0));
            }
        }

        WallSlide();
        WallJump();

        LadderClimb();

        if (!_isWallJumping)
        {
            Flip();
        }
    }

    private void FixedUpdate()
    {
        if (_isDashing)
            return;

        if (!_isWallJumping)
        {
            if (IsGrounded())
            {
                if (_horizontal != 0)
                {
                    float speedIncrement = maxHorizontalSpeed / accelerationTime * Time.fixedDeltaTime;
                    float targetSpeed = _horizontal * maxHorizontalSpeed;
                    _currentHorizontalSpeed = Mathf.MoveTowards(_currentHorizontalSpeed, targetSpeed, speedIncrement);
                }
                else
                {
                    _currentHorizontalSpeed = Mathf.MoveTowards(_currentHorizontalSpeed, 0, brakingTime);
                }
            }
            else
            {
                _currentHorizontalSpeed = maxHorizontalSpeed * 0.75f * _horizontal;
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

        if (!_isWallJumping)
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

    private bool IsLadder()
    {
        return Physics2D.OverlapCircle(ladderCheck.position, 0.1f, ladderLayer);
    }

    private void WallSlide()
    {
        if (IsWalled() && !IsGrounded() && _horizontal != 0f)
        {
            _isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            _isWallSliding = false;
        }
    }

    private void LadderClimb()
    {
        if (IsLadder())
        {
            _isLadderClimbing = true;
            if (_horizontal != 0f)
            {
                rb.gravityScale = _originalGravity;
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, ladderClimbingUpSpeed, float.MaxValue));
            }
            else if(Input.GetKey(KeyCode.S))
            {
                rb.gravityScale = 0f;
                rb.velocity = new Vector2(rb.velocity.x,  -ladderClimbingDownSpeed);
            }
            else
            {
                rb.gravityScale = 0;
                rb.velocity = new Vector2(rb.velocity.x, 0f); 
            }
        }
        else
        {
            rb.gravityScale = _originalGravity;
            _isLadderClimbing = false;
        }
    }

    private IEnumerator Dash(float additionalForceForY)
    {
        _canDash = false;
        _isDashing = true;
        rb.gravityScale = 0;
        rb.velocity = new Vector2(transform.localScale.x * dashingPower, additionalForceForY * dashingPower);
        yield return new WaitForSeconds(dashingTime);
        rb.gravityScale = _originalGravity;
        _isDashing = false;
        _canDash = true;
    }

    private void Jump()
    {
        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        _isJump = false;
    }

    private void WallJump()
    {
        if (_isWallSliding)
        {
            _isWallJumping = false;
            _wallJumpingDirection = -transform.localScale.x;
            _wallJumpingCounter = wallJumpingTime;

            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            _wallJumpingCounter -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) && !IsGrounded() && _wallJumpingCounter > 0f)
        {
            _isWallJumping = true;
            rb.velocity = new Vector2(_wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            _wallJumpingCounter = 0f;

            if (transform.localScale.x != _wallJumpingDirection)
            {
                _isFacingRight = !_isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private void StopWallJumping()
    {
        _isWallJumping = false;
    }

    private void Flip()
    {
        if (_isFacingRight && _horizontal < 0f || !_isFacingRight && _horizontal > 0f)
        {
            _isFacingRight = !_isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }
}