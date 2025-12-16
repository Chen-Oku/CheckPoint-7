using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Distancia y altura relativa al jugador")]
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -8);

    [Tooltip("Suavizado del movimiento de la cámara")]
    [SerializeField] private float smoothSpeed = 5f;

    private Transform target; // A quién seguimos

    private void OnEnable()
    {
        // NOS SUSCRIBIMOS
        // Le decimos: "Cuando el PlayerController lance el evento, ejecuta mi método 'SetTarget'"
        PlayerController.OnLocalPlayerReady += SetTarget;
    }

    private void OnDisable()
    {
        // NOS DESUSCRIBIMOS (Siempre, buena práctica para evitar memory leaks)
        PlayerController.OnLocalPlayerReady -= SetTarget;
    }

    // Este método recibe el Transform que envió el evento
    private void SetTarget(Transform localPlayerTransform)
    {
        Debug.Log("Cámara: ¡Jugador local detectado! Iniciando seguimiento.");
        target = localPlayerTransform;

        // Opcional: Teletransportar la cámara de inmediato a la posición inicial
        transform.position = target.position + offset;

        // Asegurar que la cámara mire al jugador
        transform.LookAt(target.position);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Calculamos dónde debería estar la cámara (Posición del jugador + Offset fijo)
        // Al sumar el offset fijo, ignoramos la rotación del jugador.
        Vector3 desiredPosition = target.position + offset;

        // 2. Nos movemos suavemente hacia esa posición (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 3. Aplicamos la posición
        transform.position = smoothedPosition;
    }
}