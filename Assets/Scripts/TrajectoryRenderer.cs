using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class TrajectoryRenderer : MonoBehaviour
{
    public Transform muzzle;
    public LayerMask collisionMask = ~(1 << 8 | 1 << 9);
    [Min(10)] public int steps = 400;
    public Vector3 Endpoint { get; private set; }
    public bool HasContact { get; private set; }
    public float Mass { get; private set; }
    public float Radius { get; private set; }
    readonly Vector3[] points = new Vector3[2001];
    public void Predict(float mass, float radius, float speed, float cd, float rho, Vector3 wind)
    {
        Mass = mass; Radius = radius;
        Vector3 p = muzzle.position, v = muzzle.forward * speed;
        points[0] = p; int count = 1; HasContact = false;
        for (int i = 0; i < Mathf.Min(steps, 2000); i++)
        {
            v += (Physics.gravity + QuadraticDrag.DragForce(v, radius, cd, rho, wind) / mass) * Time.fixedDeltaTime;
            Vector3 delta = v * Time.fixedDeltaTime;
            if (Physics.SphereCast(p, radius, delta.normalized, out RaycastHit hit, delta.magnitude, collisionMask, QueryTriggerInteraction.Ignore))
            {
                p += delta.normalized * hit.distance;
                points[count++] = p; HasContact = true; break;
            }
            p += delta; points[count++] = p;
        }
        Endpoint = p;
        var line = GetComponent<LineRenderer>();
        line.positionCount = count;
        for (int i = 0; i < count; i++) line.SetPosition(i, points[i]);
    }
}
