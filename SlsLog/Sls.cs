using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using System.IO;

/// <summary>
/// 阿里云 SLS 日志服务（签名认证模式，支持批量发送、缓存、退出处理）
/// </summary>
public class Sls : SingtoMono<Sls> {
    // === 配置参数 ===
    private static string accessKeyId = "YOUR_ACCESS_KEY_ID";
    private static string accessKeySecret = "YOUR_ACCESS_KEY_SECRET";
    private static string project = "project";
    private static string logstore = "logstore";
    private static string serviceAddr = "serviceAddr";
    private static string endpoint = $"https://{project}.{serviceAddr}";

    // 批量发送配置
    [Header("批量发送配置")]
    [SerializeField] private int maxBatchSize = 100;
    [SerializeField] private float sendInterval = 2.0f;
    [SerializeField] private int maxRetryCount = 3;

    [Header("退出处理配置")]
    [SerializeField] private bool enableLocalCache = true;

    // 队列和缓存
    private Queue<LogEntry> logQueue = new Queue<LogEntry>();
    private List<LogEntry> sendingBuffer = new List<LogEntry>();
    private bool isSending = false;
    private bool isQuitting = false;

    // 缓存文件路径
    private string cacheFilePath;
    private const string CACHE_FILE_NAME = "sls_log_cache.dat";

    // 统计
    private int totalSent = 0;
    private int totalFailed = 0;

    [Serializable]
    public class LogEntry {
        public string topic;
        public Dictionary<string, string> data;
        public long timestamp;
        public int retryCount;

        public LogEntry(string topic, Dictionary<string, string> sourceData) {
            this.topic = topic;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.retryCount = 0;

            this.data = new Dictionary<string, string>();
            foreach (var kvp in sourceData) this.data[kvp.Key] = kvp.Value;
        }
    }

    protected override void Awake() {
        base.Awake();
        cacheFilePath = Path.Combine(Application.persistentDataPath, CACHE_FILE_NAME);
        LoadAndSendCachedLogs();
        StartCoroutine(AutoSendCoroutine());
    }


    /// <summary>
    /// 添加日志到队列
    /// </summary>
    private void AddLog(string topic, Dictionary<string, string> logData) {
        lock (logQueue) {
            var entry = new LogEntry(topic, logData);
            logQueue.Enqueue(entry);
        }
    }


    #region 公共接口
    /// <summary>
    /// 分析埋点
    /// </summary>
    public static void Analytics(string logString, params (string key, string value)[] fields) {
        var logData = new Dictionary<string, string> {
            { "Level", "Analytics" },
            { "Message", logString }
        };
        foreach (var (key, value) in fields) logData[key] = value ?? "";
        instance?.AddLog(RandomStringGenerator.Get(10), logData);
    }

    /// <summary>
    /// 发送带自定义字段的日志
    /// </summary>
    public static void SendLogWithFields(string message, params (string key, string value)[] fields) {
        var logData = new Dictionary<string, string> {
            { "Message", message }
        };
        // 自动注入通用字段
        // logData["ClientTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        // logData["Platform"] = Application.platform.ToString();
        // logData["AppVersion"] = Application.version;
        // logData["DeviceModel"] = SystemInfo.deviceModel;
        foreach (var (key, value) in fields) logData[key] = value ?? "";
        instance.AddLog(RandomStringGenerator.Get(10), logData);
    }

    /// <summary>
    /// 立即发送所有日志
    /// </summary>
    public bool SendAllImmediately() {
        if (isSending || isQuitting) return false;
        StartCoroutine(ForceSendAllLogs());
        return true;
    }
    #endregion

    #region 批量发送核心
    /// <summary>
    /// 自动发送协程
    /// </summary>
    private IEnumerator AutoSendCoroutine() {
        while (true) {
            yield return new WaitForSeconds(sendInterval);
            if (logQueue.Count > 0 && !isSending && !isQuitting) StartCoroutine(SendBufferedLogs());
        }
    }

    /// <summary>
    /// 批量发送日志（核心方法）
    /// </summary>
    private IEnumerator SendBufferedLogs() {
        if (isSending || logQueue.Count == 0) yield break;
        isSending = true;
        try {
            lock (logQueue) {
                int count = Math.Min(maxBatchSize, logQueue.Count);
                for (int i = 0; i < count; i++) sendingBuffer.Add(logQueue.Dequeue());
            }
            if (sendingBuffer.Count == 0) yield break;
            string jsonBody = BuildBatchSLSJson(sendingBuffer);
            bool success = false;
            int retry = 0;
            while (!success && retry < maxRetryCount && !isQuitting) {
                if (retry > 0) yield return new WaitForSeconds(1f);
                UnityWebRequest request = CreateSLSRequest(jsonBody);
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success) {
                    success = true;
                    totalSent += sendingBuffer.Count;
                    sendingBuffer.Clear();
                    request.Dispose();
                    break;
                }
                if (++retry < maxRetryCount) {
                    request.Dispose();
                    continue;
                }
                List<LogEntry> failedLogs = new List<LogEntry>();
                lock (logQueue) {
                    foreach (var entry in sendingBuffer) {
                        if (entry.retryCount++ < maxRetryCount) logQueue.Enqueue(entry);
                        else failedLogs.Add(entry);
                    }
                    sendingBuffer.Clear();
                }
                if (failedLogs.Count > 0) {
                    totalFailed += failedLogs.Count;
                    SaveFailedLogsToCache(failedLogs);
                }
                request.Dispose();
            }
        } catch (Exception e) { } finally { isSending = false; }
    }

    /// <summary>
    /// 强制发送所有日志
    /// </summary>
    private IEnumerator ForceSendAllLogs() {
        while (logQueue.Count > 0 && !isQuitting) {
            yield return StartCoroutine(SendBufferedLogs());
            if (logQueue.Count > 0) yield return new WaitForSeconds(0.1f);
        }
    }
    #endregion

    #region JSON构建和请求
    /// <summary>
    /// 构建批量日志JSON
    /// </summary>
    private string BuildBatchSLSJson(List<LogEntry> logEntries) {
        StringBuilder json = new StringBuilder();
        json.Append("{\"__logs__\":[");

        bool firstLog = true;
        foreach (var entry in logEntries) {
            if (!firstLog) json.Append(",");
            firstLog = false;

            json.Append("{");

            // 添加时间戳
            json.Append($"\"__time__\":{entry.timestamp},");

            // 添加topic
            if (!string.IsNullOrEmpty(entry.topic)) json.Append($"\"__topic__\":\"{EscapeJsonString(entry.topic)}\",");

            // 添加日志内容
            bool firstField = true;
            foreach (var kvp in entry.data) {
                if (!firstField) json.Append(",");
                firstField = false;

                json.Append($"\"{EscapeJsonString(kvp.Key)}\":\"{EscapeJsonString(kvp.Value)}\"");
            }

            json.Append("}");
        }

        json.Append("]}");

        return json.ToString();
    }

    /// <summary>
    /// 创建SLS请求
    /// </summary>
    private UnityWebRequest CreateSLSRequest(string jsonBody) {
        string url = $"{endpoint}/projects/{project}/logstores/{logstore}";
        UnityWebRequest request = new UnityWebRequest(url, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // 设置Headers
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-log-apiversion", "0.6.0");
        request.SetRequestHeader("x-log-signaturemethod", "hmac-sha1");

        // 日期和签名
        string date = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
        request.SetRequestHeader("Date", date);

        string signature = GenerateSignature("POST", logstore, date, accessKeySecret);
        request.SetRequestHeader("Authorization", $"SLS {accessKeyId}:{signature}");

        return request;
    }

    /// <summary>
    /// 生成签名
    /// </summary>
    private string GenerateSignature(string method, string resource, string date, string secret) {
        string stringToSign = $"{method}\n\napplication/json\n{date}\n" +
                              $"x-log-apiversion:0.6.0\nx-log-signaturemethod:hmac-sha1\n/projects/{project}/logstores/{resource}";

        using (var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secret))) {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// JSON字符串转义
    /// </summary>
    private string EscapeJsonString(string input) {
        if (string.IsNullOrEmpty(input))
            return "";

        StringBuilder sb = new StringBuilder();
        foreach (char c in input) {
            switch (c) {
                case '\\': sb.Append("\\\\"); break;
                case '\"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < 32 || c > 126) {
                        sb.AppendFormat("\\u{0:x4}", (int)c);
                    } else {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }
    #endregion

    #region 缓存管理
    /// <summary>
    /// 加载缓存的日志到内存队列（实际发送由异步协程处理）
    /// </summary>
    private void LoadAndSendCachedLogs() {
        if (!File.Exists(cacheFilePath)) return;

        try {
            string[] lines = File.ReadAllLines(cacheFilePath);
            List<LogEntry> cachedLogs = new List<LogEntry>();

            foreach (string line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var entry = ParseCachedLogLine(line);
                if (entry != null) cachedLogs.Add(entry);
            }

            if (cachedLogs.Count == 0) return;
            lock (logQueue) {
                foreach (var log in cachedLogs) logQueue.Enqueue(log);
            }
            File.Delete(cacheFilePath);
        } catch (Exception e) { }
    }

    /// <summary>
    /// 解析单行缓存的日志（格式：topic|timestamp|data）
    /// </summary>
    /// <param name="line">缓存文件中的一行</param>
    /// <returns>解析成功的 LogEntry，失败返回 null</returns>
    private LogEntry ParseCachedLogLine(string line) {
        try {
            // 简单解析：topic|timestamp|data
            var parts = line.Split('|');
            if (parts.Length < 3) return null;
            var entry = new LogEntry(parts[0], new Dictionary<string, string>());
            entry.timestamp = long.Parse(parts[1]);

            var dataParts = parts[2].Split(';');
            foreach (var dataPart in dataParts) {
                var kv = dataPart.Split('=');
                if (kv.Length != 2) continue;
                entry.data[UnescapeCacheString(kv[0])] = UnescapeCacheString(kv[1]);
            }
            return entry;
        } catch (Exception e) { }
        return null;
    }

    /// <summary>
    /// 保存日志到本地缓存文件
    /// </summary>
    private void SaveFailedLogsToCache(List<LogEntry> logs) {
        try {
            List<string> lines = new List<string>();
            foreach (var entry in logs) {
                List<string> dataParts = new List<string>();
                foreach (var kvp in entry.data) {
                    dataParts.Add($"{EscapeCacheString(kvp.Key)}={EscapeCacheString(kvp.Value)}");
                }
                string dataStr = string.Join(";", dataParts);
                lines.Add($"{entry.topic}|{entry.timestamp}|{dataStr}");
            }
            File.AppendAllLines(cacheFilePath, lines);
        } catch (Exception e) { }
    }

    /// <summary>
    /// 保存所有未发送日志到缓存
    /// </summary>
    private void SaveAllUnsentLogsToCache() {
        try {
            var allLogs = new List<LogEntry>();
            allLogs.AddRange(sendingBuffer);
            lock (logQueue) allLogs.AddRange(logQueue);
            if (allLogs.Count == 0) return;
            SaveFailedLogsToCache(allLogs);
            sendingBuffer.Clear();
            lock (logQueue) logQueue.Clear();
        } catch (Exception e) { }
    }

    /// <summary>
    /// 缓存字符串转义
    /// </summary>
    private string EscapeCacheString(string input) {
        if (string.IsNullOrEmpty(input)) return "";
        return input
            .Replace("\\", "\\\\")
            .Replace("|", "\\p")
            .Replace(";", "\\s")
            .Replace("=", "\\e")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }

    /// <summary>
    /// 缓存字符串反转义
    /// </summary>
    private string UnescapeCacheString(string input) {
        if (string.IsNullOrEmpty(input)) return "";=
        return input
            .Replace("\\r", "\r")
            .Replace("\\n", "\n")
            .Replace("\\e", "=")
            .Replace("\\s", ";")
            .Replace("\\p", "|")
            .Replace("\\\\", "\\");
    }
    #endregion

    #region 退出处理
    /// <summary>
    /// 应用退出处理
    /// </summary>
    private void OnApplicationQuit() {
        isQuitting = true;
        StopAllCoroutines();
        if (!TrySendRemainingLogsSync()) SaveAllUnsentLogsToCache();
    }

    /// <summary>
    /// 尝试同步发送剩余日志
    /// </summary>
    private bool TrySendRemainingLogsSync() {
        int totalLogs = logQueue.Count + sendingBuffer.Count;
        if (totalLogs == 0) return true;
        try {
            List<LogEntry> batch = new List<LogEntry>();
            batch.AddRange(sendingBuffer);
            lock (logQueue) batch.AddRange(logQueue);
            if (batch.Count == 0) return true;
            if (!SendBatchSync(batch)) return false;
            sendingBuffer.Clear();
            lock (logQueue) logQueue.Clear();
            return true;
        } catch (Exception e) { return false; }
    }

    /// <summary>
    /// 同步发送日志
    /// </summary>
    private bool SendBatchSync(List<LogEntry> batch) {
        try {
            string jsonBody = BuildBatchSLSJson(batch);
            using (UnityWebRequest request = CreateSLSRequest(jsonBody)) {
                var operation = request.SendWebRequest();
                float startTime = Time.realtimeSinceStartup;
                while (!operation.isDone) {
                    if (Time.realtimeSinceStartup - startTime > 5f) return false;
                }
                if (request.result == UnityWebRequest.Result.Success) {
                    totalSent += batch.Count;
                    return true;
                }
                return false;
            }
        } catch (Exception e) { return false; }
    }
    #endregion
}