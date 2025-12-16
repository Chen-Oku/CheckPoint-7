using System.Collections.Generic;
using Photon.Realtime;
using UnityEngine;

public class RegionUIManager : MonoBehaviour
{
    [Header("Configuración")]
    public RegionItem regionButtonPrefab;
    public Transform contentParent;

    private void Start()
    {
        // 1. Suscripción a la lista de regiones
        ServerConnectionManager.Instance.OnRegionsUpdate += UpdateRegionList;

        // 2. Suscripción al evento de ocultar
        ServerConnectionManager.Instance.OnStartRegionConnection += HideRegionPanel;
    }

    private void OnDestroy()
    {
        if (ServerConnectionManager.Instance != null)
        {
            ServerConnectionManager.Instance.OnRegionsUpdate -= UpdateRegionList;

            // IMPORTANTE: Desuscribirse también para evitar errores
            ServerConnectionManager.Instance.OnStartRegionConnection -= HideRegionPanel;
        }
    }

    private void UpdateRegionList(List<Region> regions)
    {
        // Aseguramos que el panel sea visible por si se ocultó antes
        contentParent.gameObject.SetActive(true);

        Debug.Log($"UI: Actualizando lista visual... Total regiones: {regions.Count}");

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Region region in regions)
        {
            Debug.Log($"Procesando región: {region.Code} | Ping: {region.Ping}");
            RegionItem newItem = Instantiate(regionButtonPrefab, contentParent);
            newItem.Setup(region);
        }
    }

    // Ahora sí se llamará cuando ocurra el evento
    private void HideRegionPanel()
    {
        Debug.Log("UI: Región seleccionada, ocultando panel de selección.");
        contentParent.gameObject.SetActive(false);
    }
}