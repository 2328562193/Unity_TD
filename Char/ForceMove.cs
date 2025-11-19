public class ForceMove{

    private readonly float totalDuration;
    private float duration;

    private Vector2 velocity;

    public ForceMove(Vector2 totalDisplacement, float duration){
        this.duration = duration;
        this.totalDuration = duration;
        this.velocity = (totalDuration > 0) ? totalDisplacement / totalDuration : Vector2.zero;
    }

    public Vector2 GetDisplacement (float time){
        if (totalDuration <= 0) return Vector2.zero; // 防止除零
        float actualTime = Mathf.Min(time, duration);
        duration -= actualTime;
        if(duration < 0) duration = 0;
        return velocity * actualTime;
    }

    public bool IsDone() => duration <= 0;
}