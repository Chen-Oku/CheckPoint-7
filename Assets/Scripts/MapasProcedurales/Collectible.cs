using Photon.Pun;
using UnityEngine;
using ExitGames.Client.Photon; // para Hashtable

[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviourPun
{
    [Tooltip("Clave de propiedad que se añadirá al jugador al recoger.")]
    public string playerPropertyKey = "HasTicket";

    [Tooltip("Audio opcional al recoger.")]
    public AudioClip pickupSound;

    bool pickedUp = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (pickedUp) return;
        if (!other.CompareTag("Player"))
        {
            Debug.Log($"Collectible: trigger collided with '{other.name}' but tag is not 'Player'.");
            return;
        }

        // obtener PhotonView del jugador (en padre o en el mismo)
        var pv = other.GetComponentInParent<PhotonView>();
        if (pv == null)
        {
            Debug.Log($"Collectible: no PhotonView encontrado en '{other.name}' o sus padres.");
            return;
        }
        if (pv == null) return;

        // solo el jugador local solicita recoger
        if (!pv.IsMine) return;

        // Prevención cliente: si el jugador local ya tiene la propiedad, no solicitar otro
        var localProps = PhotonNetwork.LocalPlayer.CustomProperties;
        if (localProps != null && localProps.ContainsKey(playerPropertyKey))
        {
            if (localProps[playerPropertyKey] is bool has && has)
            {
                Debug.Log($"Collectible: jugador local ya tiene '{playerPropertyKey}=true', no podrá recoger otro.");
                return;
            }
        }

        Debug.Log($"Collectible: jugador local (actor {pv.OwnerActorNr}) intentó recoger collectible '{gameObject.name}'. Enviando RPC a MasterClient.");

        // solicitar al MasterClient que valide y procese el pickup
        photonView.RPC(nameof(RPC_RequestPickup), RpcTarget.MasterClient, pv.OwnerActorNr);
    }

    [PunRPC]
    void RPC_RequestPickup(int actorNr, PhotonMessageInfo info)
    {
        // Solo el MasterClient procesa la petición de recogida
        if (!PhotonNetwork.IsMasterClient) return;
        if (pickedUp) return;

        // obtener jugador objetivo
        var target = PhotonNetwork.CurrentRoom?.GetPlayer(actorNr);
        if (target == null) return;

        // Comprobación autoritativa: si ya tiene la propiedad, rechazar
        var targetProps = target.CustomProperties;
        if (targetProps != null && targetProps.ContainsKey(playerPropertyKey))
        {
            if (targetProps[playerPropertyKey] is bool already && already)
            {
                Debug.Log($"Collectible (Master): actor {actorNr} ya tiene '{playerPropertyKey}=true', ignorando petición.");
                // opcional: avisar al cliente solicitante
                photonView.RPC(nameof(RPC_PickupDenied), RpcTarget.All, actorNr);
                return;
            }
        }

        // procesar pickup
        pickedUp = true;

        // asignar la propiedad al jugador objetivo
        var props = new ExitGames.Client.Photon.Hashtable { { playerPropertyKey, true } };
        target.SetCustomProperties(props);

        // notificar a todos que fue recogido (se puede enviar actorNr para quien la recogió)
        photonView.RPC(nameof(RPC_OnPickedUp), RpcTarget.AllBuffered, actorNr);

        // MasterClient destruye el objeto de red
        if (PhotonNetwork.IsMasterClient && photonView != null)
        {
            PhotonNetwork.Destroy(photonView);
        }
    }

    [PunRPC]
    void RPC_PickupDenied(int actorNr)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNr)
        {
            Debug.Log("Collectible: pickup denegado — ya tenías un collectible.");
        }
    }

    [PunRPC]
    void RPC_OnPickedUp(int actorNr)
    {
        // efecto local (sonido, partícula, desactivar visual)
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // desactivar collider/visual para evitar colisiones múltiples (los demás clientes verán la destrucción enviada por MasterClient)
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        var rnd = GetComponentInChildren<Renderer>();
        if (rnd != null) rnd.enabled = false;
    }
}