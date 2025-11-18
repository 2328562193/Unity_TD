public class CheckUpdateAndDownload : MonoBehaviour {

    public Text updateText;
    public Button retryBtn;

    void Start() {
        retryBtn.gameObject.SetActive(false);
        retryBtn.onClick.AddListener(() => {
            StartCoroutine(DoUpdateAddressadble());
        });
        StartCoroutine(DoUpdateAddressadble());
    }

    IEnumerator DoUpdateAddressable() {

        if (!NetworkUtils.IsNetworkAvailable()) {
            updateText.text += "\n 未检测到网络，跳过更新";
            EnterGame();
            yield break;
        }

        // 有网络，继续热更流程
        updateText.text += "\n正在检查更新...";

        // 1. 初始化
        yield return Addressables.InitializeAsync();

        // 2. 检查更新
        var checkOp = Addressables.CheckForCatalogUpdates();
        yield return checkOp;
        if (checkOp.Result?.Count == 0) {
            updateText.text += "\n已是最新版本";
            EnterGame();
            yield break;
        }
        var catalogNames = checkOp.Result;
        Addressables.Release(checkOp);

        // 3. 更新 Catalog
        var updateOp = Addressables.UpdateCatalogs(catalogNames);
        yield return updateOp;
        if (updateOp.Status == AsyncOperationStatus.Failed) {
            OnError("更新 Catalog 失败: " + updateOp.OperationException);
            yield break;
        }

        // 4. 获取所有远程资源 keys（可选）
        var remoteKeys = new List<object>();
        foreach (var locator in updateOp.Result) {
            foreach (var key in locator.Keys) {
                if (locator.Locate(key, out var locations) && locations.Count > 0) {
                    if (locations[0].InternalId.StartsWith("http"))
                        remoteKeys.Add(key);
                }
            }
        }
        Addressables.Release(updateOp);

        // 5. 下载依赖
        if (remoteKeys.Count > 0) {
            updateText.text += $"\n发现 {remoteKeys.Count} 个远程资源，开始下载...";
            var downloadOp = Addressables.DownloadDependenciesAsync(remoteKeys, true);
            
            // 监听进度（简单轮询）
            while (!downloadOp.IsDone) {
                updateText.text = $"下载中: {downloadOp.PercentComplete:P1}";
                yield return null;
            }

            if (downloadOp.Status == AsyncOperationStatus.Failed) {
                OnError("下载失败: " + downloadOp.OperationException);
                yield break;
            }
        }
        Addressables.Release(downloadOp);

        // 6. 清理旧缓存（关键！）
        yield return Addressables.ClearDependencyCacheAsync();

        updateText.text += "\n更新完成！";
        EnterGame();
    }

    private void OnError(string msg) {
        updateText.text = updateText.text + $"\n{msg}\n请重试! ";
        retryBtn.gameObject.SetActive(true);
    }


    // 进入游戏
    void EnterGame() {
        // TODO
    }
}