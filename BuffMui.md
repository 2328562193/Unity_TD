BaseController的Buffs去掉private set属性

    private List<int> toRemoveBuff = new List<int>();

    private void HandlBuff(float timePassed){
        toRemoveBuff.Clear();
        int length = this.buffs.Count;
        for (int i = 0; i < length; i++) {
            BuffObj buff = this.buffs[i];
            if (buff == null) {
                toRemoveBuff.Add(i);
                continue;
            }
            buff.timeElapsed += timePassed;
            buff.duration -= timePassed;
            if (buff.model.tickTime > 0 && buff.model.onTick != null) {
                while (buff.timeElapsed >= buff.lastTickTime + buff.model.tickTime) {
                    buff.model.onTick(buffs[i]);
                    buff.ticked += 1;
                    buff.lastTickTime += buff.model.tickTime;
                }
            }
            if (buff.duration <= 0 || buff.stack <= 0) {
                buff.model.onRemoved?.Invoke(buff);
                toRemoveBuff.Add(i);
            }
        }
        for (int i = toRemoveBuff.Count - 1; i >= 0; i--){
            this.buffs.RemoveAt(toRemoveBuff[i]);
        }
    }

    private BuffObj FindBuff(string buffName, long casterId){
        if(buffName == null || casterId < 0) return null;
        int length = this.buffs.Count;
        for(int i = 0; i < length; i++) {
            BuffObj buff = this.buffs[i];
            if (buff == null) continue;
            if (!buff.model.name.Equals(buffName)) continue;
            if (buff.caster?.Id != casterId) continue;
            return buff;
        }
    }

    public void RemoveBuff(AddBuffInfo addBuffInfo) {
        BuffObj buff = FindBuff(addBuffInfo.buffModel.name, addBuffInfo.caster.Id);
        if (buff == null) return;
        buff.stack -= addBuffInfo.addStack;
    }

    public void AddBuff(AddBuffInfo addBuffInfo) {
        if (addBuffInfo.addStack <= 0){
            GameLog.LogWarning($"AddBuffInfo.addStack {addBuffInfo.addStack} <= 0");
            return;
        }
        BuffObj buff = FindBuff(addBuffInfo.buffModel.name, addBuffInfo.caster.Id);
        if(buff == null){
            BuffObj buff = new BuffObj(addBuffInfo.buffModel, 
                addBuffInfo.caster, 
                addBuffInfo.target == null ? (CharacterController)this : addBuffInfo.target, 
                addBuffInfo.buffParam);
            this.buffs.Add(buff);
            return;
        } 
        if (buff.stack + addBuffInfo.addStack < buff.model.maxStack){
            buff.stack += addBuffInfo.addStack;
            return;
        }
        buff.stack = buff.model.maxStack;
    }

    public Dictionary<string,ImageInfo> images = new Dictionary<string,ImageInfo>();

    public GameObject AddImage(string prefab, string imageName) {
        GameObject image = GameManage.instance.poolManager.GetGameObject(prefab, this.transform.position, this.transform.rotation);
        if(image == null) return null;
        RemoveImage(imageName);
        image.name = imageName;
        image.transform.SetParent(transform);
        images.Add(imageName, new ImageInfo(image, prefab));
        return image;
    }

    public void AddImage(string prefab, string imageName, float localScale) {
        GameObject image = AddImage(prefab, imageName);
        if(image == null) return;
        image.transform.localScale = new Vector3(localScale,
            localScale,
            image.transform.localScale.z);
    }

    public void RemoveImage(string imageName) {
        if(!images.ContainsKey(imageName)) return ;
        ImageInfo info = images[imageName];
        images.Remove(imageName);
        GameManage.instance.poolManager.RecycleGameObject(image.image, prefab);
    }


BulletManager.cs
    
    private List<BulletController> toRemoveBullet = new List<BulletController>();
    private List<BulletController> bulletList = new List<BulletController>();

    toRemoveBullet.Clear();
    bulletList.Clear();
    for(BulletController bullet in bullets){
        if (bullet == null || bullet.resource.hp <= 0) {
            toRemoveBullet.Add(bullet);
            continue;
        }
        if (bullet.timeElapsed <= 0) {
            bullet.model.onCreate?.Invoke(bullet);
        }
        // TODO 优化
        foreach (BulletHitRecord hitRecord in bullet.hitRecords.ToList()) {
            hitRecord.timeToCanHit -= timePassed;
            if (hitRecord.timeToCanHit <= 0) {
                bullet.hitRecords.Remove(hitRecord);
            }
        }
        bullet.MoveForce(timePassed);
        if (bullet.model.hitEnemy) TryCollideEnemys(bullet, timePassed);

        bullet.duration -= timePassed;
        bullet.timeElapsed += timePassed;
        // TODO �Ƿ���ײ�ϰ���
        if (bullet.duration <= 0 || bullet.resource.hp <= 0) { 
            toRemoveBullet.Add(bullet);
            continue;
        }
        bulletList.Add(bullet);
    }





































