using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class Target : MonoBehaviour
{
    public float Radius { get; set; }
    public bool WasHit { get; private set; }
    void OnCollisionEnter(Collision collision)
    {
        if (WasHit || !collision.gameObject.TryGetComponent<QuadraticDrag>(out var projectile)) return;
        WasHit = true;
        if (projectile.Owner) projectile.Owner.RegisterHit();
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", new Color(0.2f, 1, 0.4f));
        GetComponent<Renderer>().SetPropertyBlock(block);
    }
}
