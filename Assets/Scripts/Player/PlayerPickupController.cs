using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickupController : MonoBehaviourPun
{
    [Header("Configuración")]
    [SerializeField] private float pickupRadius = 2f; // Radio de detección
    [SerializeField] private Transform handPoint;
    [SerializeField] private LayerMask interactLayer;

    private GameObject currentItem;
    private PlayerInput playerInput;

    private void Start()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        // Usamos la acción "Interact" (Tecla E / Botón Gamepad)
        if (playerInput.actions["Interact"].WasPressedThisFrame())
        {
            if (currentItem == null) TryPickup();
            else DropItem();
        }
    }

    private void TryPickup()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius, interactLayer);

        PhotonView closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            PhotonView targetView = col.GetComponent<PhotonView>();

            // VERIFICACIÓN EXTRA: && targetView.ViewID != 0
            // Si el ViewID es 0, ignoramos el objeto porque está "roto" para Photon.
            if (targetView != null && targetView != photonView && targetView.ViewID != 0)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = targetView;
                }
            }
        }

        if (closestTarget != null)
        {
            // OTRA SEGURIDAD:
            // Solo pedimos dueño si el objeto permite ser robado (OwnershipTransfer != Fixed)
            // O si ya somos dueños, no pedimos nada.
            if (!closestTarget.IsMine)
            {
                closestTarget.RequestOwnership();
            }

            // IMPORTANTE: Enviamos el ID al RPC
            photonView.RPC("RPC_PickupItem", RpcTarget.All, closestTarget.ViewID);
        }
    }

    private void DropItem()
    {
        if (currentItem != null)
        {
            photonView.RPC("RPC_DropItem", RpcTarget.All);
        }
    }

    // RPCs

    [PunRPC]
    public void RPC_PickupItem(int viewID)
    {
        PhotonView targetView = PhotonView.Find(viewID);

        if (targetView != null && targetView.gameObject != null)
        {
            currentItem = targetView.gameObject;

            Rigidbody rb = currentItem.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            Collider col = currentItem.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            currentItem.transform.SetParent(handPoint);
            currentItem.transform.localPosition = Vector3.zero;

            // Resetear rotación local para que se acomode a la mano
            currentItem.transform.localRotation = Quaternion.identity;
        }
    }

    [PunRPC]
    public void RPC_DropItem()
    {
        if (currentItem == null) return;

        currentItem.transform.SetParent(null);

        Rigidbody rb = currentItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;

            // CAMBIO: Lanzar hacia donde mira el JUGADOR (transform.forward), no la cámara
            if (photonView.IsMine)
            {
                // Un poco hacia arriba (Vector3.up) y hacia adelante
                Vector3 throwForce = (transform.forward + Vector3.up * 0.5f).normalized * 5f;
                rb.AddForce(throwForce, ForceMode.Impulse);
            }
        }

        Collider col = currentItem.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        currentItem = null;
    }

#if UNITY_EDITOR
    // --- VISUALIZACIÓN EN EDITOR (GIZMOS) ---
    // Esto dibujará una esfera amarilla en el editor para que sepas cuan lejos llega tu brazo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
#endif
}