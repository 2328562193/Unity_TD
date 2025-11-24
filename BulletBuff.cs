private static void AddToxic(BuffObj buffObj, ref DamageInfo damageInfo, BaseController attacker) {
    CharacterController defender = damageInfo.defender;
    string buffName = "Toxic";
    AddBuffInfo addBuffInfo = new AddBuffInfo(attacker, defender, DesingerTables.Buff.data[buffName], 1);
    defender.AddBuff(addBuffInfo);
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