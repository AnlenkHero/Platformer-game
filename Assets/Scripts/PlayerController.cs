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
    private float _originalGravity;
    private bool _isFacingRight = true;

    [Header("Movement properties")] 
    [SerializeField] private float maxHorizontalSpeed = 10f;
    [SerializeField] private float accelerationTime = 0.15f;
    [SerializeField] private float brakingTime = 0.5f;
    private float _currentHorizontalSpeed;
    private float _horizontal;

    [Header("Dash properties")] 
    [SerializeField] private float dashingPower = 24f;
    [SerializeField] private float dashingTime = 0.2f;
    [SerializeField] private float dashingCooldown = 3f;
    [SerializeField] private float dashVerticalForce = 1.5f;
    [SerializeField] private int maxAdditionalDashes =3;
    [SerializeField] private int diagonalDashCost = 2;
    [SerializeField] private int defaultDashCost = 1;
    private float _recoverDashTime;
    private float _dashVerticalDirection;
    private int _remainingDashes;
    private bool _canDash = true;
    private bool _isDashing;
    private bool _shouldDash;


    [Header("Jump properties")] 
    [SerializeField] private float jumpForce = 18f;
    [SerializeField] private float fallMultiplier = 5f;
    [SerializeField] private float jumpMultiplier = 2.5f;
    [SerializeField] private float jumpTime = 0.2f;
    [SerializeField] private int maxAdditionalJumps = 2;
    private float _jumpCounter;
    private int _remainingJumps;
    private bool _isJumping;
    private bool _isJump;

    [Header("Wall Jump properties")] 
    [SerializeField] private Vector2 wallJumpingPower = new Vector2(10f, 10f);
    [SerializeField] private float wallJumpingTime = 0.2f;
    [SerializeField] private float wallJumpingDuration = 0.2f;
    [SerializeField] private float wallJumpCooldown = 1.5f;
    private float _wallJumpingDirection;
    private float _wallJumpingCounter;
    private float _recoverWallJumpTime;
    private int _lastWallId;
    private bool _isWallJumping;

    [Header("Wall sliding properties")] 
    [SerializeField] private float wallSlidingSpeed = 2f;
    private bool _isWallSliding;
    
    
    [Header("Ladder climbing properties")] 
    [SerializeField] private float ladderClimbingUpSpeed = 6f;
    [SerializeField] private float ladderClimbingDownSpeed = 3f;
    private bool _isLadderClimbing;

    private void Start()
    {
        _lastWallId = -1;
        _originalGravity = rb.gravityScale;
        _remainingDashes = maxAdditionalDashes;
        _vecGravity = new Vector2(0, -Physics2D.gravity.y);
    }

    private void Update()
    {
        _horizontal = Input.GetAxisRaw("Horizontal");
        
        DashRecover();
        WallJumpRecover();
        RenewAdditionalJumps();
        CheckJumpCondition();
        CheckDashCondition();
        ResetJumpCounter();
        WallSlide();
        WallJump();
        
        if (!_isWallJumping)
            Flip();
    }
    
    private void FixedUpdate()
    {
        if (_isDashing)
            return;
        
        if(_isWallSliding)
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        
        if (_isWallJumping)
            rb.velocity = new Vector2(_wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
        
        if (!_isWallJumping)
            HandleMovement();
        
        if (_shouldDash)
        {
            PerformDash();
            _shouldDash = false;
        }

        if (_isJump)
            Jump();
        
        LadderClimb();
        ApplyJumpPhysics();
    }
    
    private void HandleMovement()
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
    private void WallSlide()
    {
        if (IsWalled().Item1 && !IsGrounded() && _horizontal != 0f)
        {
            _isWallSliding = true;
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

    #region Jump

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x,  jumpForce);
        _isJump = false;
    }
    private void CheckJumpCondition()
    {
        if (Input.GetKeyDown(KeyCode.Space) && (_remainingJumps > 0 || IsGrounded()) && !_isWallSliding &&
            !_isLadderClimbing && !_isWallJumping && _wallJumpingCounter < 0f)
        {
            _isJump = true;
            _isJumping = true;
            _jumpCounter = 0;
            _remainingJumps--;
        }
    }
    

    private void RenewAdditionalJumps()
    {
        if (IsGrounded() && !Input.GetKeyDown(KeyCode.Space))
        {
            _remainingJumps = maxAdditionalJumps;
        }
    }
    
    private void ApplyJumpPhysics()
    {
        if (rb.velocity.y > 0 && _isJumping)
        {
            _jumpCounter += Time.deltaTime;
            if (_jumpCounter > jumpTime) _isJumping = false;

            var jumpFraction = _jumpCounter / jumpTime;
            var currentJumpMultiplier = jumpMultiplier;

            if (jumpFraction > 0.5f)
            {
                currentJumpMultiplier = jumpMultiplier * (1 - jumpFraction);
            }

            rb.velocity += _vecGravity * (currentJumpMultiplier * Time.deltaTime);
        }

        if (_isWallJumping) return;
        
        if (rb.velocity.y > 0 && !_isJumping)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.6f);
        }

        if (rb.velocity.y < 0)
        {
            rb.velocity -= _vecGravity * (fallMultiplier * Time.deltaTime);
        }
        
    }
    
    private void ResetJumpCounter()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            _isJumping = false;
            _jumpCounter = 0;
        }
    }

    #endregion
    
    #region Dash

    private void PerformDash()
    {
        _canDash = false;
        _isDashing = true;
        rb.gravityScale = 0;
        rb.velocity = new Vector2(transform.localScale.x * dashingPower, _dashVerticalDirection * dashingPower);
        StartCoroutine(EndDash());
    }

    private IEnumerator EndDash()
    {
        yield return new WaitForSeconds(dashingTime);
        rb.gravityScale = _originalGravity;
        _isDashing = false;
        _canDash = true;
    }

    private void CheckDashCondition()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && _canDash && !_isLadderClimbing && !_isWallSliding &&
            _remainingDashes > 0)
        {
            if (Input.GetKey(KeyCode.W) && _remainingDashes >= diagonalDashCost)
            {
                _remainingDashes -= diagonalDashCost;
                _dashVerticalDirection = dashVerticalForce;
                _shouldDash = true;
            }
            else if (!Input.GetKey(KeyCode.W))
            {
                _remainingDashes -= defaultDashCost;
                _dashVerticalDirection = 0;
                _shouldDash = true;
            }
        }
    }
    
    private void DashRecover()
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
    }

    #endregion

    #region WallJump

    private void WallJump()
    {
        var (isWalled, wallId) = IsWalled();
        if (_isWallSliding && wallId != _lastWallId)
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

        if (!Input.GetKeyDown(KeyCode.Space) || IsGrounded() || !(_wallJumpingCounter > 0f)) return;
        
        _lastWallId = wallId;
        _isWallJumping = true;
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

    private void StopWallJumping()
    {
        _isWallJumping = false;
    }

    private void WallJumpRecover()
    {
        if (_lastWallId != -1)
        {
            _recoverWallJumpTime += Time.deltaTime;
            if (_recoverWallJumpTime >= wallJumpCooldown)
            {
                _lastWallId = -1;
                _recoverWallJumpTime = 0;
            }
        }
    }
    #endregion

    #region LayerCheck
    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private (bool,int) IsWalled()
    {
        var wallCollider = Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
        return wallCollider ? (true, wallCollider.GetInstanceID()) : (false, -1);
    }

    private bool IsLadder()
    {
        return Physics2D.OverlapCircle(ladderCheck.position, 0.1f, ladderLayer);
    }
    #endregion
}