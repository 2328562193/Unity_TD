private static void AddToxic(BuffObj buffObj, ref DamageInfo damageInfo, BaseController attacker) {
    string buffName = "Toxic";
    AddBuffInfo addBuffInfo = new AddBuffInfo(attacker, damageInfo.defender, DesingerTables.Buff.data[buffName], 1);
    damageInfo.AddBuffToTarget(addBuffInfo);
}

private static void ToxicTick(BuffObj buffObj) {
    int poisoningInjury = 0;
    if (buffObj.model.onTickParams.Length > 0) poisoningInjury = (int)buffObj.model.onTickParams[0];
    GameManage.instance.CreateDamage(buffObj.caster, buffObj.carrier,
                            new Damage(poisoningInjury),
                            0, 0,
                            DamageInfoTag.POISON);
}

private static void HighlyToxic(BuffObj buffObj, ref DamageInfo damageInfo, BaseController attacker) {
    CharacterController defender = damageInfo.defender;
    string buffName = "Toxic";
    BuffObj buff = defender.FindBuff(buffName, attacker.Id);
    if(buff == null) return;
    if(buff.stack < buff.model.maxStack) return;
    AddBuffInfo addBuffInfo = new AddBuffInfo(attacker, defender, DesingerTables.Buff.data[buffName], buff.stack);
    defender.RemoveBuff(addBuffInfo);
    int poisoningInjury = 0;
    if (buffObj.model.onHitParams.Length > 0) poisoningInjury = (int)buffObj.model.onHitParams[0];
    float multiplier = 1f + Mathf.Log(buff.stack);
    GameManage.instance.CreateDamage(attacker, defender,
                            new Damage(poisoningInjury * multiplier),
                            0, 0,
                            DamageInfoTag.POISON);
}

private static void AugmentToxic(BuffObj buffObj, ref DamageInfo damageInfo, BaseController attacker) {
    int percentage = 0;
    if (buffObj.model.onHitParams.Length > 0) percentage = (int)buffObj.model.onHitParams[0];
    if(damageInfo.tag != DamageInfoTag.TOXIC) return;
    damageInfo.damage += new Damage(0, 0, percentage);
}

public struct Damage {
    public int ordinary;
    public int augmentValue;
    public int augmentPercentage;

    public Damage(int ordinary, int augmentValue = 0, int augmentPercentage = 0) {
        this.ordinary = ordinary;
        this.augmentValue = augmentValue;
        this.augmentPercentage = augmentPercentage;
    }

    public int Overall() {
        return this.ordinary + this.augmentValue + (this.augmentPercentage * this.ordinary / 100);
    }

    public static Damage operator +(Damage a, Damage b) {
        return new Damage(a.ordinary + b.ordinary, 
            a.augmentValue + b.augmentValue, 
            a.augmentPercentage + b.augmentPercentage);
    }
}

private static TimelineObj SubAttackTime(BuffObj buff, SkillObj skill, TimelineObj timeline) {
    if(skill.model.id != "FireBullet") return timeline;
    if(!GameManage.instance.levelManager.HaveGold(10)) return timeline;
    CharacterController attacker = buff.caster;
    string buffName = "SubAttackTime";
    AddBuffInfo addBuffInfo = new AddBuffInfo(attacker, attacker, DesingerTables.Buff.data[buffName], 1);
    buff.caster.AddBuff(addBuffInfo);
    GameManage.instance.levelManager.AddGold(-10);
    return timeline;
}
//create[SubAttackTime]buff

private static void MoneyReward(BuffObj buff, DamageInfo damageInfo, BaseController target){
    int money = 1;
    if (buff.model.onKillParams.Length > 0) money = (int)buff.model.onKillParams[0];
    int value = Random.Range(1, money + 1);
    GameManage.instance.levelManager.AddGold(value);
}

private static void DamageTransfer(BuffObj buff, ref DamageInfo damageInfo, BaseController attacker){
    CharacterController target = buff.caster;
    if(target == null || target.Dead) return;
    damageInfo.defender = target;
}

public static Vector2 StarTween(float t, BulletController bullet, BaseController target) {
    if(t < 0.7f || !bullet.param.ContainsKey("endPos")) {
        float mR = bullet.fireDegree * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(mR), Mathf.Sin(mR)) * bullet.property.moveSpeed;
    }
    if(t < 1f) return Vector2.zero;
    Vector2 startPos = bullet.Position;
    Vector2 endPos = (Vector2)bullet.param["endPos"];
    return (endPos - startPos).normalized * bullet.property.moveSpeed;
}

public static TimelineObj AddExplosionDebris(BuffObj buff, SkillObj skill, TimelineObj timeline) {
    if(skill.model.id != "AssignedPlaceCreateAoe") return timeline;
    timeline.model.nodes = AddNodeByAfter(timeline.model.nodes,
        "CreateAoe",
        "CreateExplosionDebris",
        "CreateExplosionDebris",
        0.0f,
        new object[1] { 3 });
    return timeline;
}

public static void CreateExplosionDebris(TimelineObj timeline, object[] param) {
    int debrisNumber = 0;
    if (param.Length > 0) debrisNumber = (int)param[0];
    if (!timeline.param.ContainsKey("AoeLauncher")) return;
    AoeLauncher aoeLauncher = (AoeLauncher)timeline.param["AoeLauncher"];
    for (int i = 0; i < debrisNumber; i++) {
        BulletLauncher bulletLauncher = new BulletLauncher("DebrisBullet",
                timeline.caster,
                aoeLauncher.position,
                Random.Range(0f, 360f),
                (int)(timeline.caster.property.attack * 0.3f),
                null);
        GameManage.instance.CreateBullet(bulletLauncher);
    }
}

public bool ReleaseSkill(SkillObj skill, Dictionary<string, object> param) {
    timeline = null;
    if (skill == null) return false;
    if (!skill.ReleaseSkill(this, param, out timeline)) return false;
    this.ModResource(skill.model.cost);
    foreach(BuffObj buff in this.Buffs) {
        buff.model.onCast?.Invoke(buff, skill, timeline);
    }
    return true;
}

// 子弹移除函数
public static void CreateAoe(BulletController bullet) {
    SkillObj skill = new SkillObj(DesingerTables.Skill.data["AssignedPlaceCreateAoe"]);
    Dictionary<string, object> param = new Dictionary<string, object>() { {"Position", bullet.Position} };
    bullet.caster.controller.ReleaseSkill(skill, param);
}

{"AssignedPlaceCreateAoe", new SkillModel("AssignedPlaceCreateAoe",
        "SkillAssignedPlaceCreateAoe",
        ChaResource.Null,
        "",
        ChaResource.Null,
        0f) }
        
{"SkillAssignedPlaceCreateAoe", new TimelineModel("SkillAssignedPlaceCreateAoe", new TimelineNode[] {
        new TimelineNode("SetAoeLauncher", 0f, "SetAoeLauncher", new object[1]{"HurtAoe"}),
        new TimelineNode("CreateAoe", 0f, "CreateAoe", new object[0])
    }) },

public static void SetAoeLauncher(TimelineObj timeline, object[] param) {
    if(!timeline.param.ContainsKey("Position")) return;
    Vector2 position = (Vector2)timeline.param["Position"];
    string aoeName = "HurtAoe";
    if(param != null && param.Length > 0) aoeName = (string)param[0];
    AoeLauncher aoeLauncher = new AoeLauncher(aoeName, 
        timeline.caster, 
        position, 
        0,
        timeline.caster.property.attack)
    timeline.param.Add("AoeLauncher", aoeLauncher);
}

public static void CreateAoe(TimelineObj timeline, object[] param) {
    if (!timeline.param.ContainsKey("AoeLauncher")) return;
    AoeLauncher aoeLauncher = (AoeLauncher)timeline.param["AoeLauncher"];
    GameManage.instance.CreateAoe(aoeLauncher);
}

BulletLauncher.cs
    public Vector2 targetPosition;

private static TimelineObj ThrowMore(BuffObj buff, SkillObj skill, TimelineObj timeline) {
    timeline.model.nodes = ReplaceNode(timeline.model.nodes, "FireBullet", "ThrowMoreBullet");
    return timeline;
}

public static void ThrowMoreBullet(TimelineObj timeline, object[] param) {
    if (!timeline.param.ContainsKey("BulletLauncher")) return;
    BulletLauncher bulletLauncher = (BulletLauncher)timeline.param["BulletLauncher"];
    int count = 3;
    for (int i = 0; i < count; i++) { 
        BulletLauncher newBulletLauncher = bulletLauncher.Clone();
        newBulletLauncher.targetPosition = bulletLauncher.targetPosition + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
        GameManage.instance.CreateBullet(newBulletLauncher);
    }
}

public static TimelineObj AddLavaAoe(BuffObj buff, SkillObj skill, TimelineObj timeline) {
    if(skill.model.id != "AssignedPlaceCreateAoe") return timeline;
    timeline.model.nodes = AddNodeByAfter(timeline.model.nodes,
        "CreateAoe",
        "CreateLavaAoe",
        "CreateLavaAoe",
        0.0f,
        new object[0]);
    return timeline;
}

public static void CreateLavaAoe(TimelineObj timeline, object[] param) {
    AoeLauncher aoeLauncher = (AoeLauncher)timeline.param["AoeLauncher"];
    AoeLauncher lavaAoeLauncher = new AoeLauncher("LavaAoe", 
        timeline.caster, 
        aoeLauncher.position + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 
        0,
        timeline.caster.property.attack);
    GameManage.instance.CreateAoe(aoeLauncher);
}