using UnityEngine;
using UnityEngine.UI;

public class PlayerCrosshair : MonoBehaviour
{
    public Image crosshair;
    public Sprite normalSprite;
    public Sprite interactSprite;
    public Sprite shootSprite;
    public Sprite alertSprite;
    [Header("Optional")]
    public Camera targetCamera;

    [Header("Raycast Options")]
    [Tooltip("If set, the raycast will originate from this Transform.position and use its forward direction. Useful for third-person to originate rays from the player head/weapon instead of the camera to avoid occlusion by the player model.")]
    public Transform rayOriginOverride;
    [Tooltip("LayerMask used for interaction raycasts. Default uses Unity's DefaultRaycastLayers.")]
    public LayerMask raycastMask = Physics.DefaultRaycastLayers;
    [Tooltip("Max distance for interaction raycasts.")]
    public float rayDistance = 300f;

    private Camera cachedCamera;
    // cached reference to the player root transform so we can ignore player's colliders
    private Transform playerRootTransform;
    // remember last frame time we validated camera so we don't do expensive search every call if not needed
    private float lastCameraCheckTime = 0f;
    private const float cameraCheckInterval = 0.25f; // seconds

    private void Awake()
    {
        // Auto-assign the Image if not set in Inspector
        if (crosshair == null)
        {
            crosshair = GetComponentInChildren<Image>();
            if (crosshair != null)
                Debug.Log("PlayerCrosshair: auto-assigned crosshair Image from children.");
        }

        // Try to resolve a camera now, but don't spam warnings at Awake: it's common for cameras to be initialized later.
        cachedCamera = ResolveBestCamera();
        if (cachedCamera != null)
        {
            
        }
        else
        {
            // Informational only: will retry during updates
            Debug.Log("PlayerCrosshair: no Camera found at Awake; will search during Update.");
        }

        // If there's a PlayerCamera in the scene, prefer its currently active camera so we match the player view at startup
        PlayerCamera scenePlayerCamera = FindObjectOfType<PlayerCamera>();
        if (scenePlayerCamera != null)
        {
            Camera pcam = null;
            // choose which transform to query based on the PlayerCamera's isFirstPerson flag
            if (scenePlayerCamera.isFirstPerson && scenePlayerCamera.firstPersonCamera != null)
                pcam = scenePlayerCamera.firstPersonCamera.GetComponent<Camera>() ?? scenePlayerCamera.firstPersonCamera.GetComponentInChildren<Camera>();
            else if (!scenePlayerCamera.isFirstPerson && scenePlayerCamera.thirdPersonCamera != null)
                pcam = scenePlayerCamera.thirdPersonCamera.GetComponent<Camera>() ?? scenePlayerCamera.thirdPersonCamera.GetComponentInChildren<Camera>();

            if (pcam != null)
            {
                cachedCamera = pcam;
                targetCamera = pcam;
                Debug.Log($"PlayerCrosshair: bound to PlayerCamera active camera '{cachedCamera.name}' at Awake.");
            }
        }

        // Cache the Player root if present so we can ignore its colliders during raycasts
        Player playerComp = GetComponentInParent<Player>();
        if (playerComp == null)
        {
            playerComp = FindObjectOfType<Player>();
        }
        if (playerComp != null)
        {
            playerRootTransform = playerComp.transform;
            Debug.Log($"PlayerCrosshair: cached player root '{playerRootTransform.name}' for raycast ignore.");
        }
    }

    // Expose a utility to retrieve the current aim point in world space (first non-player hit) if any.
    public bool TryGetAimPoint(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        if (cachedCamera == null) cachedCamera = ResolveBestCamera();
        if (cachedCamera == null) return false;

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray ray = cachedCamera.ScreenPointToRay(screenCenter);

        RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, raycastMask);
        if (hits == null || hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (h.collider.GetComponentInParent<Player>() != null) continue;
            if (playerRootTransform != null)
            {
                Transform t = h.collider.transform;
                bool belongsToPlayer = false;
                while (t != null)
                {
                    if (t == playerRootTransform)
                    {
                        belongsToPlayer = true;
                        break;
                    }
                    t = t.parent;
                }
                if (belongsToPlayer) continue;
            }

            worldPoint = h.point;
            return true;
        }

        return false;
    }

    // Always return an aim point: if there's a hit return that, otherwise return a point far along the camera forward direction
    public bool GetAimPointOrFallback(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        // try real hit first
        if (TryGetAimPoint(out Vector3 hitPoint))
        {
            worldPoint = hitPoint;
            return true;
        }

        // fallback: use camera forward
        if (cachedCamera == null) cachedCamera = ResolveBestCamera();
        if (cachedCamera == null) return false;

        worldPoint = cachedCamera.transform.position + cachedCamera.transform.forward * rayDistance;
        return true;
    }

    // Allow external components (e.g. PlayerCamera) to explicitly set which camera to use. 
    public void SetCamera(Camera cam)
    {
        if (cam == null)
            return;
        cachedCamera = cam;
        targetCamera = cam;
    }

    // Expose the currently resolved camera for other systems to use. May be null if none found.
    public Camera GetResolvedCamera()
    {
        if (cachedCamera == null || !cachedCamera.enabled || !cachedCamera.gameObject.activeInHierarchy)
            cachedCamera = ResolveBestCamera();
        return cachedCamera;
    }

    // Finds the best available Camera to use for raycasts.
    private Camera ResolveBestCamera()
    {
        // 1) Explicit assignment in Inspector
        if (targetCamera != null && targetCamera.enabled && targetCamera.gameObject.activeInHierarchy)
            return targetCamera;

        // 2) Camera.main (tagged MainCamera)
        if (Camera.main != null && Camera.main.enabled && Camera.main.gameObject.activeInHierarchy)
            return Camera.main;

        // 3) Choose among all Cameras: prefer enabled and active with highest depth
        Camera best = null;
        float bestDepth = float.NegativeInfinity;
        Camera[] all = Camera.allCameras;
        for (int i = 0; i < all.Length; i++)
        {
            Camera c = all[i];
            if (c == null) continue;
            if (!c.enabled) continue;
            if (!c.gameObject.activeInHierarchy) continue;
            if (c.depth > bestDepth)
            {
                best = c;
                bestDepth = c.depth;
            }
        }

        return best;
    }

    // Call every frame from the Player controller
    public void UpdateCrosshair(PlayerMode.Mode mode, bool shootPressed)
    {
        if (crosshair == null)
        {
            
            return;
        }

        // Debug: log mode and shootPressed when in Shooting mode or when changed
        if (mode == PlayerMode.Mode.Shooting || shootPressed)
        {
            
        }

        // Priority: if in shooting mode, immediately show the shoot sprite
        if (mode == PlayerMode.Mode.Shooting)
        {
            if (shootSprite != null)
            {
                if (crosshair.sprite != shootSprite)
                {
                    crosshair.sprite = shootSprite;

                }
                else
                {

                }
            }
            else
            {
                
            }
            return;
        }

        // Ensure we have a valid camera reference (try again if null or inactive)
        if (cachedCamera == null || !cachedCamera.enabled || !cachedCamera.gameObject.activeInHierarchy || (targetCamera != null && cachedCamera != targetCamera))
        {
            lastCameraCheckTime = Time.time;
            cachedCamera = ResolveBestCamera();
        }

        if (cachedCamera == null)
        {
            
            return;
        }

        // We'll set interact sprite if we detect an interactable; otherwise ensure normal/clear at the end
        bool didSetInteract = false;

        // 始终用摄像机ScreenPointToRay生成射线，保证射线起点和crosshair圆心一致
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray ray = cachedCamera.ScreenPointToRay(screenCenter);
        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.magenta);

        // Use RaycastAll with the configured mask so we can inspect and ignore player's own colliders
        RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, raycastMask);
        if (hits != null && hits.Length > 0)
        {
            // sort by distance ascending
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            RaycastHit? chosen = null;
            foreach (var h in hits)
            {
                // ignore null colliders defensively
                if (h.collider == null) continue;
                // if this hit belongs to the player (any parent has a Player component), skip it
                if (h.collider.GetComponentInParent<Player>() != null)
                {
                    continue;
                }
                // fallback: if we cached a specific player root transform, also skip children of it
                if (playerRootTransform != null)
                {
                    Transform t = h.collider.transform;
                    bool belongsToPlayer = false;
                    while (t != null)
                    {
                        if (t == playerRootTransform)
                        {
                            belongsToPlayer = true;
                            break;
                        }
                        t = t.parent;
                    }
                    if (belongsToPlayer)
                    {
                        continue;
                    }
                }

                // first non-player hit
                chosen = h;
                break;
            }

            if (chosen.HasValue)
            {
                var hit = chosen.Value;
                // 如果命中对象带有 Ethereal 标签，显示交互（interactSprite）
                if (hit.collider.CompareTag("Ethereal"))
                {
                    if (interactSprite != null)
                    {
                        crosshair.sprite = interactSprite;
                        didSetInteract = true;
                    }
                }
                // 命中 NPC 时显示警告（alertSprite）
                else if (hit.collider.CompareTag("NPC"))
                {
                    if (alertSprite != null)
                    {
                        crosshair.sprite = alertSprite;
                        didSetInteract = true;
                    }
                }
            }
            else
            {
                
            }
        }
        else
        {

        }

        // If we didn't set interact sprite this frame, ensure we show the normal sprite (or clear it)
        if (!didSetInteract)
        {
            if (normalSprite != null)
                crosshair.sprite = normalSprite;
            else
                crosshair.sprite = null;
        }
    }
}
