public class MovePreorder{

    private Vector3 totalDisplacement;
    private readonly float totalDuration;
    private float duration;

    public MovePreorder(Vector3 totalDisplacement, float duration){
        this.totalDisplacement = totalDisplacement;
        this.duration = duration;
        this.totalDuration = duration;
    }

    public Vector3 GetDisplacement (float time){
        if (totalDuration <= 0) return Vector3.zero; // 防止除零
        float actualTime = Mathf.Min(time, duration);
        duration -= actualTime;
        if(duration < 0) duration = 0;
        return (totalDisplacement / totalDuration) * actualTime;
    }
}