using UnityEngine;

[DisallowMultipleComponent]
public class DamageZone : MonoBehaviour
{
    [Tooltip("是否只造成一次伤害（进入时）")]
    public bool singleHit = true;

    [Tooltip("每秒造成一次持续伤害（当 singleHit=false 时生效）")]
    public float damageInterval = 1f;

    [Tooltip("Hunter 类型造成的伤害百分比（0-1，例 0.1 = 10%）")]
    [Range(0f, 1f)]
    public float hunterDamagePercent = 0.1f;

    [Tooltip("可选：只对带有此标签的对象生效（例如 Player）")]
    public string targetTag = "Player";

    private float tickTimer = 0f;

    private void Reset()
    {
        // 默认在编辑器中创建时确保有触发器
        var col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsTarget(other)) return;

        var resource = other.GetComponent<PlayerResource>();
        if (resource == null) return;

        // 立即造成一次伤害
        ApplyHunterDamage(resource);

        if (!singleHit)
        {
            tickTimer = 0f;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (singleHit) return;
        if (!IsTarget(other)) return;

        var resource = other.GetComponent<PlayerResource>();
        if (resource == null) return;

        tickTimer += Time.deltaTime;
        if (tickTimer >= damageInterval)
        {
            tickTimer = 0f;
            ApplyHunterDamage(resource);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsTarget(other)) return;
        // 重置计时器以避免离开再进导致立即伤害
        tickTimer = 0f;
    }

    private bool IsTarget(Collider other)
    {
        if (!string.IsNullOrEmpty(targetTag))
        {
            return other.CompareTag(targetTag);
        }
        return other.GetComponent<PlayerResource>() != null;
    }

    private void ApplyHunterDamage(PlayerResource resource)
    {
        if (resource == null) return;
        float current = resource.CurrentHealth;
        float damage = current * hunterDamagePercent;
        resource.SetHealthNormalized(Mathf.Max(0f, current - damage));
    }
}
