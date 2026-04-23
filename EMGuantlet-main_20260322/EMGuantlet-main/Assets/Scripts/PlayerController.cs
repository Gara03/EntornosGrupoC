using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerController : CharController
{
    protected int damageToEnemy;
    protected float attackCooldown;

    private PlayerControls controls;

    public bool IsAttacking { get; private set; } = false;
    public int DamageToEnemy => damageToEnemy;

    [Header("Multiplayer Visuals")]
    [SerializeField] private PlayerStats[] allCharacters;

    private NetworkVariable<int> selectedCharacterIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private bool isReadyForMultiplayer = false;

    /// <summary>
    /// Inicializa controles de entrada y registra el jugador local en el gestor global.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        controls = new PlayerControls();
        controls.Player.Move.performed += ctx => movement = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += _ => movement = Vector2.zero;

        // ✅ Ocultar hasta que LevelGenerator lo reposicione
        //gameObject.SetActive(false);
        if (TryGetComponent(out SpriteRenderer sr)) sr.enabled = false;
        if (TryGetComponent(out Collider2D col)) col.enabled = false;
    }

    /// <summary>
    /// [NUEVO] Se ejecuta en cuanto el jugador "nace" en la red.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.SetParent(this.transform);
                mainCam.transform.localPosition = new Vector3(0f, 0f, -10f);
            }

            int index = FindCharacterIndex(GameManager.Instance.SelectedCharacterStats);
            selectedCharacterIndex.Value = index;
        }

        selectedCharacterIndex.OnValueChanged += (oldVal, newVal) => ApplyVisuals(newVal);

        if (selectedCharacterIndex.Value != -1)
        {
            ApplyVisuals(selectedCharacterIndex.Value);
        }
    }

    private void ApplyVisuals(int index)
    {
        if (index < 0 || index >= allCharacters.Length) return;
        PlayerStats selectedStats = allCharacters[index];

        if (animator != null && selectedStats.animatorController != null)
        {
            animator.runtimeAnimatorController = selectedStats.animatorController;
            animator.Update(0);
        }

        this.stats = selectedStats;
        LoadStats();

        isReadyForMultiplayer = true;
        if (TryGetComponent(out SpriteRenderer sr)) sr.enabled = true;
        if (TryGetComponent(out Collider2D col)) col.enabled = true;
    }

    private int FindCharacterIndex(PlayerStats targetStats)
    {
        if (allCharacters == null) return 0;
        for (int i = 0; i < allCharacters.Length; i++)
            if (allCharacters[i] == targetStats) return i;
        return 0;
    }

    /// <summary>
    /// Inicializa estado del jugador y notifica los valores iniciales al HUD.
    /// </summary>
    protected override void Start()
    {
        base.Start();
        if (!IsOwner) return;

        // Dispara eventos iniciales para actualizar el HUD
        GameEvents.HealthChanged(health);
        GameEvents.KeysChanged();
        GameEvents.DiamondsChanged();

        IsAttacking = false;
    }

    /// <summary>
    /// Actualiza animación, orientación y estado de vida en cada frame.
    /// </summary>
    protected override void Update()
    {
        if (!IsOwner || !isReadyForMultiplayer) return;

        animator.SetFloat("speed", movement.sqrMagnitude);

        if (movement.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        checkDeath();
    }

    /// <summary>
    /// Activa el mapa de controles y suscribe la acción de ataque.
    /// </summary>
    private void OnEnable()
    {
        if (controls == null) controls = new PlayerControls();
        if (!IsOwner) return;

        controls.Enable();
        controls.Player.Attack.performed += onAttack;
    }

    /// <summary>
    /// Desuscribe la acción de ataque y desactiva el mapa de controles.
    /// </summary>
    private void OnDisable()
    {
        if (!IsOwner || controls == null) return;

        controls.Player.Attack.performed -= onAttack;
        controls.Disable();
    }

    /// <summary>
    /// Gestiona la muerte del jugador y lanza el flujo de fin de partida.
    /// </summary>
    public override void Die()
    {
        if (!IsOwner) return;

        base.Die();

        // Dispara evento de muerte
        GameEvents.PlayerDied();

        GameManager.Instance?.TriggerGameOver();

    }

    /// <summary>
    /// Aplica daño al jugador y notifica el cambio de salud al HUD.
    /// </summary>
    public override void TakeDamage(int amount, Vector2 knockbackDir)
    {
        if (!IsOwner || !isReadyForMultiplayer) return;

        base.TakeDamage(amount, knockbackDir);

        // Dispara evento de cambio de salud
        GameEvents.HealthChanged(health);
    }

    /// <summary>
    /// Aplica un conjunto de estadísticas de personaje y recarga sus valores activos.
    /// </summary>
    public void ApplyCharacterStats(PlayerStats newStats)
    {
        if (newStats == null)
        {
            Debug.LogWarning("[PlayerController] ApplyCharacterStats llamado con null");
            return;
        }

        stats = newStats;

        isReadyForMultiplayer = true;
        if (TryGetComponent(out SpriteRenderer sr)) sr.enabled = true;
        if (TryGetComponent(out Collider2D col)) col.enabled = true;

        // Recargar todas las stats
        LoadStats();

        Debug.Log($"[PlayerController] Stats aplicadas: {newStats.characterName}");
    }

    /// <summary>
    /// Carga estadísticas del personaje seleccionado y aplica valores de combate y movimiento.
    /// </summary>
    protected override void LoadStats()
    {
        if (!IsOwner) return;

        // ✅ PRIMERO: Intenta cargar desde GameManager (personaje seleccionado)
        if (GameManager.Instance != null && GameManager.Instance.SelectedCharacterStats != null)
        {
            stats = GameManager.Instance.SelectedCharacterStats;
            Debug.Log($"[PlayerController] Cargando personaje seleccionado: {stats.characterName}");
        }

        // Si no hay personaje seleccionado, usa el asignado en el prefab (fallback)
        if (stats == null)
        {
            Debug.LogWarning("[PlayerController] No hay personaje seleccionado, usando stats por defecto del prefab");
        }

        base.LoadStats();

        // ✅ Haz casting del campo heredado
        PlayerStats playerStats = stats as PlayerStats;

        if (playerStats != null)
        {
            // Aplica el bonus de velocidad del jugador
            moveSpeed *= playerStats.speedBonus;
            
            // Carga stats específicas del jugador
            damageToEnemy = playerStats.attackDamage;
            attackCooldown = playerStats.attackCooldown;
        }
        else
        {
            // Valores por defecto si no hay PlayerStats
            Debug.LogWarning($"[{gameObject.name}] No tiene PlayerStats asignado. Usando valores por defecto.");
            damageToEnemy = 50;
            attackCooldown = 0.5f;
            moveSpeed *= 1.25f; // Bonus por defecto
        }
    }

    /// <summary>
    /// Verifica si la salud ha llegado a cero y ejecuta la muerte una sola vez.
    /// </summary>
    private void checkDeath()
    {
        if (!IsOwner || !isReadyForMultiplayer) return;

        if (health <= 0 && !isDead)
        {
            Die();
        }
    }

    /// <summary>
    /// Inicia la animación de ataque y programa su final según el cooldown.
    /// </summary>
    private void onAttack(InputAction.CallbackContext context)
    {
        if (!IsOwner || !isReadyForMultiplayer) return;

        animator.SetTrigger("Attack");
        IsAttacking = true;
        Invoke(nameof(endAttack), attackCooldown);
    }

    /// <summary>
    /// Finaliza el estado de ataque del jugador.
    /// </summary>
    private void endAttack()
    {
        IsAttacking = false;
    }
}
