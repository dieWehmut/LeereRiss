using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("Shoot Settings")]
    public Transform muzzle;         // 枪口位置
    public GameObject bulletPrefab;  // 子弹 prefab
    public float bulletSpeed = 100f;  // 子弹初速度

    [Header("Audio")]
    [Tooltip("射击音效 (使用 PlayOneShot 播放)")]
    public AudioClip shootClip;
    [Tooltip("可选：指定用于播放射击音效的 AudioSource。若为空，将在运行时创建一个缓存的 AudioSource。")]
    public AudioSource audioSource;

    [Header("Optional")]
    public float shootCooldown = 0.1f; // 射击间隔
    private float lastShootTime = 0f;

    public void OnShoot()
    {
        if (muzzle == null || bulletPrefab == null) return;

        if (Time.time - lastShootTime < shootCooldown) return; // 射击冷却
        lastShootTime = Time.time;

        // Determine firing direction from crosshair aim point (prefer PlayerCrosshair)
        Vector3 aimPoint;
        Vector3 fireDirection = muzzle.forward;
        PlayerCrosshair pc = FindObjectOfType<PlayerCrosshair>();
        if (pc != null && pc.GetAimPointOrFallback(out aimPoint))
        {
            fireDirection = (aimPoint - muzzle.position).normalized;
        }

        // Instantiate bullet aligned to fire direction
        Quaternion rot = Quaternion.LookRotation(fireDirection, Vector3.up);
        GameObject bullet = Instantiate(bulletPrefab, muzzle.position, rot);

        // Give bullet initial velocity
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = fireDirection * bulletSpeed;
        }

        // Add BulletAcceleration component to allow continuous acceleration (optional)
        BulletAcceleration ba = bullet.GetComponent<BulletAcceleration>();
        if (ba == null)
        {
            ba = bullet.AddComponent<BulletAcceleration>();
            // sensible default: accelerate along local forward (positive z) at small value
            ba.acceleration = new Vector3(0f, 0f, 0f);
            ba.useLocalForward = true;
        }

        
        Destroy(bullet, 10f); 

        // Play shooting sound (if provided)
        if (shootClip != null)
        {
            // create a temporary AudioSource if none provided or cached
            if (audioSource == null)
            {
                // cache a new AudioSource on this GameObject to avoid repeated allocations
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            audioSource.PlayOneShot(shootClip);
        }
    }

    private void Awake()
    {
        // ensure we have an AudioSource if a clip is assigned but no source specified
        if (shootClip != null && audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }
}
