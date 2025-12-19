using Photon.Pun;
using UnityEngine;
using ExitGames.Client.Photon;

public class PlayerColorAssigner : MonoBehaviourPun
{
    [Header("Configurable palette (set in prefab)")]
    [SerializeField]
    private Color[] palette;

    [Header("Player custom property key")]
    [SerializeField]
    private string colorIndexKey = "colorIndex";

    private void Awake()
    {
        if (palette == null || palette.Length == 0)
        {
            palette = new Color[] {
                Color.red,
                Color.blue,
                Color.green,
                Color.yellow,
                Color.magenta,
                Color.cyan,
                new Color(1f, 0.5f, 0f), // orange
                Color.gray
            };
        }
    }

    private void Start()
    {
        if (photonView == null) return;

        if (photonView.IsMine)
        {
            // If player already has colorIndex in custom props, apply it
            if (PhotonNetwork.LocalPlayer.CustomProperties != null && PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(colorIndexKey, out object val) && val is int idx)
            {
                photonView.RPC(nameof(RPC_ApplyColorIndex), RpcTarget.AllBuffered, photonView.ViewID, idx);
            }
            else
            {
                // Ask MasterClient to assign a color
                photonView.RPC(nameof(RPC_RequestColor), RpcTarget.MasterClient, photonView.ViewID, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
    }

    // MasterClient handles assignment requests
    [PunRPC]
    public void RPC_RequestColor(int targetViewId, int actorNr, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var used = new System.Collections.Generic.HashSet<int>();
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties != null && p.CustomProperties.TryGetValue(colorIndexKey, out object o) && o is int ix)
                used.Add(ix);
        }

        int chosen = -1;
        for (int i = 0; i < palette.Length; i++)
        {
            if (!used.Contains(i)) { chosen = i; break; }
        }
        if (chosen == -1) chosen = Random.Range(0, palette.Length);

        // Assign to player custom properties (authoritative)
        var target = PhotonNetwork.CurrentRoom?.GetPlayer(actorNr);
        if (target != null)
        {
            var props = new Hashtable { { colorIndexKey, chosen } };
            target.SetCustomProperties(props);
        }

        // Apply color via RPC on the target's view (AllBuffered so late joiners keep it)
        var pv = PhotonView.Find(targetViewId);
        if (pv != null)
        {
            pv.RPC(nameof(RPC_ApplyColorIndex), RpcTarget.AllBuffered, targetViewId, chosen);
        }
    }

    [PunRPC]
    public void RPC_ApplyColorIndex(int targetViewId, int index)
    {
        var pv = PhotonView.Find(targetViewId);
        if (pv == null || pv.gameObject == null) return;

        Color color = palette[Mathf.Clamp(index, 0, palette.Length - 1)];

        var renderers = pv.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mat = r.material;
            // Try common color property
            if (mat.HasProperty("_Color")) mat.color = color;
            else mat.color = color;
        }
    }
}
