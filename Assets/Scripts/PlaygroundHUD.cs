using UnityEngine;

public class PlaygroundHUD : MonoBehaviour
{
    public PlayerController player;

    private float goalTimer = 0f;
    private string lastGoalText = "";
    private int goalCount = 0;

    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle badgeStyle;
    private GUIStyle helpStyle;
    private GUIStyle goalBannerStyle;
    private Texture2D panelBackground;
    private Texture2D badgeBackground;
    private Texture2D goalBannerBackground;

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        InitStyles();
    }

    void Update()
    {
        if (goalTimer > 0)
        {
            goalTimer -= Time.deltaTime;
        }
    }

    public void ShowGoalNotification(string goalName)
    {
        goalCount++;
        lastGoalText = $"⚽ GOAL！進球得分！ ({goalName}) ⚽\n累計進球: {goalCount}";
        goalTimer = 3.5f;
    }

    private void InitStyles()
    {
        panelBackground = MakeColorTexture(new Color(0.08f, 0.1f, 0.14f, 0.85f));
        badgeBackground = MakeColorTexture(new Color(0.15f, 0.55f, 0.35f, 0.9f));
        goalBannerBackground = MakeColorTexture(new Color(0.85f, 0.2f, 0.15f, 0.95f));

        headerStyle = new GUIStyle();
        headerStyle.fontSize = 18;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle();
        labelStyle.fontSize = 14;
        labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.95f);

        badgeStyle = new GUIStyle();
        badgeStyle.fontSize = 13;
        badgeStyle.fontStyle = FontStyle.Bold;
        badgeStyle.alignment = TextAnchor.MiddleCenter;
        badgeStyle.normal.textColor = Color.white;
        badgeStyle.normal.background = badgeBackground;

        helpStyle = new GUIStyle();
        helpStyle.fontSize = 12;
        helpStyle.normal.textColor = new Color(0.75f, 0.85f, 0.95f);

        goalBannerStyle = new GUIStyle();
        goalBannerStyle.fontSize = 26;
        goalBannerStyle.fontStyle = FontStyle.Bold;
        goalBannerStyle.alignment = TextAnchor.MiddleCenter;
        goalBannerStyle.normal.textColor = Color.yellow;
        goalBannerStyle.normal.background = goalBannerBackground;
    }

    private Texture2D MakeColorTexture(Color col)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if (panelBackground == null)
            InitStyles();

        // 1. Top Left Panel: Scene Title and Player Status
        float panelW = 340;
        float panelH = 160;
        Rect panelRect = new Rect(20, 20, panelW, panelH);
        GUI.DrawTexture(panelRect, panelBackground);

        GUILayout.BeginArea(new Rect(panelRect.x + 15, panelRect.y + 12, panelW - 30, panelH - 24));
        GUILayout.Label("🏫 校園運動場與綠草地足球場", headerStyle);
        GUILayout.Space(6);

        string currentZone = GetPlayerZone();
        GUILayout.Label($"📍 所在位置: {currentZone}", labelStyle);

        if (player != null)
        {
            string stateStr = "靜止 (Idle)";
            if (!player.IsGrounded)
            {
                stateStr = "跳躍滯空中 (Jumping)";
            }
            else if (player.IsSprinting)
            {
                stateStr = "高速奔跑 (Sprinting)";
            }
            else if (player.CurrentSpeed > 0.2f)
            {
                stateStr = "漫步走動 (Walking)";
            }

            GUILayout.Label($"🏃 移動狀態: {stateStr}", labelStyle);
            GUILayout.Label($"⚡ 移動速度: {player.CurrentSpeed:F1} m/s", labelStyle);
        }
        GUILayout.EndArea();

        // 2. Bottom Left Panel: Controls Guide
        float helpW = 360;
        float helpH = 145;
        Rect helpRect = new Rect(20, Screen.height - helpH - 20, helpW, helpH);
        GUI.DrawTexture(helpRect, panelBackground);

        GUILayout.BeginArea(new Rect(helpRect.x + 15, helpRect.y + 10, helpW - 30, helpH - 20));
        GUILayout.Label("🎮 操作指南 (Controls)", headerStyle);
        GUILayout.Space(4);
        GUILayout.Label("• [W][A][S][D] - 自由走動 / 探索操場", helpStyle);
        GUILayout.Label("• [Left Shift] - 按住衝刺高速奔跑", helpStyle);
        GUILayout.Label("• [Space 空白鍵] - 起跳騰空 (Jump)", helpStyle);
        GUILayout.Label("• [滑鼠移動] - 360° 旋轉視角 / [滾輪] 視角遠近縮放", helpStyle);
        GUILayout.Label("• [R 鍵] - 角色重設回中央 / [ESC] 釋放滑鼠指標", helpStyle);
        GUILayout.Label("• 提示: 走向足球即可進行踢球與帶球！", helpStyle);
        GUILayout.EndArea();

        // 3. Goal Notification Banner
        if (goalTimer > 0)
        {
            float bannerW = 480;
            float bannerH = 80;
            Rect bannerRect = new Rect((Screen.width - bannerW) * 0.5f, 40, bannerW, bannerH);
            GUI.Box(bannerRect, lastGoalText, goalBannerStyle);
        }
    }

    private string GetPlayerZone()
    {
        if (player == null) return "操場園區";
        Vector3 pos = player.transform.position;

        // Soccer field: |X| <= 32 and |Z| <= 50
        if (Mathf.Abs(pos.x) <= 32f && Mathf.Abs(pos.z) <= 50f)
        {
            return "⚽ 綠草地足球場 (Soccer Pitch)";
        }
        // Track: approx |X| between 32 and 42 or curves
        float r = Mathf.Sqrt(pos.x * pos.x + Mathf.Pow(Mathf.Max(0, Mathf.Abs(pos.z) - 50f), 2));
        if (r >= 32f && r <= 42f)
        {
            int lane = Mathf.Clamp(Mathf.FloorToInt((r - 32f) / 1.6f) + 1, 1, 6);
            return $"🏃 400米跑道 (第 {lane} 跑道)";
        }
        if (pos.x > 42f && Mathf.Abs(pos.z) <= 45f)
        {
            return "🏟️ 主看台觀眾席 (Bleachers)";
        }

        return "🌲 操場外圍周邊 (Perimeter Area)";
    }
}
