using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class CameraPlayer : MonoBehaviour
{
    [SerializeField] PlayerStat stat;
    [SerializeField] CinemachinePanTilt panTilt;
    [SerializeField] CinemachineCamera vcam;
    [SerializeField] CinemachineRecomposer recomposer;
    [SerializeField] Transform body;
    [SerializeField] Transform head;
    [SerializeField] float speed = 3.2f;
    [SerializeField] float runSpeed = 5.6f;
    [SerializeField] float boost = 18f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float groundPull = -2f;
    [SerializeField] float runThreshold = 0.1f;
    [SerializeField] float sprintTime = 6f;
    [SerializeField] float restRate = 0.5f;
    [SerializeField] float restWait = 1f;
    [SerializeField] float restNeed = 1.5f;
    [SerializeField] float mouse = 0.12f;
    [SerializeField] float stick = 180f;
    [SerializeField] float minAngle = -80f;
    [SerializeField] float maxAngle = 80f;
    [SerializeField] bool invertY;
    [SerializeField] bool lockMouse = true;
    [SerializeField] float bobRate = 6f;
    [SerializeField] float bobUp = 0.030f;
    [SerializeField] float bobSide = 0.010f;
    [SerializeField] float runBob = 2.2f;
    [SerializeField] float bobSmooth = 14f;
    [SerializeField] float rollSize = 0.25f;
    [SerializeField] float runRoll = 4f;
    [SerializeField] float leanSize = 1.2f;
    [SerializeField] float rollSmooth = 7f;
    [SerializeField] float breathRate = 1.3f;
    [SerializeField] float breathSize = 0.011f;
    [SerializeField] float walkFov = 70f;
    [SerializeField] float runFov = 78f;
    [SerializeField] float fovSmooth = 4f;
    [SerializeField] float dipSize = 0.11f;
    [SerializeField] float dipSmooth = 9f;
    [SerializeField] float shakeFade = 2.2f;
    [SerializeField] float shakeSize = 0.085f;
    [SerializeField] float shakeRoll = 5f;
    [SerializeField] float shakeRate = 34f;
    [SerializeField] string woodTag = "Wood";
    [SerializeField] float footRay = 1.5f;

    [Header("Log Balance")]
    [SerializeField] float logSpeedMultiplier = 0.55f;
    [SerializeField] float logLeanSpeedMin = 4f;
    [SerializeField] float logLeanSpeedMax = 8f;
    [SerializeField] float logCorrectPower = 70f;
    [SerializeField] float logRecoverSpeed = 6f;
    [SerializeField] float logFallThreshold = 35f;
    [SerializeField] float logPushForce = 2.5f;
    [SerializeField] float logPushDecay = 6f;
    [SerializeField] float logPushCooldown = 0.6f;
    [SerializeField] float logKickOnPush = 0.35f;
    [SerializeField] float logDutchMult = 0.4f;
    [SerializeField] bool logInvertControls = false;

    InputManager input;
    CharacterController controller;
    Vector2 move;
    Vector2 look;
    Vector3 way;
    Vector3 velocity;
    Vector3 motion;
    Vector3 headStart;
    Vector3 bob;
    Vector3 goal;
    float fall;
    float topSpeed;
    float power;
    float turn;
    float pitch;
    float step;
    float mix;
    float size;
    float up;
    float side;
    float breath;
    float roll;
    float lean;
    float dip;
    float fov;
    float slow;
    float stamina;
    float shake;
    Vector3 jolt;
    float restLeft;
    float stepMark;
    bool wantsRun;
    bool running;
    bool tired;
    bool down;
    bool onGround;
    bool trapped;
    bool onWood;
    RaycastHit footHit;

    float logTilt;
    float logDir;
    float logLeanSpeed;
    float logPushTimer;
    Vector3 logPushVelocity;
    Vector3 logAxis;
    float logMoveSign;
    bool onLog;

    public float Speed => velocity.magnitude;
    public bool Running => running;
    public float Stamina => stamina;
    public bool Tired => tired;
    public bool Trapped => trapped;
    public bool OnLog => onLog;
    public float LogBalance01 => Mathf.Clamp01(Mathf.Abs(logTilt) / logFallThreshold);

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (stat == null) stat = GetComponent<PlayerStat>();
        if (body == null) body = transform;
        if (head != null) headStart = head.localPosition;

        fov = walkFov;
        stamina = sprintTime;

        if (panTilt == null)
        {
            Debug.LogError("CameraPlayer : aucun CinemachinePanTilt assigne.", this);
            return;
        }

        panTilt.PanAxis.Range = new Vector2(-180f, 180f);
        panTilt.PanAxis.Wrap = true;
        panTilt.TiltAxis.Range = new Vector2(minAngle, maxAngle);
        panTilt.TiltAxis.Wrap = false;
        panTilt.ReferenceFrame = CinemachinePanTilt.ReferenceFrames.World;
    }

    void OnEnable()
    {
        Cursor.lockState = lockMouse ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockMouse;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        input = InputManager.Instance;

        down = stat != null && (stat.Fainted || stat.Dead);
        move = input != null && !down && !trapped ? input.Move : Vector2.zero;

        Turn();
        Balance();
        Walk();
        Shake();
        Breathe();
    }

    public void SetTrapped(bool value)
    {
        trapped = value;
    }

    public void Kick(float amount)
    {
        shake = Mathf.Clamp01(shake + amount);
    }

    // axis = direction du rondin (sa longueur). Le joueur ne pourra plus se deplacer
    // que le long de cet axe une fois sur le tronc.
    public void EnterLog(Vector3 axis)
    {
        onLog = true;
        logTilt = 0f;
        logPushTimer = 0f;
        logDir = Random.value < 0.5f ? -1f : 1f;
        logLeanSpeed = Random.Range(logLeanSpeedMin, logLeanSpeedMax);

        axis.y = 0f;
        logAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : transform.forward;

        // on fige le sens "avancer" en fonction de l'orientation du joueur au moment ou il monte,
        // pour que le mouvement sur le rail ne depende plus de la camera ensuite
        float facing = Vector3.Dot(transform.forward, logAxis);
        logMoveSign = facing >= 0f ? 1f : -1f;
    }

    public void ExitLog()
    {
        onLog = false;
        logTilt = 0f;
    }

    void Turn()
    {
        if (down || input == null || panTilt == null) return;

        look = input.Look;
        power = input.MouseLook ? mouse : stick * Time.deltaTime;

        turn = look.x * power;
        pitch = (invertY ? look.y : -look.y) * power;

        panTilt.PanAxis.Value = panTilt.PanAxis.ClampValue(panTilt.PanAxis.Value + turn);
        panTilt.TiltAxis.Value = panTilt.TiltAxis.ClampValue(panTilt.TiltAxis.Value + pitch);

        body.rotation = Quaternion.Euler(0f, panTilt.PanAxis.Value, 0f);
    }

    void Balance()
    {
        logPushTimer -= Time.deltaTime;

        if (!onLog || down)
        {
            logTilt = Mathf.MoveTowards(logTilt, 0f, logRecoverSpeed * 2f * Time.deltaTime);
            return;
        }

        float prevSign = Mathf.Sign(logTilt);

        // le rondin penche tout seul en continu dans une direction
        logTilt += logDir * logLeanSpeed * Time.deltaTime;

        // le joueur corrige avec gauche/droite (Q/D) : ceci n'affecte QUE l'equilibre,
        // pas le deplacement physique (voir Walk, ou move.x n'est plus utilise sur le rondin)
        float correctInput = logInvertControls ? -move.x : move.x;
        logTilt += correctInput * logCorrectPower * Time.deltaTime;

        logTilt = Mathf.Clamp(logTilt, -(logFallThreshold + 15f), logFallThreshold + 15f);

        // si le joueur ramene le tilt de l'autre cote (ou proche de 0), le rondin repart
        // dans une nouvelle direction, avec une vitesse legerement differente
        if (Mathf.Sign(logTilt) != prevSign || Mathf.Abs(logTilt) < 1.5f)
        {
            logDir = Random.value < 0.5f ? -1f : 1f;
            logLeanSpeed = Random.Range(logLeanSpeedMin, logLeanSpeedMax);
        }

        if (Mathf.Abs(logTilt) >= logFallThreshold && logPushTimer <= 0f)
            PushFromLog();
    }

    void PushFromLog()
    {
        // sur le rondin, la propulsion se fait perpendiculairement a l'axe du tronc
        Vector3 side = Vector3.Cross(Vector3.up, logAxis);
        Vector3 pushDir = side * Mathf.Sign(logTilt);
        logPushVelocity += pushDir * logPushForce;

        // on redonne un peu de marge au joueur au lieu de le repousser en boucle
        logTilt = Mathf.Sign(logTilt) * (logFallThreshold * 0.3f);
        logPushTimer = logPushCooldown;

        Kick(logKickOnPush);
    }

    void Walk()
    {
        way = transform.right * move.x + transform.forward * move.y;

        if (way.sqrMagnitude > 1f) way.Normalize();

        // sur le rondin : le mouvement ne depend plus du tout de l'orientation de la camera.
        // W/S avancent/reculent le long du rail (axe fige a l'entree), Q/D ne bougent plus rien
        // physiquement, ils ne servent qu'a Balance() pour corriger le tilt.
        if (onLog)
            way = logAxis * (move.y * logMoveSign);

        wantsRun = input != null && !down && !trapped && !onLog && input.Run && move.y > runThreshold;
        running = wantsRun && !tired && stamina > 0f;

        if (running)
        {
            stamina -= Time.deltaTime;
            restLeft = restWait;

            if (stamina <= 0f)
            {
                stamina = 0f;
                tired = true;
            }
        }
        else
        {
            restLeft -= Time.deltaTime;

            if (restLeft <= 0f) stamina = Mathf.Min(sprintTime, stamina + restRate * Time.deltaTime);
            if (tired && stamina >= restNeed) tired = false;
        }

        slow = stat != null ? stat.Slow : 1f;
        if (onLog) slow *= logSpeedMultiplier;

        topSpeed = (running ? runSpeed : speed) * slow;

        velocity = Vector3.MoveTowards(velocity, way * topSpeed, boost * Time.deltaTime);

        if (controller.isGrounded && fall < 0f)
            fall = groundPull;
        else
            fall += gravity * Time.deltaTime;

        motion = velocity + Vector3.up * fall + logPushVelocity;
        controller.Move(motion * Time.deltaTime);

        logPushVelocity = Vector3.MoveTowards(logPushVelocity, Vector3.zero, logPushDecay * Time.deltaTime);
    }

    void Shake()
    {
        shake = Mathf.MoveTowards(shake, 0f, shakeFade * Time.deltaTime);

        jolt.x = (Mathf.PerlinNoise(Time.time * shakeRate, 0f) - 0.5f) * 2f * shakeSize * shake;
        jolt.y = (Mathf.PerlinNoise(0f, Time.time * shakeRate) - 0.5f) * 2f * shakeSize * shake;

        if (controller.isGrounded && !onGround) dip = dipSize;

        onGround = controller.isGrounded;
        dip = Mathf.Lerp(dip, 0f, dipSmooth * Time.deltaTime);

        mix = Mathf.Clamp01(Speed / speed);
        size = running ? runBob : 1f;
        step += Speed * Time.deltaTime * bobRate / Mathf.Max(speed, 0.01f);

        if (step > Mathf.PI * 2f)
        {
            step -= Mathf.PI * 2f;
            stepMark -= Mathf.PI * 2f;
        }

        if (onGround && mix > 0.05f && step - stepMark >= Mathf.PI)
        {
            stepMark += Mathf.PI;
            Footstep();
        }

        up = Mathf.Sin(step * 2f) * bobUp * size * mix;
        side = Mathf.Cos(step) * bobSide * size * mix;
        breath = Mathf.Sin(Time.time * breathRate) * breathSize * (1f - mix * 0.7f);

        if (head != null)
        {
            goal = new Vector3(side, up + breath - dip, 0f);
            bob = Vector3.Lerp(bob, goal, bobSmooth * Time.deltaTime);
            head.localPosition = headStart + bob + jolt;
        }

        if (recomposer != null)
        {
            lean = -move.x * leanSize;
            roll = Mathf.Lerp(roll, Mathf.Cos(step) * rollSize * (running ? runRoll : 1f) * mix + lean,
                rollSmooth * Time.deltaTime);
            recomposer.Dutch = roll + logTilt * logDutchMult
                + (Mathf.PerlinNoise(Time.time * shakeRate, 5f) - 0.5f) * 2f * shakeRoll * shake;
        }

        if (vcam != null)
        {
            fov = Mathf.Lerp(fov, running ? runFov : walkFov, fovSmooth * Time.deltaTime);
            vcam.Lens.FieldOfView = fov;
        }
    }

    void Footstep()
    {
        if (SoundManager.Instance == null) return;

        onWood = Physics.Raycast(transform.position, Vector3.down, out footHit, footRay, ~0, QueryTriggerInteraction.Ignore) && footHit.collider.CompareTag(woodTag);

        SoundManager.Instance.PlayFootstep(transform.position, onWood);
    }

    void Breathe()
    {
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.SetBreathing(tired && !down);
    }
}