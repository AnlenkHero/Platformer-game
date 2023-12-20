using UnityEngine;



public class GrappleRope : MonoBehaviour
{
    [Header("General references")]
    public GrapplingGun grapplingGun;
    [SerializeField] LineRenderer lineRenderer;
    
    [Header("General Settings")]
    [SerializeField] private int precision = 20;
    [Range(0, 100)][SerializeField] private float straightenLineSpeed = 4;

    [Header("Animation")]
    public AnimationCurve ropeAnimationCurve;
    [SerializeField] [Range(0.01f, 4)] private float waveSize = 20;
    private float _waveSize;

    [Header("Rope Speed")]
    public AnimationCurve ropeLaunchSpeedCurve;
    [SerializeField] [Range(1, 50)] private float ropeLaunchSpeedMultiplayer = 4;

    private float _moveTime;

    public bool isGrappling;
    
    private bool _drawLine = true;
    private bool _straightLine = true;
    

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        lineRenderer.positionCount = precision;
        _waveSize = waveSize;
    }

    private void OnEnable()
    {
        _moveTime = 0;
        lineRenderer.enabled = true;
        lineRenderer.positionCount = precision;
        _waveSize = waveSize;
        _straightLine = false;
        LinePointToFirePoint();
    }

    private void OnDisable()
    {
        lineRenderer.enabled = false;
        isGrappling = false;
    }

    void LinePointToFirePoint()
    {
        for (int i = 0; i < precision; i++)
        {
            lineRenderer.SetPosition(i, grapplingGun.firePoint.position);
        }
    }

    void Update()
    {
        _moveTime += Time.deltaTime;

        if (_drawLine)
        {
            DrawRope();
        }
    }

    void DrawRope()
    {
        if (!_straightLine) 
        {
            if (lineRenderer.GetPosition(precision - 1).x != grapplingGun.grapplePoint.x)
            {
                DrawRopeWaves();
            }
            else 
            {
                _straightLine = true;
            }
        }
        else 
        {
            if (!isGrappling) 
            {
                grapplingGun.Grapple();
                isGrappling = true;
            }
            if (_waveSize > 0)
            {
                _waveSize -= Time.deltaTime * straightenLineSpeed;
                DrawRopeWaves();
            }
            else 
            {
                _waveSize = 0;
                DrawRopeNoWaves();
            }
        }
    }

    void DrawRopeWaves() 
    {
        for (int i = 0; i < precision; i++)
        {
            float delta = (float)i / ((float)precision - 1f);
            Vector2 offset = Vector2.Perpendicular(grapplingGun.distanceVector).normalized * ropeAnimationCurve.Evaluate(delta) * _waveSize;
            Vector2 targetPosition = Vector2.Lerp(grapplingGun.firePoint.position, grapplingGun.grapplePoint, delta) + offset;
            Vector2 currentPosition = Vector2.Lerp(grapplingGun.firePoint.position, targetPosition, ropeLaunchSpeedCurve.Evaluate(_moveTime) * ropeLaunchSpeedMultiplayer);

            lineRenderer.SetPosition(i, currentPosition);
        }
    }

    void DrawRopeNoWaves() 
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, grapplingGun.grapplePoint);
        lineRenderer.SetPosition(1, grapplingGun.firePoint.position);
    }

}
