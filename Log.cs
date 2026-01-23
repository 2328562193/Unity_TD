// GameAnalytics.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 阿里云 SLS 游戏埋点 SDK（WebTracking 模式）
/// 调用示例：
///   GameAnalytics.TrackCrux("LevelStart", ("LevelId", "L1_2"));
///   GameAnalytics.TrackError("资源加载失败");
/// </summary>
public class GameAnalytics : MonoBehaviour
{
    // === 配置区（请替换为你的实际值）===
    private const string PROJECT = "your-project-name";
    private const string LOGSTORE = "your-logstore-name";
    private const string SERVICE_ADDR = "cn-hangzhou.log.aliyuncs.com"; // 替换为你的地域 endpoint

    // 单例
    public static GameAnalytics Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ================== 公共 API ==================

    /// <summary>关键行为埋点（Crux）</summary>
    public static void TrackCrux(string eventName, params (string key, string value)[] fields)
        => Track(LogLevel.Crux, eventName, fields);

    /// <summary>普通信息埋点</summary>
    public static void TrackInfo(string message, params (string key, string value)[] fields)
        => Track(LogLevel.Info, message, fields);

    /// <summary>警告埋点</summary>
    public static void TrackWarning(string message, params (string key, string value)[] fields)
        => Track(LogLevel.Warning, message, fields);

    /// <summary>错误埋点</summary>
    public static void TrackError(string message, params (string key, string value)[] fields)
        => Track(LogLevel.Error, message, fields);

    // ================== 内部实现 ==================

    private static void Track(LogLevel level, string message, (string key, string value)[] fields)
    {
        if (Instance == null)
        {
            Debug.LogWarning("GameAnalytics not initialized! Add an empty GameObject with this script.");
            return;
        }

        var dict = new Dictionary<string, string>
        {
            { "Event", message },
            { "Level", level.ToString() }
        };

        // 自动注入通用字段
        dict["ClientTime"] = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        dict["Platform"] = Application.platform.ToString();
        dict["AppVersion"] = Application.version;
        dict["DeviceModel"] = SystemInfo.deviceModel;
        dict["UserId"] = GetOrCreateUserId(); // 可替换为你的用户ID逻辑

        // 添加自定义字段
        foreach (var (key, value) in fields)
        {
            dict[key] = value ?? "";
        }

        Instance.StartCoroutine(Instance.SendToSls(dict));
    }

    private static string GetOrCreateUserId()
    {
        if (!PlayerPrefs.HasKey("Analytics_UserId"))
        {
            string uid = System.Guid.NewGuid().ToString("N").Substring(0, 16);
            PlayerPrefs.SetString("Analytics_UserId", uid);
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetString("Analytics_UserId");
    }

    private IEnumerator SendToSls(Dictionary<string, string> data)
    {
        // 构造查询参数（URL 编码）
        var pairs = data.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"
        );
        string queryString = string.Join("&", pairs);
        string url = $"https://{PROJECT}.{SERVICE_ADDR}/logstores/{LOGSTORE}/track?APIVersion=0.6.0&{queryString}";

        using (var request = new UnityEngine.Networking.UnityWebRequest(url, "GET"))
        {
            yield return request.SendWebRequest();

            #if UNITY_2020_1_OR_NEWER
            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            #else
            if (request.isHttpError || request.isNetworkError)
            #endif
            {
                Debug.LogError($"[SLS] Failed to send log: {request.error}");
            }
        }
    }
}