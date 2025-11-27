using System.Collections.Generic;
using DesignerScripts;

namespace DesingerTables {
    public class Buff {
        public static Dictionary<string, BuffModel> data = new Dictionary<string, BuffModel>(){
            {"DestinationTask", new BuffModel(
                "DestinationTask", 
                "Go to the designated location", 
                BuffStackType.None,
                maxStack: 1, priority: 0, duration: float.MaxValue)
                .SetGroupAndTags("task", "任务")
                .SetOnRemoved("TaskCommit")
                .SetOnTick(0.1f, "ArriveDestination", "x", "y")
                .SetOnBeKilled("TaskFail")
            },
            {"SurvivalTask", new BuffModel(
                "SurvivalTask",
                "在规定时间内未死亡",
                BuffStackType.None,
                maxStack: 1, priority: 0, duration: 60f)
                .SetGroupAndTags("task", "时间")
                .SetOnRemoved("TaskCommit")
                .SetOnTick(3f, "Survival")
                .SetOnBeKilled("TaskFail")
            },
            {"HealHealth", new BuffModel(
                "HealHealth",
                "一段时间内恢复一定血量",
                BuffStackType.None,
                maxStack: 1, priority: 0, duration: float.MaxValue)
                .SetGroupAndTags("恢复血量", "恢复")
                .SetOnTick(1f, "HealHealth", 10)
            },
            {"Repelled", new BuffModel(
                "Repelled",
                "受到攻击时触发击退效果",
                BuffStackType.None,
                maxStack: 1, priority: 0, duration: float.MaxValue)
                .SetGroupAndTags("被动击退", "击退")
                .SetOnBeHurt("Repelled")
            },
            {"ProductionGold", new BuffModel(
                "ProductionGold",
                "一段时间内产生指定数量的金币",
                BuffStackType.None,
                maxStack: 1, priority: 0, duration: float.MaxValue)
                .SetGroupAndTags("生产金币", "生产")
                .SetOnTick(5f, "ProductionGold", 10)
            },
            {"Maintain", new BuffModel(
                "Maintain",
                "一段时间内维持该角色存活",
                BuffStackType.None,
                maxStack: 1, priority: 0, duration: float.MaxValue)
                .SetGroupAndTags("维持血量", "维持")
                .SetOnTick(3f, "Maintain", 1)
            },
            {"FireBulletCooling", new BuffModel(
                "FireBulletCooling",
                "修复子弹冷却",
                BuffStackType.None,
                maxStack: 1, priority: 0, duration: float.MaxValue)
                .SetGroupAndTags("修复子弹冷却", "冷却")
                .SetOnCast("FireBulletCooling")
            },
            {"NormalAttackCooling", new BuffModel(
                "NormalAttackCooling",
                "修复近战攻击冷却",
                BuffStackType.None,
                maxStack: 1, priority: 0, duration: float.MaxValue)
                .SetGroupAndTags("修复近战攻击冷却", "冷却")
                .SetOnCast("NormalAttackCooling")
            },
            {"FireVolleyBullet", new BuffModel(
                "FireVolleyBullet",
                "发射子弹",
                BuffStackType.None,
                maxStack: 1, priority: 0, duration: float.MaxValue)
                .SetOnCast("FireVolleyBullet")
            },
            {"FireDartleBullet", new BuffModel(
                "FireDartleBullet",
                "发射子弹",
                BuffStackType.None,
                maxStack: 1, priority: 0, duration: float.MaxValue)
                .SetOnCast("FireDartleBullet")
            },
            {"FireScatteringBullet", new BuffModel(
                "FireScatteringBullet",
                "散射子弹",
                BuffStackType.None,
                maxStack: 1, priority: 0, duration: float.MaxValue)
                .SetOnCast("FireScatteringBullet")
            },
            {"BulletPenetrate", new BuffModel(
                "BulletPenetrate",
                "子弹穿透",
                BuffStackType.None,
                maxStack: 1, priority: 0, duration: float.MaxValue)
                .SetOnCast("BulletPenetrate")
            },
        };
    }
}