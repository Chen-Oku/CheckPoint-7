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
        if (!other.CompareTag("Player")) return;

        // obtener PhotonView del jugador (en padre o en el mismo)
        var pv = other.GetComponentInParent<PhotonView>();
        if (pv == null) return;

        // solo el jugador local (owner) solicita recoger
        if (!pv.IsMine) return;

        // solicitar al MasterClient que valide y procese el pickup
        photonView.RPC(nameof(RPC_RequestPickup), RpcTarget.MasterClient, pv.OwnerActorNr);
    }

    [PunRPC]
    void RPC_RequestPickup(int actorNr, PhotonMessageInfo info)
    {
        // Solo el MasterClient procesa la petición de recogida
        if (!PhotonNetwork.IsMasterClient) return;
        if (pickedUp) return;

        pickedUp = true;

        // asignar la propiedad al jugador objetivo
        var target = PhotonNetwork.CurrentRoom?.GetPlayer(actorNr);
        if (target != null)
        {
            var props = new Hashtable { { playerPropertyKey, true } };
            target.SetCustomProperties(props);
        }

        // notificar a todos que fue recogido (se puede enviar actorNr para quien la recogió)
        photonView.RPC(nameof(RPC_OnPickedUp), RpcTarget.AllBuffered, actorNr);

        // MasterClient destruye el objeto de red
        if (PhotonNetwork.IsMasterClient && photonView != null)
        {
            PhotonNetwork.Destroy(photonView);
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