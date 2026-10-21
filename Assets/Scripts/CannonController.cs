using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CannonController : MonoBehaviour
{
    public Transform muzzle, barrelPivot;
    public QuadraticDrag projectilePrefab;
    public TrajectoryRenderer trajectory;
    public Vector2 massRange = new Vector2(2, 6), radiusRange = new Vector2(0.1f, 0.2f);
    public float launchSpeed = 35, moveSpeed = 6, turnSpeed = 45;
    public float airDensity = 1.225f, dragCoefficient = 0.47f;
    public Vector3 wind = Vector3.zero;
    public float projectileLifetime = 15;
    public float NextMass { get; private set; }
    public float NextRadius { get; private set; }
    public int Shots { get; private set; }
    public int Hits { get; private set; }
    public QuadraticDrag LastProjectile { get; private set; }
    float elevation = 20;
    void Start() { PrepareNext(); }
    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;
        Vector3 move = new Vector3((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), 0,
            (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
        transform.position += Vector3.ClampMagnitude(move, 1) * moveSpeed * Time.deltaTime;
        Vector3 p = transform.position; p.x = Mathf.Clamp(p.x, -18, 18); p.z = Mathf.Clamp(p.z, -8, 15); transform.position = p;
        transform.Rotate(Vector3.up, ((k.eKey.isPressed ? 1 : 0) - (k.qKey.isPressed ? 1 : 0)) * turnSpeed * Time.deltaTime, Space.World);
        elevation = Mathf.Clamp(elevation + ((k.upArrowKey.isPressed ? 1 : 0) - (k.downArrowKey.isPressed ? 1 : 0)) * 25 * Time.deltaTime, 5, 65);
        barrelPivot.localRotation = Quaternion.Euler(-elevation, 0, 0);
        if (k.spaceKey.wasPressedThisFrame) Fire();
    }
    void LateUpdate() { RefreshPrediction(); }
    void PrepareNext()
    {
        NextMass = Random.Range(massRange.x, massRange.y);
        NextRadius = Random.Range(radiusRange.x, radiusRange.y);
        RefreshPrediction();
    }
    public void RefreshPrediction() { trajectory.Predict(NextMass, NextRadius, launchSpeed, dragCoefficient, airDensity, wind); }
    public QuadraticDrag Fire()
    {
        var shot = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
        shot.Owner = this;
        shot.SetPhysicalParams(NextMass, NextRadius, dragCoefficient, airDensity, wind, muzzle.forward * launchSpeed);
        foreach (var c in GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(shot.GetComponent<Collider>(), c);
        LastProjectile = shot; Shots++;
        Destroy(shot.gameObject, projectileLifetime);
        PrepareNext();
        return shot;
    }
    public void RegisterHit() { Hits++; }
}
