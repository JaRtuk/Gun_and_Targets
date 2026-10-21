using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class QuadraticDrag : MonoBehaviour
{
    public float Mass { get; private set; }
    public float Radius { get; private set; }
    public float Cd { get; private set; }
    public float AirDensity { get; private set; }
    public Vector3 Wind { get; private set; }
    public Rigidbody Body { get; private set; }
    public bool HasContact { get; private set; }
    public Vector3 FirstContactCenter { get; private set; }
    public CannonController Owner { get; set; }

    void Awake() { Body = GetComponent<Rigidbody>(); }
    public static Vector3 DragForce(Vector3 velocity, float radius, float cd, float rho, Vector3 wind)
    {
        Vector3 relative = velocity - wind;
        return -0.5f * rho * cd * Mathf.PI * radius * radius * relative.magnitude * relative;
    }
    public void SetPhysicalParams(float m, float r, float cd, float rho, Vector3 wind, Vector3 initialVelocity)
    {
        Mass = Mathf.Max(0.001f, m); Radius = Mathf.Max(0.001f, r);
        Cd = Mathf.Max(0, cd); AirDensity = Mathf.Max(0, rho); Wind = wind;
        if (!Body) Body = GetComponent<Rigidbody>();
        transform.localScale = Vector3.one * (2 * Radius);
        GetComponent<SphereCollider>().radius = 0.5f;
        Body.mass = Mass; Body.linearDamping = 0; Body.angularDamping = 0;
        Body.useGravity = true; Body.maxLinearVelocity = 200;
        Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Body.linearVelocity = initialVelocity;
    }
    void FixedUpdate()
    {
        Body.AddForce(DragForce(Body.linearVelocity, Radius, Cd, AirDensity, Wind), ForceMode.Force);
    }
    void OnCollisionEnter(Collision collision)
    {
        if (HasContact) return;
        HasContact = true;
        FirstContactCenter = Body.position;
    }
}
