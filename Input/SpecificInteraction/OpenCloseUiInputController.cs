public class OpenCloseUiInputController : IInputController {

    private OpenCloseInteractable lastOpenCloseInteractable;

    public bool Enable { get; set; } = false;

    public void Update(){
        PushKey mouseKey = InputManager.Instance.GetInput<PushKey>(InputEnum.MouseLeft);
        if (mouseKey == null || !mouseKey.isDown) return ;
        OpenCloseInteractable openCloseInteractable = InputManager.Instance.WorldInteractable<OpenCloseInteractable>();
        lastOpenCloseInteractable?.OnClose();
        if (openCloseInteractable != lastOpenCloseInteractable){
            openCloseInteractable?.OnOpen();
            lastOpenCloseInteractable = openCloseInteractable;
        } else {
            lastOpenCloseInteractable = null;
        }
    }
}

public class OpenCloseInteractable : IInteractable { 
    public void OnOpen() {
        GameLogger.Log("打开");
    }

    public void OnClose() {
        GameLogger.Log("关闭了");
    }
}