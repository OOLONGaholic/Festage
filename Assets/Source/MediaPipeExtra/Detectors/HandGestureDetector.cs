using LandMarkProcess;
using UnityEngine;
using UnityEngine.UI;

public class HandGestureDetector : IDetector
{
    private readonly PersonState person;

    // ====== Output Flags ======
    public bool ThrowThisUpdate { get; private set; }
    public bool PullDownThisUpdate { get; private set; }
    public bool RotateThisUpdate { get; private set; }
    public bool TaikoHitLeftThisUpdate { get; private set; }
    public bool TaikoHitRightThisUpdate { get; private set; }
    private Image gestureImage;
    public Sprite feedbackSprite;
    private Vector3 smoothWristRight;
    private Vector3 smoothWristLeft;
    private float smoothFactor = 0.4f;


    // ====== 内部缓存 ======
    private Vector3? lastWristRight;
    private Vector3? lastWristLeft;

    private Quaternion lastRightRot;
    private Quaternion lastLeftRot;

    private float lastHitRightTime = -999;
    private float lastHitLeftTime = -999;

    public float MinHitIntervalMS = 250;

    private bool Started = false;

    public HandGestureDetector(PersonState p)
    {
        person = p;
        gestureImage = GameObject.Find("GestureImage")?.GetComponent<Image>();
    }

    public void Start()
    {
        Started = true;
    }
    private void ShowGestureSprite()
    {
        if (gestureImage != null && feedbackSprite != null)
        {
            gestureImage.sprite = feedbackSprite;
            gestureImage.color = Color.white;
        }
    }
    public void Update()
    {
        if (!Started) return;

        ThrowThisUpdate = false;
        PullDownThisUpdate = false;
        RotateThisUpdate = false;
        TaikoHitLeftThisUpdate = false;
        TaikoHitRightThisUpdate = false;

        if (person.Pose == null) return;

        // ❗ GetValidLandmarks 返回 Vector3?[]
        var LM = person.Pose.GetValidLandmarks();
        if (LM == null || LM.Length < 33) return;

        // 安全读取
        Vector3? wristR = LM[16];
        Vector3? wristL = LM[15];

        if (wristR.HasValue)
        {
            smoothWristRight = Vector3.Lerp(smoothWristRight, wristR.Value, smoothFactor);
        }
        if (wristL.HasValue)
        {
            smoothWristLeft = Vector3.Lerp(smoothWristLeft, wristL.Value, smoothFactor);
        }


        // ====== 右手动作 ======
        if (wristR.HasValue && lastWristRight.HasValue)
        {
            DetectThrowMotion(smoothWristRight);
            DetectPullDown(smoothWristRight);
        }

        // 旋转手腕需要 16,20,22 三个点
        DetectRotateMotion(smoothWristRight, LM);

        // ====== 太鼓达人 ======
        DetectTaikoHitLeft(smoothWristLeft);
        DetectTaikoHitRight(smoothWristRight);

        // 记录
        lastWristRight = smoothWristRight;
        lastWristLeft = smoothWristLeft;
    }

    // ============================================================
    // 1. 向前挥动（右手扔球）
    // ============================================================
    private void DetectThrowMotion(Vector3 wrist)
    {
        if (!lastWristRight.HasValue) return;

        Vector3 v = wrist - lastWristRight.Value; // 速度
        float speed = v.magnitude;

        // --- 新增：要求速度 > 阈值 ---
        if (speed < 0.02f) return;

        // --- 新增：方向性更强，必须与前方角度 < 35° ---
        float dot = Vector3.Dot(v.normalized, Vector3.forward);
        if (dot < 0.75f) return;

        // --- 新增：加速度判断，提高稳定性 ---
        static Vector3 lastVel = Vector3.zero;
        Vector3 accel = v - lastVel;
        lastVel = v;

        if (accel.z > 0.015f)
        {
            ThrowThisUpdate = true;
            Debug.LogWarning("右手扔球（增强检测）");
        }
    }

    // ============================================================
    // 2. 拉灯绳（右手自上而下）
    // ============================================================
    private void DetectPullDown(Vector3 wrist)
    {
        if (!lastWristRight.HasValue) return;

        Vector3 v = wrist - lastWristRight.Value;

        // --- 新增：下落速度阈值 ---
        if (v.y > -0.05f) return;

        // --- 新增：避免左右横扫动作误判 ---
        if (Mathf.Abs(v.x) > 0.05f) return;
        if (Mathf.Abs(v.z) > 0.05f) return;

        // --- 新增：必须整体向下趋势 ---
        if (v.normalized.y > -0.7f) return;

        PullDownThisUpdate = true;
        Debug.LogWarning("拉灯绳（增强检测）");
    }


    // ============================================================
    // 3. 手腕旋转
    // ============================================================
    private int rotateStableCount = 0;

    private void DetectRotateMotion(Vector3? wrist, Vector3?[] lm)
    {
        if (!wrist.HasValue || !lm[20].HasValue || !lm[22].HasValue) return;

        Vector3 v1 = lm[20].Value - wrist.Value;
        Vector3 v2 = lm[22].Value - wrist.Value;

        float angle = Vector3.SignedAngle(v1, v2, Vector3.forward);

        // --- 必须达到旋转角 ---
        if (Mathf.Abs(angle) < 25f)
        {
            rotateStableCount = 0;
            return;
        }

        // --- 连续多帧才算旋转 ---
        rotateStableCount++;

        if (rotateStableCount >= 3)
        {
            RotateThisUpdate = true;
            rotateStableCount = 0;
            Debug.LogWarning("手腕旋转（增强检测）");
            ShowGestureSprite();
        }
    }


    // ============================================================
    // 4. 左手敲击（太鼓达人）
    // ============================================================
    private void DetectTaikoHitLeft(Vector3? wrist)
    {
        if (!wrist.HasValue || !lastWristLeft.HasValue) return;

        Vector3 v = wrist.Value - lastWristLeft.Value;

        // --- 必须是快速向下 ---
        if (v.y > -0.10f) return;

        // --- 限制左右偏移 ---
        if (Mathf.Abs(v.x) > 0.05f) return;

        // --- 必须在冷却时间之后 ---
        if (Time.time * 1000 - lastHitLeftTime < MinHitIntervalMS) return;

        TaikoHitLeftThisUpdate = true;
        lastHitLeftTime = Time.time * 1000;
        Debug.LogWarning("左手敲击（增强检测）");
    }


    // ============================================================
    // 5. 右手敲击（太鼓达人）
    // ============================================================
    private void DetectTaikoHitRight(Vector3? wrist)
    {
        if (!wrist.HasValue || !lastWristRight.HasValue) return;

        Vector3 v = wrist.Value - lastWristRight.Value;

        if (v.y > -0.10f) return;
        if (Mathf.Abs(v.x) > 0.05f) return;

        if (Time.time * 1000 - lastHitRightTime < MinHitIntervalMS) return;

        TaikoHitRightThisUpdate = true;
        lastHitRightTime = Time.time * 1000;
        Debug.LogWarning("右手敲击（增强检测）");
    }

}
