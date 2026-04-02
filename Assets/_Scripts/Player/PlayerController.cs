using UnityEngine;
/// <summary>
/// Управляет перемещением игрока в 3D (вид от третьего лица).
/// Использует InputManager для получения ввода.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Компонент статистики игрока (здоровье, скорость и т.д.).")]
    public PlayerStats playerStats;
    [Tooltip("Трансформ камеры, относительно которой считается движение. Обычно main camera.")]
    public Transform cameraTransform;
    [Tooltip("Корневой трансформ визуальной модели (вращается в сторону камеры).")]
    public Transform visualRoot;

    [Header("Движение и физика")]
    [Tooltip("Гравитация (отрицательное значение).")]
    public float gravity = -9.81f;
    [Tooltip("Небольшая отрицательная скорость, чтобы прижимать игрока к земле.")]
    public float groundedGravity = -2f;
    [Tooltip("Множитель скорости при спринте.")]
    public float sprintMultiplier = 1.5f;

    private CharacterController characterController;
    private Vector3 verticalVelocity;
    private bool isGrounded;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (InputManager.Instance == null)
            return;
        HandleMovement();
        HandleJump();
        InputManager.Instance.ResetButtonFlags();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = InputManager.Instance.MoveInput;
        Vector3 moveDirection = Vector3.zero;
        // Движение относительно камеры:
        // W/S – вперёд/назад по камере, A/D – влево/вправо по камере.
        if (moveInput.sqrMagnitude > 0.001f && cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
            moveDirection = forward * moveInput.y + right * moveInput.x;
        }
        moveDirection.Normalize();

        // Скорость и скорость поворота берём из PlayerData (через PlayerStats), если возможно.
        float speed = 5f;
        float rotationSpeed = 720f;
        if (playerStats != null && playerStats.playerData != null)
        {
            speed = playerStats.playerData.moveSpeed;
            rotationSpeed = playerStats.playerData.rotationSpeed;
        }
        if (InputManager.Instance.IsSprintHeld())
        {
            speed *= sprintMultiplier;
        }
        Vector3 horizontalVelocity = moveDirection * speed;

        // Проверка на землю
        isGrounded = characterController.isGrounded;
        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = groundedGravity;
        }
        // Гравитация
        verticalVelocity.y += gravity * Time.deltaTime;
        Vector3 velocity = horizontalVelocity + verticalVelocity;

        // Перемещаем игрока
        characterController.Move(velocity * Time.deltaTime);

        // ПОВОРОТ ИГРОКА (стрейфовый стиль):
        // - визуальная модель всегда смотрит туда же, куда и камера;
        // - направление взгляда зависит только от камеры, а не от W/S.
        if (cameraTransform != null && visualRoot != null)
        {
            // Берём направление вперёд камеры по плоскости XZ
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();
            if (cameraForward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
                visualRoot.rotation = Quaternion.Slerp(
                    visualRoot.rotation,
                    targetRotation,
                    rotationSpeed * Mathf.Deg2Rad * Time.deltaTime
                );
            }
        }
    }

    private void HandleJump()
    {
        if (!isGrounded)
            return;
        if (InputManager.Instance.IsJumpPressed())
        {
            float jumpForce = 5f;
            if (playerStats != null && playerStats.playerData != null)
                jumpForce = playerStats.playerData.jumpForce;
            verticalVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
    }
}
