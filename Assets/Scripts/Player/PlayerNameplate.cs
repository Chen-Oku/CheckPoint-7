using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class PlayerNameplate : MonoBehaviourPun
{
    [Header("Text")]
    public TMP_Text nameTextTMP;
    public Text nameTextLegacy;

    [Header("Billboard")]
    [Tooltip("Si true la placa solo rota en Y (se mantiene 'upright'). Si false enfrenta completamente la cámara.")]
    public bool keepUpright = true;

    [Tooltip("Giro extra en Y para corregir si la placa queda invertida.")]
    public bool flip180 = false;

    [Tooltip("Si se quiere usar una cámara distinta a Camera.main, asígnala aquí.")]
    public Camera targetCamera;

    Camera cam;
    private PhotonView pvRef;

    [Header("Locking")]
    [Tooltip("If true, the nameplate will keep a fixed world rotation and will NOT rotate with the player.")]
    public bool keepStaticRotation = false;

    [Tooltip("World Euler rotation to lock to when 'keepStaticRotation' is true. Default points 'up'.")]
    public Vector3 lockedWorldEuler = new Vector3(-90f, 0f, 0f);

    private Quaternion lockedRotation;

    void OnEnable()
    {
        cam = ResolveCamera();
        lockedRotation = Quaternion.Euler(lockedWorldEuler);
        pvRef = GetComponent<PhotonView>() ?? GetComponentInParent<PhotonView>();
    }

    Camera ResolveCamera()
    {
        if (targetCamera != null) return targetCamera;

        // 1) Buscar cámara "local" del jugador: camera cuyo GameObject tiene un PhotonView.IsMine en sus padres.
        for (int i = 0; i < Camera.allCamerasCount; i++)
        {
            var c = Camera.allCameras[i];
            if (c == null || !c.isActiveAndEnabled) continue;
            var pv = c.gameObject.GetComponentInParent<PhotonView>();
            if (pv != null && pv.IsMine) return c;
        }

        // 2) Camera.main si existe y está activa
        if (Camera.main != null && Camera.main.isActiveAndEnabled) return Camera.main;

        // 3) Buscar por tag "MainCamera"
        var tagged = GameObject.FindGameObjectWithTag("MainCamera");
        if (tagged != null)
        {
            var tc = tagged.GetComponent<Camera>();
            if (tc != null && tc.isActiveAndEnabled) return tc;
        }

        // 4) Fallback: elegir la cámara activa con mayor depth
        Camera best = null;
        float bestDepth = float.MinValue;
        for (int i = 0; i < Camera.allCamerasCount; i++)
        {
            var c = Camera.allCameras[i];
            if (c == null || !c.isActiveAndEnabled) continue;
            if (c.depth > bestDepth)
            {
                best = c;
                bestDepth = c.depth;
            }
        }
        if (best != null) return best;

        return null;
    }

    public void SetCamera(Camera camToUse)
    {
        targetCamera = camToUse;
        cam = ResolveCamera();
    }

    void LateUpdate()
    {
        // Update name text (in case Owner/Nick changed) — lightweight
        UpdateNameTextIfNeeded();

        if (cam == null) cam = ResolveCamera();
        if (cam == null) return;

        // If requested, keep a fixed world rotation (prevents rotation following the parent/player)
        if (keepStaticRotation)
        {
            transform.rotation = lockedRotation;
            return;
        }

        Vector3 cameraPos = cam.transform.position;
        Vector3 dir = cameraPos - transform.position;

        if (keepUpright)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.0001f) return;
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            if (flip180) targetRot *= Quaternion.Euler(0f, 180f, 0f);
            ApplyRotation(targetRot);
        }
        else
        {
            if (dir.sqrMagnitude <= 0.0001f) return;
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            if (flip180) targetRot *= Quaternion.Euler(0f, 180f, 0f);
            ApplyRotation(targetRot);
        }
    }

    void ApplyRotation(Quaternion targetRot)
    {
        // Rotación instantánea (sin smoothing)
        transform.rotation = targetRot;
    }

    void UpdateNameTextIfNeeded()
    {
        string nick = "Unknown";
        if (pvRef != null && pvRef.Owner != null) nick = pvRef.Owner.NickName;

        if (nameTextTMP != null && nameTextTMP.text != nick) nameTextTMP.text = nick;
        else if (nameTextLegacy != null && nameTextLegacy.text != nick) nameTextLegacy.text = nick;
    }

    // método público opcional para actualizar el texto desde otro script si hace falta
    public void SetName(string nick)
    {
        if (string.IsNullOrEmpty(nick)) nick = "Unknown";
        if (nameTextTMP != null) nameTextTMP.text = nick;
        else if (nameTextLegacy != null) nameTextLegacy.text = nick;
    }
}
