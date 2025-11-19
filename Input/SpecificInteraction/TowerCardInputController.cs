public class TowerCardInputController : IInputController { 

    public bool Enable { get; set; } = false;

    private static readonly InputEnum[] TowerKeys = {
        InputEnum.Alpha1,
        InputEnum.Alpha2,
        InputEnum.Alpha3,
        InputEnum.Alpha4
    };

    public void HandleInteraction() {
        for (int i = 0; i < TowerKeys.Length; i++) {
            PushKey key = InputManager.Instance.GetInput<PushKey>(TowerKeys[i]);
            if (key != null && key.isDown) {
                EventManager.Allocate<SelectTowerArgs>()
                    .Config(UIEvent.SelectTower, null, i)
                    .Invoke();
            }
        }
    }
}

public class SelectTowerArgs : BaseEventArgs{
    public int index;

    public SelectTowerArgs Config(KeyEvent _t, 
            GameObject _sender,
            int _index){
        base.Config(_t, _sender);
        this.index = _index;
        return this;
    }
}