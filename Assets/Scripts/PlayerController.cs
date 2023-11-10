using UnityEngine;


public class PlayerController : MonoBehaviour
{
    [SerializeField] private float jumpForce;
    private float _horizontal;
    
    [SerializeField] private float maxHorizontalSpeed; // This will be the maximum speed the player can reach
    [SerializeField] private float accelerationTime; // How fast the player will reach the maximum speed
    private bool _isJumping;
    private float _currentHorizontalSpeed;
    [SerializeField] private float brakingTime;
        [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    private Vector2 _vecGravity;
    [SerializeField] private float fallMultiplier;
    [SerializeField] private float jumpMultiplier;
    [SerializeField] private float jumpTime;
    private float _jumpCounter;
    private bool _isJump;

    private Rigidbody2D _rigidbody2D;

    void Start()
    {
        _vecGravity = new Vector2(0, -Physics2D.gravity.y);
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }
    

    private void Update()
    {
        
        _horizontal = Input.GetAxisRaw("Horizontal");
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            _isJump = true;
            _isJumping = true;
            _jumpCounter = 0;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            _isJumping = false;
            _jumpCounter = 0;
        }
        

        Flip();
    }

    private void FixedUpdate()
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
        
        _rigidbody2D.velocity = new Vector2(_currentHorizontalSpeed, _rigidbody2D.velocity.y);
        
        if (_isJump)
        {
            Jump();
        }
        
        if (_rigidbody2D.velocity.y > 0 && _isJumping)
        {
            _jumpCounter += Time.deltaTime;
            if (_jumpCounter > jumpTime) _isJumping = false;

            float jumpFraction = _jumpCounter / jumpTime;
            float currentJumpMultiplier = jumpMultiplier;

            if (jumpFraction > 0.5f)
            {
                currentJumpMultiplier = jumpMultiplier * (1 - jumpFraction);
            }
            
            _rigidbody2D.velocity += _vecGravity * (currentJumpMultiplier * Time.deltaTime);
        }
        
        if (_rigidbody2D.velocity.y > 0 && !_isJumping)
        {
            _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, _rigidbody2D.velocity.y * 0.6f);
        }
        
        if (_rigidbody2D.velocity.y < 0)
        {
            _rigidbody2D.velocity -= _vecGravity * (fallMultiplier * Time.deltaTime);
            
        }
    }

    bool IsGrounded()
    {
       return Physics2D.OverlapCapsule(groundCheck.position, new Vector2(0.8f, 0.1f),
            CapsuleDirection2D.Horizontal, 0, groundLayer);
    }

    private void Jump()
    {
        _rigidbody2D.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        _isJump = false;
    }

    private void Flip()
    {
        if (_horizontal < -0.01f) transform.localScale = new Vector3(-1, 1, 1);
        if (_horizontal > 0.01f) transform.localScale = new Vector3(1, 1, 1);
    }
}