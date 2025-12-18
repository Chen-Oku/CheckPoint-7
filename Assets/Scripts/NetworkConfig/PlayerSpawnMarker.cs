using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

// Attach this to the player prefab (or to a component that runs when the player's avatar is ready).
// When the local player's object starts, it sets a custom player property indicating it's spawned/ready.
public class PlayerSpawnMarker : MonoBehaviourPun
{
    [Tooltip("Clave de propiedad de jugador que indica que el avatar ya se ha instanciado.")]
    public string playerSpawnedPropKey = "spawned";

    void Start()
    {
        // Solo el jugador local debe escribir su propia propiedad
        if (!PhotonNetwork.IsConnected) return;
        if (!photonView.IsMine) return;

        var props = new ExitGames.Client.Photon.Hashtable { { playerSpawnedPropKey, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"PlayerSpawnMarker: marcado jugador local (actor {PhotonNetwork.LocalPlayer.ActorNumber}) como '{playerSpawnedPropKey}=true'.");
    }
}
