using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviourPun
{
    public static event Action<Transform> OnLocalPlayerReady;

    [Header("Ajustes")]
    public float speed = 5f;
    public float turnSpeed = 200f;

    private PlayerInput playerInput;
    private bool isMyCharacter; // Variable para almacenar el resultado de IsMine

    void Start()
    {
        // 1. Capturar el estado UNA VEZ en Start (es el estado correcto al instanciar)
        isMyCharacter = photonView.IsMine;

        // 2. Depuración: Ver quién soy y quién es mi dueño.
        Debug.Log($"[PlayerController] InstanceID: {gameObject.GetInstanceID()} - Soy mío: {isMyCharacter} - Dueño: {photonView.Owner.NickName}");

        if (!isMyCharacter)
        {
            // SI NO SOY MÍO: Desactivar Input

            // Si el objeto tiene un PlayerInput, lo desactivamos para que no reciba comandos
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            playerInput.enabled = false;

            // Ya que no es mío, no necesitamos seguir ejecutando el resto del Start()
            return;
        }

        // --- LÓGICA SOLO PARA MI PERSONAJE ---

        // 3. Activación de Input y Camera/Audio (si es mío)
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        playerInput.enabled = true; // Asegurarse de que el input está activo

        // GRITAMOS AL MUNDO: "¡Soy el jugador local y este es mi transform!"
        // La cámara escuchará esto.
        OnLocalPlayerReady?.Invoke(transform);
    }

    void Update()
    {
        // El control de si es mío se hace con la variable almacenada UNA VEZ.
        if (!isMyCharacter) return;

        // Aseguramos que el input esté disponible antes de leer
        if (playerInput != null && playerInput.actions["Move"] != null)
        {
            Vector2 input = playerInput.actions["Move"].ReadValue<Vector2>();

            // Si hay input (nos estamos moviendo)
            if (input.sqrMagnitude > 0.01f)
            {
                // 1. CALCULAR DIRECCIÓN (En el plano X, Z)
                Vector3 direction = new Vector3(input.x, 0, input.y).normalized;

                // 2. MOVER EN ESPACIO MUNDIAL (Space.World)
                // Esto hace que "Arriba" en el stick siempre sea "Norte" en el juego
                transform.Translate(direction * speed * Time.deltaTime, Space.World);

                // 3. ROTACIÓN SUAVE HACIA LA DIRECCIÓN
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
            }
        }
    }
}