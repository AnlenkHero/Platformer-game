using UnityEngine;

public class GrapplingGun : MonoBehaviour
{
    [Header("Scripts")]
    public GrappleRope grappleRope;
    [Header("Layer Settings")]
    [SerializeField] private bool grappleToAll = false;
    [SerializeField] private int grappableLayerNumber = 9;

    [Header("Main Camera")]
    public Camera mainCamera;

    [Header("Transform References")]
    public Transform gunHolder;
    public Transform gunPivot;
    public Transform firePoint;

    [Header("Rotation")]
    [SerializeField] private bool rotateOverTime = true;
    [Range(0, 80)] [SerializeField] private float rotationSpeed = 4;

    [Header("Distance")]
    [SerializeField] private bool hasMaxDistance = true;
    [SerializeField] private float maxDistance = 4;

    [Header("Launching")]
    [SerializeField] private bool launchToPoint = true;
    [SerializeField] private LaunchType launchType = LaunchType.Transform_Launch;
    [Range(0, 5)] [SerializeField] private float launchSpeed = 5;

    
    [Header("No Launch To Point")]
    [SerializeField] private bool autoConfigureDistance;
    [SerializeField] private float targetDistance = 3;
    [SerializeField] private float targetFrequency = 3;


    private enum LaunchType
    {
        Transform_Launch,
        Physics_Launch,
    }

    [Header("Component References")]
    public SpringJoint2D springJoint2D;

    [HideInInspector] public Vector2 grapplePoint;
    [HideInInspector] public Vector2 distanceVector;
    private Vector2 mouseFirePointDistanceVector;

    public Rigidbody2D ballRigidbody;


    private void Start()
    {
        grappleRope.enabled = false;
        springJoint2D.enabled = false;
        ballRigidbody.gravityScale = 5;
    }

    private void Update()
    {
       
        mouseFirePointDistanceVector = mainCamera.ScreenToWorldPoint(Input.mousePosition) - gunPivot.position;

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            SetGrapplePoint();
            PlayerMovement.Instance.isGrappling = true;
            PlayerMovement.Instance.isPhysicsCanceledAfterGrapple = true;
        }

        if (Input.GetKeyDown(KeyCode.Mouse1) && grappleRope.enabled)
        {
            launchToPoint = true;
            Grapple();
            PlayerMovement.Instance.isGrappling = true;
            PlayerMovement.Instance.isPhysicsCanceledAfterGrapple = true;
        }
        else if (Input.GetKey(KeyCode.Mouse0))
        {
            if (grappleRope.enabled)
            {
                RotateGun(grapplePoint, false);
            }
            else
            {
                RotateGun(mainCamera.ScreenToWorldPoint(Input.mousePosition), false);
            }

            if (launchToPoint && grappleRope.isGrappling)
            {
                if (launchType == LaunchType.Transform_Launch)
                {
                    gunHolder.position = Vector3.Lerp(gunHolder.position, grapplePoint, Time.deltaTime * launchSpeed);
                }
            }

        }
        else if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            PlayerMovement.Instance.isGrappling = false;
            grappleRope.enabled = false;
            springJoint2D.enabled = false;
            ballRigidbody.gravityScale = 5;
        }
        else
        {
            RotateGun(mainCamera.ScreenToWorldPoint(Input.mousePosition), true);
        }
    }

    void RotateGun(Vector3 lookPoint, bool allowRotationOverTime)
    {
        Vector3 distanceVector = lookPoint - gunPivot.position;

        float angle = Mathf.Atan2(distanceVector.y, distanceVector.x) * Mathf.Rad2Deg;
        if (rotateOverTime && allowRotationOverTime)
        {
            Quaternion startRotation = gunPivot.rotation;
            gunPivot.rotation = Quaternion.Lerp(startRotation, Quaternion.AngleAxis(angle, Vector3.forward), Time.deltaTime * rotationSpeed);
        }
        else
            gunPivot.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

    }

    void SetGrapplePoint()
    {
        if (Physics2D.Raycast(firePoint.position, mouseFirePointDistanceVector.normalized))
        {
            RaycastHit2D _hit = Physics2D.Raycast(firePoint.position, mouseFirePointDistanceVector.normalized);
            if ((_hit.transform.gameObject.layer == grappableLayerNumber || grappleToAll) && ((Vector2.Distance(_hit.point, firePoint.position) <= maxDistance) || !hasMaxDistance))
            {
                grapplePoint = _hit.point;
                distanceVector = grapplePoint - (Vector2)gunPivot.position;
                grappleRope.enabled = true;
            }
        }
    }

    public void Grapple()
    {

        if (!launchToPoint && !autoConfigureDistance)
        {
            springJoint2D.distance = targetDistance;
            springJoint2D.frequency = targetFrequency;
        }

        if (!launchToPoint)
        {
            if (autoConfigureDistance)
            {
                springJoint2D.autoConfigureDistance = true;
                springJoint2D.frequency = 0;
            }
            springJoint2D.connectedAnchor = grapplePoint;
            springJoint2D.enabled = true;
        }

        else
        {
            if (launchType == LaunchType.Transform_Launch)
            {
                ballRigidbody.gravityScale = 0;
                ballRigidbody.velocity = Vector2.zero;
            }
            if (launchType == LaunchType.Physics_Launch)
            {
                springJoint2D.connectedAnchor = grapplePoint;
                springJoint2D.distance = 0;
                springJoint2D.frequency = launchSpeed;
                springJoint2D.enabled = true;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (hasMaxDistance)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(firePoint.position, maxDistance);
        }
    }

}
