using UnityEngine;
using System.Collections.Generic;

public static class GameResource {

    private static readonly Dictionary<string, AsyncOperationHandle> caches = new Dictionary<string, AsyncOperationHandle>();

    public static T ResourceLoad<T>(string path) {
        if (string.IsNullOrEmpty(path)) {
            GameLogger.LogError("路径是null或empty");
            return null;
        }
        T data = Resources.Load<T>(path);
        if (data == null) {
            GameLogger.LogError($"资源为空或内容为空，路径: [{path}]");
            return null;
        }
        return data;
    }

    public static void ResourceUnloadAll() => Resources.UnloadUnusedAssets();
    
    public static T RemoteLoad<T>(string path) {
        if (string.IsNullOrEmpty(path)) {
            GameLogger.LogError("路径是null或empty");
            return null;
        }

        if(caches.TryGetValue(path, out var cache)) return cache.Result as T;
        
        var handle = Addressables.LoadAssetAsync<T>(path);
        T data = handle.WaitForCompletion();

        if (data == null) {
            GameLogger.LogError($"资源为空或内容为空，路径: [{path}]");
            Addressables.Release(handle);
            return null;
        }
        caches[path] = handle;
        return data;
    }

    public static async Task Preload(params string[] paths) {

        List<AsyncOperationHandle<Object>> handles = new List<AsyncOperationHandle<Object>>();
        List<string> keys = new List<string>();

        foreach (string path in paths) {
            if (string.IsNullOrEmpty(path)) continue;
            if (caches.ContainsKey(path)) continue;

            var handle = Addressables.LoadAssetAsync<Object>(path);
            handles.Add(handle);
            keys.Add(path);
        }

        if(handles.Count == 0) return;

        try{
            await Task.WhenAll(handles.Select(x => x.Task));
        } catch(Exception e) {
            GameLogger.LogError($"预加载失败，错误信息：{e.Message}");
            foreach(var h in handles) Addressables.Release(h);
            return;
        }
        
        for(int i = 0; i < handles.Count; i++){
            string key = keys[i];
            Object value = handles[i].Result;
            if(caches.ContainsKey(key) || value == null) {
                Addressables.Release(handles[i]);
                continue;
            }
            caches[key] = handles[i];
        }
    }

    private static void RemoteUnloadAll() {
        foreach (var value in caches.Values) {
            Addressables.Release(value);
        }
        caches.Clear();
    }
}