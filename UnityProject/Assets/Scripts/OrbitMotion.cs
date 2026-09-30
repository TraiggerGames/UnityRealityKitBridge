using UnityEngine;

// A tiny example behavior. All animation is calculated in Unity; RealityKit
// receives resulting transforms only.
public sealed class OrbitMotion : MonoBehaviour
{
    [SerializeField] private Vector3 center = new Vector3(0f, 1.45f, -1.4f);
    [SerializeField] private float radius = 0.45f;
    [SerializeField] private float speed = 0.65f;
    [SerializeField] private float phase;
    private bool boosted;

    public void Configure(Vector3 origin, float orbitRadius, float orbitSpeed, float startPhase)
    {
        center = origin;
        radius = orbitRadius;
        speed = orbitSpeed;
        phase = startPhase;
    }

    public void SetBoosted(bool value) { boosted = value; }

    private void Update()
    {
        float angle = Time.time * speed * (boosted ? 2f : 1f) + phase;
        transform.position = center + new Vector3(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle * 1.5f) * 0.07f,
            Mathf.Sin(angle) * radius
        );
        transform.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
    }
}
