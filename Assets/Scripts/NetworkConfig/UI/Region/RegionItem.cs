using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;

public class RegionItem : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI regionText;
    [SerializeField] private Button button;

    private string _regionCode;

    // Este método lo llama el RegionUIManager para configurar el botón
    public void Setup(Region region)
    {
        _regionCode = region.Code;

        // Formato: "EU", "US", etc.
        regionText.text = $"{_regionCode.ToUpper()}";

        // Limpiamos clicks anteriores y añadimos la llamada al Singleton
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickItem);
    }

    private void OnClickItem()
    {
        // Llamamos a la lógica que ya creaste para conectar
        ServerConnectionManager.Instance.ConnectToSpecificRegion(_regionCode);
    }
}