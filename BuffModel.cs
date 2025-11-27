public struct BuffModel {
    public string name;             
    public string icon;             
    public string description;      
    public int maxStack;            
    public string group;            
    public string[] tags;           
    public int priority;            
    public float duration;          
    public float tickTime;          
    public BuffStackType stackType; 
    public ChaProperty[] propMod;
    public BuffOnOccur onOccur;
    public object[] onOccurParams;

    public BuffOnTick onTick;
    public object[] onTickParams;

    public BuffOnRemoved onRemoved;
    public object[] onRemovedParams;

    public BuffOnCast onCast;
    public object[] onCastParams;

    public BuffOnHit onHit;
    public object[] onHitParams;

    public BuffOnBeHurt onBeHurt;
    public object[] onBeHurtParams;

    public BuffOnKill onKill;
    public object[] onKillParams;

    public BuffOnBeKill onBeKill;
    public object[] onBeKillParams;

    public BuffOnBeKilled onBeKilled;
    public object[] onBeKilledParams;

    public BuffModel(string name,
            string description,
            BuffStackType stackType,
            int maxStack, 
            int priority,
            float duration){
        this.name = name;
        this.icon = "";
        this.group = "";
        this.tags = new string[0];
        this.description = description;
        this.stackType = stackType;
        this.maxStack  = maxStack;
        this.priority = priority;
        this.duration = duration;
        this.tickTime = float.MaxValue;
        this.propMod = new ChaProperty[2]{ChaProperty.zero, ChaProperty.zero};

        this.onOccur = null;
        this.onOccurParams = null;

        this.onRemoved = null;
        this.onRemovedParams = null;

        this.onTick = null;
        this.onTickParams = null;

        this.onCast = null;
        this.onCastParams = null;

        this.onHit = null;
        this.onHitParams = null;

        this.onBeHurt = null;
        this.onBeHurtParams = null;

        this.onKill = null;
        this.onKillParams = null;

        this.onBeKill = null;
        this.onBeKillParams = null;

        this.onBeKilled = null;
        this.onBeKilledParams = null;

    }

    public BuffModel SetIcon(string icon){
        BuffModel newBuff = new BuffModel(this);
        newBuff.icon = icon ?? "";
        return newBuff;
    }
    public BuffModel SetGroupAndTags(string group, params string[] tags){
        BuffModel newBuff = new BuffModel(this);
        newBuff.group = group ?? "";
        newBuff.tags = tags ?? new string[0];
        return newBuff;
    }
    public BuffModel AddProperty(ChaProperty augmentProperty, ChaProperty percentageProperty){
        BuffModel newBuff = new BuffModel(this);
        newBuff.propMod[0] += augmentProperty;
        newBuff.propMod[1] += percentageProperty;
        return newBuff;
    }
    public BuffModel SetOnOccur(string onOccur, params object[] occurParam = null){
        this.onOccur = (onOccur == "") ? null : DesignerScripts.Buff.onOccurFunc[onOccur];
        this.onOccurParams = occurParam == null ? new object[0] : occurParam;
        return this;
    }
    public BuffModel SetOnRemoved(string onRemoved, params object[] removedParam = null){
        this.onRemoved = (onRemoved == "") ? null : DesignerScripts.Buff.onRemovedFunc[onRemoved];
        this.onRemovedParams = removedParam == null ? new object[0] : removedParam;
        return this;
    }
    public BuffModel SetOnTick(float tickTime, string onTick, params object[] tickParam = null){
        this.tickTime = tickTime;
        this.onTick = (onTick == "") ? null : DesignerScripts.Buff.onTickFunc[onTick];
        this.onTickParams = tickParam == null ? new object[0] : tickParam;
        return this;
    }
    public BuffModel SetOnCast(string onCast, params object[] castParam = null){
        this.onCast = (onCast == "") ? null : DesignerScripts.Buff.onCastFunc[onCast];
        this.onCastParams = castParam == null ? new object[0] : castParam;
        return this;
    }
    public BuffModel SetOnHit(string onHit, params object[] hitParam = null){
        this.onHit = (onHit == "") ? null : DesignerScripts.Buff.onHitFunc[onHit];
        this.onHitParams = hitParam == null ? new object[0] : hitParam;
        return this;
    }
    public BuffModel SetOnBeHurt(string onBeHurt, params object[] hurtParam = null){
        this.onBeHurt = (onBeHurt == "") ? null : DesignerScripts.Buff.beHurtFunc[onBeHurt];
        this.onBeHurtParams = hurtParam == null ? new object[0] : hurtParam;
        return this;
    }
    public BuffModel SetOnKill(string onKill, params object[] killParam = null){
        this.onKill = (onKill == "") ? null : DesignerScripts.Buff.onKillFunc[onKill];
        this.onKillParams = killParam == null ? new object[0] : killParam;
        return this;
    }
    public BuffModel SetOnBeKill(string onBeKill, params object[] beKillParam = null){
        this.onBeKill = (onBeKill == "") ? null : DesignerScripts.Buff.onBeKillFunc[onBeKill];
        this.onBeKillParams = beKillParam == null ? new object[0] : beKillParam;
        return this;
    }
    public BuffModel SetOnBeKilled(string onBeKilled, params object[] beKilledParam = null){
        this.onBeKilled = (onBeKilled == "") ? null : DesignerScripts.Buff.beKilledFunc[onBeKilled];
        this.onBeKilledParams = beKilledParam == null ? new object[0] : beKilledParam;
        return this;
    }
    
    private BuffModel(BuffModel model){
        this.name = model.name;
        this.icon = model.icon;
        this.group = model.group;
        this.tags = model.tags;
        this.description = model.description;
        this.stackType = model.stackType;
        this.maxStack  = model.maxStack;
        this.priority = model.priority;
        this.duration = model.duration;
        this.tickTime = model.tickTime;
        this.propMod = new ChaProperty[2]{model.propMod[0], model.propMod[1]}
        
        this.onOccur = model.onOccur;
        this.onOccurParams = model.onOccurParams;

        this.onRemoved = model.onRemoved;
        this.onRemovedParams = model.onRemovedParams;

        this.onTick = model.onTick;
        this.onTickParams = model.onTickParams;

        this.onCast = model.onCast;
        this.onCastParams = model.onCastParams;

        this.onHit = model.onHit;
        this.onHitParams = model.onHitParams;

        this.onBeHurt = model.onBeHurt;
        this.onBeHurtParams = model.onBeHurtParams;

        this.onKill = model.onKill;
        this.onKillParams = model.onKillParams;

        this.onBeKill = model.onBeKill;
        this.onBeKillParams = model.onBeKillParams;

        this.onBeKilled = model.onBeKilled;
        this.onBeKilledParams = model.onBeKilledParams;
    }
}