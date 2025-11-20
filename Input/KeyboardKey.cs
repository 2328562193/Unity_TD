public abstract class AbstractInput{}

public abstract class PushKey : AbstractInput { 

    public abstract bool isDown { get; }
    public abstract bool isUp { get; }
    public abstract bool isPress { get; }
}
public class KeyboardKey : PushKey {
    
    private KeyCode key;

    public override bool isDown { get => Input.GetKeyDown(key);}
    public override bool isUp { get => Input.GetKeyUp(key);}
    public override bool isPress { get => Input.GetKey(key);}

    public KeyboardKey(KeyCode key) {
        this.key = key;
    }

    public void ResetKey(KeyCode key) {
        this.key = key;
    }
}
public class MouseKey : PushKey {

    private int key;

    public override bool isDown { get => Input.GetMouseButtonDown(key);}
    public override bool isUp { get => Input.GetMouseButtonUp(key);}
    public override bool isPress { get => Input.GetMouseButton(key);}

    public MouseKey(int key) {
        this.key = key;
    }

    public void ResetKey(int key) {
        this.key = key;
    }
}

public abstract class ValueInput : AbstractInput {
    public abstract float value { get; }
}
public class ScrollInputX : ValueInput {
    public override float value { get => Input.mouseScrollDelta.x; }
}
public class ScrollInputY : ValueInput {
    public override float value { get => Input.mouseScrollDelta.y; }
}
public class AxisInput : ValueInput {
    private string axisName;
    public override float value { get => Input.GetAxisRaw(axisName); }
    public AxisInput(string axisName) {
        this.axisName = axisName;
    }
    public void ResetAxis(string axisName) {
        this.axisName = axisName;
    }
}

public class MousePosition : AbstractInput {
    public Vector2 value { 
        get => new Vector2(Mathf.Clamp(Input.mousePosition.x, 0f, Screen.width), 
            Mathf.Clamp(Input.mousePosition.y, 0f, Screen.height)); 
    }
}

public enum InputEnum{
    Alpha1,
    Alpha2,
    Alpha3,
    Alpha4,
    MouseLeft,
    MouseMiddle,
    MouseRight,
    MouseScrollX,
    MouseScrollY,
    MousePosition,
}







