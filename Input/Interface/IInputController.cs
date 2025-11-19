/// 每种交互的控制器必须实现此接口
public interface IInputController { 
    public bool Enable { get; set; }

    public void HandleInteraction();
}