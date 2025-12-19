using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Photon.Pun;
using ExitGames.Client.Photon;

public class GoalTrigger : MonoBehaviour
{
    [Tooltip("Nombre de la escena a cargar (si quieres cargar localmente).")]
    public string sceneToLoad = "Level2";

    [Tooltip("Clave de propiedad que indica si el jugador tiene el ticket.")]
    public string requiredPropertyKey = "HasTicket";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var pv = other.GetComponentInParent<PhotonView>();
        if (pv == null) return;

        // solo el jugador local (dueño) procesa su intento de entrar al goal
        if (!pv.IsMine) return;

        // comprobar propiedad sincronizada
        bool hasTicket = false;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(requiredPropertyKey, out object val))
        {
            if (val is bool b) hasTicket = b;
        }

        if (!hasTicket)
        {
            Debug.Log("No tienes el ticket. No puedes entrar al goal.");
            return;
        }

        Debug.Log("¡Nivel completado! Jugador con ticket entró al goal.");

        // efecto local (cambiar color del trigger)
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.material.color = Color.green;

        // Notificamos al MasterClient que alguien llegó al goal usando propiedades de sala.
        // El MasterClient será responsable de cargar la escena para todos con PhotonNetwork.LoadLevel.
        if (PhotonNetwork.CurrentRoom != null)
        {
            var props = new ExitGames.Client.Photon.Hashtable { { "gameFinishedBy", PhotonNetwork.LocalPlayer.ActorNumber } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    IEnumerator NextLevel()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(sceneToLoad);
    }
}
