public struct ChaProperty {

    public static readonly ChaProperty Zero = new ChaProperty(0, 0, default);

    public int hp;
    public int attack;
    public float moveSpeed;
    public int criticalRate;
    public float attackTime;

    public Circle hitCircle;
    public Circle attackRange;

    public ChaProperty(
            int hp, 
            int attack,
            Circle hitCircle) {
        this.hp = hp;
        this.attack = attack;
        this.moveSpeed = 0f;
        this.criticalRate = 0;
        this.attackTime = float.MaxValue;
        this.hitCircle = hitCircle;
        this.attackRange = default;
    }

    public ChaProperty(
            int hp, 
            int attack,
            float attackTime,
            Circle hitCircle,
            Circle attackRange) {
        this.hp = hp;
        this.attack = attack;
        this.moveSpeed = 0f;
        this.criticalRate = 0;
        this.attackTime = attackTime;
        this.hitCircle = hitCircle;
        this.attackRange = attackRange;
    }

    public void SetPosition(Vector2 pos) {
        this.hitCircle.x = pos.x;
        this.hitCircle.y = pos.y;
        this.attackRange.x = pos.x;
        this.attackRange.y = pos.y;
    }
    public void SetMoveSpeed(float moveSpeed) => this.moveSpeed = moveSpeed;
    public void SetCriticalRate(int criticalRate) => this.criticalRate = criticalRate;
    public void SetAttackTime(float attackTime) => this.attackTime = attackTime;

    public static ChaProperty operator +(ChaProperty a, ChaProperty b) {
        return new ChaProperty(
            a.hp + b.hp,
            a.attack + b.attack,
            a.moveSpeed + b.moveSpeed,
            a.criticalRate + b.criticalRate,
            a.hitCircle
        );
    }
    public static ChaProperty operator -(ChaProperty a, ChaProperty b) {
        return new ChaProperty(
            a.hp - b.hp,
            a.attack - b.attack,
            a.moveSpeed - b.moveSpeed,
            a.criticalRate - b.criticalRate,
            a.hitCircle);
    }
    public static ChaProperty operator *(ChaProperty a, ChaProperty b) {
        return new ChaProperty(
            Mathf.RoundToInt(a.hp * (1.0000f + Mathf.Max(b.hp, -0.9999f))),
            Mathf.RoundToInt(a.attack * (1.0000f + Mathf.Max(b.attack, -0.9999f))),
            Mathf.RoundToInt(a.moveSpeed * (1.0000f + Mathf.Max(b.moveSpeed, -0.9999f))),
            Mathf.RoundToInt(a.criticalRate * (1.0000f + Mathf.Max(b.criticalRate, -0.9999f))),
            a.hitCircle);
    }
    public static ChaProperty operator *(ChaProperty a, float b) {
        return new ChaProperty(
            Mathf.RoundToInt(a.hp * b),
            Mathf.RoundToInt(a.attack * b),
            Mathf.RoundToInt(a.moveSpeed * b),
            Mathf.RoundToInt(a.criticalRate * b),
            a.hitCircle);
    }
}