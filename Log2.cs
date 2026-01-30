using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;

public class SLSSender : MonoBehaviour
{
    private string accessKeyId = "YOUR_ACCESS_KEY_ID";
    private string accessKeySecret = "YOUR_ACCESS_KEY_SECRET";
    private string project = "your-project";
    private string logstore = "your-logstore";
    private string endpoint =  $ "https://{project}.cn-hangzhou.log.aliyuncs.com"; // 替换为你的地域

    public void SendLog(string topic, Dictionary<string, object> logData)
    {
        string url =  $ "{endpoint}/logstores/{logstore}/track";
        var request = new UnityWebRequest(url, "POST");

        // 构造日志内容（JSON 格式）
        var logItem = new Dictionary<string, object>
        {
            { "time", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            { "contents", logData }
        };
        string jsonBody = JsonUtility.ToJson(new LogGroup { logs = new List<Dictionary<string, object>> { logItem } });
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // 设置必要 Header
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-log-apiversion", "0.6.0");
        request.SetRequestHeader("x-log-signaturemethod", "hmac-sha1");

        // 生成签名
        string date = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
        request.SetRequestHeader("Date", date);

        string signature = GenerateSignature("POST", logstore, date, accessKeySecret);
        request.SetRequestHeader("Authorization",  $ "SLS {accessKeyId}:{signature}");

        StartCoroutine(SendRequest(request));
    }

    private string GenerateSignature(string method, string resource, string date, string secret)
    {
        string stringToSign =  $ "{method}\n\napplication/json\n{date}\nx-log-apiversion:0.6.0\nx-log-signaturemethod:hmac-sha1\n/{resource}";
        using (var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secret)))
        {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            return Convert.ToBase64String(hash);
        }
    }

    private IEnumerator SendRequest(UnityWebRequest req)
    {
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Log sent successfully!");
        }
        else
        {
            Debug.LogError( $ "Failed to send log: {req.error}");
        }
    }
}

// 简化的 LogGroup 结构（用于 JSON 序列化）
[System.Serializable]
public class LogGroup
{
    public List<Dictionary<string, object>> logs;
}