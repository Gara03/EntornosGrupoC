using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerController : CharController
{
    private PlayerControls controls;
    private bool isReadyForMultiplayer = false;

    // Variable Network
    private NetworkVariable<int> selectedCharacterIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    protected int damageToEnemy;
    protected float attackCooldown;

    public bool IsAttacking { get; private set; } = false;
    public int DamageToEnemy => damageToEnemy;

    [Header("Character Colors")]
    [SerializeField] private PlayerStats[] allCharacters;

    /// <summary>
    /// Inicializa controles de entrada y registra el jugador local en el gestor global.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        if (TryGetComponent(out SpriteRenderer sr)) sr.enabled = false;
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }
    }

    /// <summary>
    /// [NUEVO] Se ejecuta en cuanto el jugador "nace" en la red.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            controls = new PlayerControls();
            controls.Player.Move.performed += ctx => movement = ctx.ReadValue<Vector2>();
            controls.Player.Move.canceled += _ => movement = Vector2.zero;
            controls.Player.Attack.performed += onAttack;
            controls.Enable();

            UniqueEntity uniqueEntity = GetComponent<UniqueEntity>();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterLocalPlayer(this, uniqueEntity);
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

    /// <summary>
    /// Desconecta al jugador de la red.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        if (IsOwner && controls != null)
        {
            controls.Player.Attack.performed -= onAttack;
            controls.Disable();
        }
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Aplica el personaje seleccionado por el jugador.
    /// </summary>
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

        if (TryGetComponent(out SpriteRenderer sr)) sr.enabled = true;

        Invoke(nameof(EnablePhysics), 0.5f);
    }


    /// <summary>
    /// Activa el collider del personaje.
    /// </summary>
    private void EnablePhysics()
    {
        isReadyForMultiplayer = true;
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders) col.enabled = true;
    }

    /// <summary>
    /// Busca el personaje a seleccionar.
    /// </summary>
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
        if (newStats == null) return;
        stats = newStats;
        LoadStats();
    }

    /// <summary>
    /// Carga estadísticas del personaje seleccionado y aplica valores de combate y movimiento.
    /// </summary>
    protected override void LoadStats()
    {
        // 1. SOLO el dueño lee del GameManager local. El servidor respeta lo que le llega por red.
        if (IsOwner && GameManager.Instance != null && GameManager.Instance.SelectedCharacterStats != null)
        {
            stats = GameManager.Instance.SelectedCharacterStats;
        }

        // 2. Cargamos la vida y velocidad base (heredadas)
        base.LoadStats();

        // 3. Cargamos el daño y cooldown
        PlayerStats playerStats = stats as PlayerStats;
        if (playerStats != null)
        {
            moveSpeed *= playerStats.speedBonus;
            damageToEnemy = playerStats.attackDamage;
            attackCooldown = playerStats.attackCooldown;
        }
        else
        {
            damageToEnemy = 50;
            attackCooldown = 0.5f;
            moveSpeed *= 1.25f;
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

        NotifyAttackServerRpc();
    }

    /// <summary>
    /// Finaliza el estado de ataque del jugador.
    /// </summary>
    private void endAttack()
    {
        IsAttacking = false;
    }


    /// <summary>
    /// Todos los clientes y el Host reciben el aviso de que el jugador ha atacado.
    /// </summary>
    [Rpc(SendTo.Everyone)]
    private void PlayAttackAnimationRpc()
    {
        if (IsOwner) return;

        animator.SetTrigger("Attack");
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
    [Rpc(SendTo.Owner)]
    public void ReceiveDamageRpc(int amount, Vector2 knockbackDir)
    {
        Debug.Log($"[Cliente] ¡Me han pegado! Recibo {amount} de daño del servidor.");

        TakeDamage(amount, knockbackDir);
    }

    [Rpc(SendTo.Server)]
    private void NotifyAttackServerRpc()
    {
        // ESTO ES VITAL: El servidor registra que estás atacando
        IsAttacking = true;
        Invoke(nameof(endAttack), attackCooldown);

        PlayAttackAnimationRpc();
    }
}
