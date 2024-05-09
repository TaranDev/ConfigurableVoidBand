using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using Inventory = RoR2.Inventory;
using RiskOfOptions;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;

namespace ConfigurableVoidBand
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class ConfigurableVoidBand : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "TaranDev";
        public const string PluginName = "ConfigurableVoidBand";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<float> cooldown;

        public static ConfigEntry<float> baseTotalDamage;

        public static ConfigEntry<float> totalDamagePerStack;

        public static ConfigEntry<float> baseRadius;

        public static ConfigEntry<float> radiusPerStack;

        public static ConfigEntry<float> basePullForce;

        public static ConfigEntry<float> pullForcePerStack;

        public void Awake()
        {
            configs();
        }

        private void OnEnable()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += OnHitEnemy;
        }

        private void OnDisable()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy -= OnHitEnemy;
        }

        private static void OnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo.procCoefficient == 0f || damageInfo.rejected || !NetworkServer.active || !damageInfo.attacker || !(damageInfo.procCoefficient > 0f))
            {
                return;
            }

            CharacterBody body = damageInfo.attacker.GetComponent<CharacterBody>();
            if (!body)
            {
                return;
            }

            CharacterMaster master = body.master;
            if (!master)
            {
                return;
            }

            Inventory inventory = master.inventory;

            if (!damageInfo.procChainMask.HasProc(ProcType.Rings) && damageInfo.damage / body.damage >= 4f)
            {
                if (body.HasBuff(DLC1Content.Buffs.ElementalRingVoidReady))
                {
                    int numVands = inventory.GetItemCount(DLC1Content.Items.ElementalRingVoid);
                    body.RemoveBuff(DLC1Content.Buffs.ElementalRingVoidReady);
                    for (int l = 1; (float)l <= cooldown.Value; l++)
                    {
                        body.AddTimedBuff(DLC1Content.Buffs.ElementalRingVoidCooldown, l);
                    }
                    ProcChainMask vandProcChainMask = damageInfo.procChainMask;
                    vandProcChainMask.AddProc(ProcType.Rings);
                    if (numVands > 0)
                    {
                        var vand = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/ElementalRingVoidBlackHole");
                        var radialForce = vand.GetComponent<RadialForce>();
                        var projectileExplosion = vand.GetComponent<ProjectileExplosion>();

                        var scale = (baseRadius.Value + radiusPerStack.Value * (numVands - 1)) / 15f;
                        vand.transform.localScale = new Vector3(scale, scale, scale);
                        radialForce.radius = baseRadius.Value + radiusPerStack.Value * (numVands - 1);
                        radialForce.forceMagnitude = -((basePullForce.Value * 100) + (pullForcePerStack.Value * 10 * (numVands - 1)));
                        projectileExplosion.blastRadius = baseRadius.Value + radiusPerStack.Value * (numVands - 1);

                        float damage = baseTotalDamage.Value + totalDamagePerStack.Value * (numVands - 1);
                        float totalDamage = Util.OnHitProcDamage(damageInfo.damage, body.damage, damage);

                        ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                        {
                            damage = totalDamage,
                            crit = damageInfo.crit,
                            damageColorIndex = DamageColorIndex.Void,
                            position = damageInfo.position,
                            procChainMask = vandProcChainMask,
                            force = 6000f,
                            owner = damageInfo.attacker,
                            projectilePrefab = vand,
                            rotation = Quaternion.identity,
                            target = null
                        });
                    }
                }
            }

            orig(self, damageInfo, victim);

        }

        private void configs()
        {

            cooldown = Config.Bind("General", "Cooldown", 20f, "Default is 20.");
            ModSettingsManager.AddOption(new StepSliderOption(cooldown,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 50f,
                    increment = 1f
                }));

            baseTotalDamage = Config.Bind("General", "Base Total Damage", 1f, "Each increment of 1 is 100% damage, so 1 is 100% base damage and 5 is 500% base damage. Default is 1.");
            ModSettingsManager.AddOption(new StepSliderOption(baseTotalDamage,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 250f,
                    increment = 0.25f
                }));

            totalDamagePerStack = Config.Bind("General", "Additional Total Damage Per Stack", 1f, "Each increment of 1 is +100% damage, so 1 is +100% base damage and 5 is +500% base damage. Default is 1.");
            ModSettingsManager.AddOption(new StepSliderOption(totalDamagePerStack, 
                new StepSliderConfig {
                min = 0f,
                max = 250f,
                increment = 0.25f
            }));

            baseRadius = Config.Bind("General", "Base Radius", 10f, "Default is 10.");
            ModSettingsManager.AddOption(new StepSliderOption(baseRadius,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 250f,
                    increment = 0.25f
                }));

            radiusPerStack = Config.Bind("General", "Additional Radius Per Stack", 3f, "Default is 3.");
            ModSettingsManager.AddOption(new StepSliderOption(radiusPerStack,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 250f,
                    increment = 0.25f
                }));

            basePullForce = Config.Bind("General", "Base Pull Force", 10f, "Default is 10.");
            ModSettingsManager.AddOption(new StepSliderOption(basePullForce,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 250f,
                    increment = 0.25f
                }));

            pullForcePerStack = Config.Bind("General", "Additional Pull Force Per Stack", 5f, "Default is 5.");
            ModSettingsManager.AddOption(new StepSliderOption(pullForcePerStack,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 250f,
                    increment = 0.25f
                }));
        }
    }
}
