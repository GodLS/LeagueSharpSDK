using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.SDK.Core.Extensions;
using LeagueSharp.SDK.Core.Wrappers;

namespace XinZhaoGod
{
    class Damage
    {
        public static double DamageQ(Obj_AI_Base target)
        {
                return Spells.Q.IsReady()
                       ? ObjectManager.Player.CalculateDamage(
                           target,
                           DamageType.Physical,
                           new double[] { 15, 30, 45, 50, 75 }[Spells.Q.Level - 1]
                           + ObjectManager.Player.TotalAttackDamage * .2f
                           + ObjectManager.Player.TotalAttackDamage)
                       : 0d;
        }

        public static double DamageE(Obj_AI_Base target)
        {
                return Spells.E.IsReady()
                       ? ObjectManager.Player.CalculateDamage(
                           target,
                           DamageType.Magical,
                           new double[] { 70, 110, 150, 190, 230 }[Spells.E.Level - 1]
                           + ObjectManager.Player.TotalMagicalDamage * .6f)
                       : 0d;
        }

        public static double DamageR(Obj_AI_Base target)
        {
                return Spells.R.IsReady()
                       ? ObjectManager.Player.CalculateDamage(
                           target,
                           DamageType.Physical,
                           new double[] { 75, 175, 275 }[Spells.R.Level - 1]
                           + ObjectManager.Player.FlatPhysicalDamageMod * 1f
                           + target.Health * .15f)
                       : 0d;
        }

        public static double DamageIgnite(Obj_AI_Base target)
        {
            return Spells.ignite.IsReady()
                       ? ObjectManager.Player.CalculateDamage(
                           target,
                           DamageType.True,
                           new double[] { 70, 90, 110, 130, 150,
                               170, 190, 210, 230, 250,
                               270, 290, 310, 330, 350,
                               370, 390, 410 }
                               [ObjectManager.Player.Level - 1])
                       : 0d;
        }

        public static float ComboDamage(Obj_AI_Base target)
        {
            var d = 0d;
            d += DamageQ(target)*3 +
                 DamageE(target) +
                 DamageR(target) +
                 DamageIgnite(target);

            return (float)d;
        }
    }
}
