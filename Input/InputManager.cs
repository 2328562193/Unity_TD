public class InputManager : Singleton<InputManager> { 
    
    private static readonly Dictionary<string, IInputController> inputControllerDict = new();

    public void Start(){
        AddInputController("towerCardInputController", new TowerCardInputController());
        AddInputController("openCloseUiInputController", new OpenCloseUiInputController());
    }

    public void Update(){ 
        foreach (IInputController controller in inputControllerDict.Values) {
            if (controller.Enable) controller.Update();
        }
    }

    public void AddInputController(string inputControllerName, IInputController inputController) { 
        inputControllerDict.Add(inputControllerName, inputController);
    }

    public void SetEnableInputController(string inputControllerName, bool enable) { 
        if (inputControllerDict.TryGetValue(inputControllerName, out IInputController controller)) {
            controller.Enable = enable;
        }
    }
    
    private readonly Dictionary<InputEnum, AbstractInput> enumToInput = new();

    public static T GetInput<T>(InputEnum inputEnum) where T : AbstractInput {
        if(enumToInput.TryGetValue(inputEnum, out AbstractInput input)) return input as T;
        if(inputEnum == InputEnum.Alpha1) input = new KeyboardKey(KeyCode.Alpha1);
        else if(inputEnum == InputEnum.Alpha2) input = new KeyboardKey(KeyCode.Alpha2);
        else if(inputEnum == InputEnum.Alpha3) input = new KeyboardKey(KeyCode.Alpha3);
        else if(inputEnum == InputEnum.Alpha4) input = new KeyboardKey(KeyCode.Alpha4);
        else if(inputEnum == InputEnum.MouseLeft) input = new MouseKey(0);
        else if(inputEnum == InputEnum.MouseMiddle) input = new MouseKey(1);
        else if(inputEnum == InputEnum.MouseRight) input = new MouseKey(2);
        else if(inputEnum == InputEnum.MouseScrollX) input = new ScrollInputX();
        else if(inputEnum == InputEnum.MouseScrollY) input = new ScrollInputY();
        else if(inputEnum == InputEnum.MousePosition) input = new MousePosition();
        else input = new AxisInput(inputEnum.ToString());
        if (input is T typedInput) {
            enumToInput.Add(inputEnum, input);
            return typedInput;
        } else {
            GameLogger.LogError($"InputEnum '{inputEnum}' resolved to {input?.GetType()}, but requested type is {typeof(T)}.");
            return null;
        }
        
    }
}