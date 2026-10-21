using System.Collections.Generic;
using UnityEngine;

public sealed class TargetSpawner : MonoBehaviour
{
    public Target targetPrefab;
    public Vector2 massRange = new Vector2(1, 5), radiusRange = new Vector2(0.4f, 1);
    public Vector2 speedRange = new Vector2(2, 5);
    public float interval = 2.5f, lifetime = 10, height = 8, distance = 40, halfWidth = 12;
    public int maxTargets = 4;
    public readonly List<Target> Living = new List<Target>();
    float nextSpawn;
    void Update()
    {
        Living.RemoveAll(t => !t);
        if (Time.time < nextSpawn) return;
        nextSpawn = Time.time + Mathf.Max(0.1f, interval);
        if (Living.Count < maxTargets) Spawn();
    }
    public Target Spawn()
    {
        Living.RemoveAll(t => !t);
        if (Living.Count >= maxTargets) return null;
        float side = Random.value < 0.5f ? -1 : 1;
        Vector3 pos = transform.position + new Vector3(side * halfWidth, height, distance + Random.Range(-2f, 2f));
        Target target = Instantiate(targetPrefab, pos, Quaternion.identity);
        target.Radius = Random.Range(radiusRange.x, radiusRange.y);
        target.transform.localScale = Vector3.one * (2 * target.Radius);
        var body = target.GetComponent<Rigidbody>();
        body.mass = Random.Range(massRange.x, massRange.y); body.useGravity = false; body.linearDamping = 0;
        body.linearVelocity = new Vector3(-side * Random.Range(speedRange.x, speedRange.y), 0, 0);
        Living.Add(target); Destroy(target.gameObject, Mathf.Max(0.1f, lifetime));
        return target;
    }
}
