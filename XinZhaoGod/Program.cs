using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.SDK.Core;
using LeagueSharp.SDK.Core.Enumerations;
using LeagueSharp.SDK.Core.Events;
using LeagueSharp.SDK.Core.Extensions;
using LeagueSharp.SDK.Core.UI.IMenu;
using LeagueSharp.SDK.Core.UI.IMenu.Values;
using LeagueSharp.SDK.Core.Utils;
using LeagueSharp.SDK.Core.Wrappers;
using SharpDX.IO;

namespace XinZhaoGod
{
    internal class Program
    {
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        private static Menu _config;

        private static void Main(string[] args)
        {
            Load.OnLoad += OnLoad;
            Bootstrap.Init(args);
        }

        private static void OnLoad(object sender, EventArgs e)
        {
            if (Player.ChampionName != "XinZhao")
                return;

            Spells.Initialize();
            InitMenu();

            Game.OnUpdate += OnUpdate;
            Orbwalker.OnAction += OnAction;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnAction(object sender, Orbwalker.OrbwalkerActionArgs e)
        {
            if (e.Target == null) return;
            if (e.Target.Type != GameObjectType.obj_AI_Hero || e.Target.Type != GameObjectType.obj_AI_Minion) return;
            var target = (Obj_AI_Base) e.Target;
            {
                if (e.Type == OrbwalkerType.BeforeAttack)
                {
                    if (target == null) return;
                    if (_config["Combo"]["ComboW"].GetValue<MenuBool>().Value && Spells.W.IsReady() &&
                        Orbwalker.ActiveMode == OrbwalkerMode.Orbwalk ||
                        _config["Harass"]["HarassW"].GetValue<MenuBool>().Value && Spells.W.IsReady() &&
                        Orbwalker.ActiveMode == OrbwalkerMode.Hybrid && target.Type == GameObjectType.obj_AI_Hero ||
                        _config["LaneClear"]["LaneClearW"].GetValue<MenuBool>().Value && Spells.W.IsReady() &&
                        target.Type == GameObjectType.obj_AI_Minion)
                        Spells.W.Cast();

                    if (_config["Killsteal"]["KillstealQ"].GetValue<MenuBool>().Value &&
                        Spells.Q.IsReady() && target.Type != GameObjectType.obj_AI_Minion &&
                        Damage.DamageQ(target) >= target.Health)
                        Spells.Q.Cast();
                }
            }

            if (e.Type != OrbwalkerType.OnAttack) return;
            if (target == null) return;
            if ((!_config["Combo"]["ComboQ"].GetValue<MenuBool>().Value || !Spells.Q.IsReady() ||
                 Orbwalker.ActiveMode != OrbwalkerMode.Orbwalk) &&
                (!_config["Harass"]["HarassQ"].GetValue<MenuBool>().Value || !Spells.Q.IsReady() ||
                 Orbwalker.ActiveMode != OrbwalkerMode.Hybrid || target.Type != GameObjectType.obj_AI_Hero) &&
                (!_config["LaneClear"]["LaneClearQ"].GetValue<MenuBool>().Value || !Spells.Q.IsReady() ||
                 Orbwalker.ActiveMode != OrbwalkerMode.LaneClear)) return;
            var aaDelay = Player.AttackDelay*100 + Game.Ping/2f;
            DelayAction.Add(aaDelay, () => Spells.Q.Cast());
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            Killsteal();

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Orbwalk:
                    Combo();
                    break;
                case OrbwalkerMode.Hybrid:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Spells.E.Range);
            if (target == null)
                return;

            if (!target.IsValidTarget()) return;
            if (target.HasBuff("xenzhaointimidate") && Spells.R.IsReady())
            {
                if (_config["Combo"]["ComboR"].GetValue<MenuBool>().Value)
                    Spells.R.Cast();

                else if (_config["Combo"]["ComboRKillable"].GetValue<MenuBool>().Value &&
                         Damage.ComboDamage(target) >= target.Health)
                    Spells.R.Cast();

                if (_config["Combo"]["ComboRAoE"].GetValue<MenuBool>().Value)
                {
                    var enemies =
                        GameObjects.EnemyHeroes.Where(
                            e => e.IsValidTarget() && Player.Distance(e.Position) <= Spells.R.Range);

                    if (enemies.Count() >= _config["Combo"]["ComboMinR"].GetValue<MenuSlider>().Value)
                        Spells.R.Cast();
                }
            }

            if (_config["Combo"]["ComboE"].GetValue<MenuBool>().Value && Spells.E.IsReady() && target.IsValidTarget() &&
                Player.Distance(target) >= _config["Combo"]["ComboEDistance"].GetValue<MenuSlider>().Value)
                Spells.E.CastOnUnit(target);
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Spells.E.Range);
            if (target == null)
                return;

            if (_config["Harass"]["ComboE"].GetValue<MenuBool>().Value && Spells.E.IsReady() && target.IsValidTarget() &&
                Player.Distance(target) >= _config["Harass"]["HarassEDistance"].GetValue<MenuSlider>().Value)
                Spells.E.CastOnUnit(target);
        }

        private static void LaneClear()
        {
            var minions =
                GameObjects.EnemyMinions.FirstOrDefault(m => m.IsValid && Player.Distance(m.Position) <= Spells.E.Range);

            if (minions == null)
                return;

            if (_config["LaneClear"]["LaneClearE"].GetValue<MenuBool>().Value && Spells.E.IsReady())
                Spells.E.CastOnUnit(minions);
        }

        private static void JungleClear()
        {
            var minions =
                GameObjects.Jungle.FirstOrDefault(m => m.IsValid && Player.Distance(m.Position) <= Spells.E.Range);

            if (minions == null)
                return;

            if (_config["LaneClear"]["LaneClearE"].GetValue<MenuBool>().Value && Spells.E.IsReady())
                Spells.E.CastOnUnit(minions);
        }

        private static void Killsteal()
        {
            foreach (var enemy in
                GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(Spells.E.Range)))
            {
                if (_config["Killsteal"]["KillstealE"].GetValue<MenuBool>().Value &&
                    Damage.DamageE(enemy) >= enemy.Health)
                {
                    Spells.E.CastOnUnit(enemy);
                }

                else if (!_config["Killsteal"]["KillstealR"].GetValue<MenuBool>().Value ||
                         !(Player.Distance(enemy) <= Spells.R.Range) ||
                         Damage.DamageR(enemy) <= enemy.Health) return;
                Spells.R.Cast();
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;

            if (_config["Drawings"]["DrawingsE"].GetValue<MenuBool>().Value)
            {
                Drawing.DrawCircle(Player.Position, Spells.E.Range, Color.Coral);
            }

            if (_config["Drawings"]["DrawingsE2"].GetValue<MenuBool>().Value)
            {
                Drawing.DrawCircle(Player.Position, _config["Combo"]["ComboEDistance"].GetValue<MenuSlider>().Value, Color.LightCoral);
            }

            if (_config["Drawings"]["DrawingsR"].GetValue<MenuBool>().Value)
            {
                Drawing.DrawCircle(Player.Position, Spells.R.Range, Color.Coral);
            }

            if (!_config["Drawings"]["DrawingsP"].GetValue<MenuBool>().Value) return;
            foreach (var enemy in
                GameObjects.EnemyHeroes.Where(e => e.HasBuff("xinzhaointimidate")))
            {
                Drawing.DrawCircle(enemy.Position, 100, Color.Yellow);
            }

        }

        private static void InitMenu()
        {
            _config = new Menu("XinZhaoGod", "Xin Zhao God", true).Attach();

            var comboMenu = new Menu("Combo", "Combo settings");
            {
                comboMenu.Add(new MenuBool("ComboQ", "Use Q", true));
                comboMenu.Add(new MenuBool("ComboW", "Use W", true));
                comboMenu.Add(new MenuBool("ComboE", "Use E", true));
                comboMenu.Add(new MenuSlider("ComboEDistance", "E minimum distance", 350, 1, 600));
                comboMenu.Add(new MenuBool("ComboR", "Use R Always", true));
                comboMenu.Add(new MenuBool("ComboRKillable", "Use R Killable", true));
                comboMenu.Add(new MenuBool("ComboRAoE", "Use R AoE ", true));
                comboMenu.Add(new MenuSlider("ComboMinR", "Minimum targets to R", 3, 1, 5));
                _config.Add(comboMenu);
            }

            var harassMenu = new Menu("Harass", "Harass settings");
            {
                harassMenu.Add(new MenuBool("HarassQ", "Use Q", true));
                harassMenu.Add(new MenuBool("HarassW", "Use W", true));
                harassMenu.Add(new MenuBool("HarassE", "Use E", true));
                harassMenu.Add(new MenuSlider("HarassEDistance", "E minimum distance", 350, 1, 600));
                _config.Add(harassMenu);
            }

            var laneClearMenu = new Menu("LaneClear", "LaneClear settings", true);
            {
                laneClearMenu.Add(new MenuBool("LaneClearQ", "LaneClear Q", true));
                laneClearMenu.Add(new MenuBool("LaneClearW", "LaneClear W", true));
                laneClearMenu.Add(new MenuBool("LaneClearE", "LaneClear E", true));
                _config.Add(laneClearMenu);
            }

            var killstealMenu = new Menu("Killsteal", "Killsteal settings", true);
            {
                killstealMenu.Add(new MenuBool("KillstealQ", "Killsteal Q", true));
                killstealMenu.Add(new MenuBool("KillstealE", "Killsteal E", true));
                killstealMenu.Add(new MenuBool("KillstealR", "Killsteal R", false));
                _config.Add(killstealMenu);
            }

            var drawingMenu = new Menu("Drawings", "Drawing settings", true);
            {
                drawingMenu.Add(new MenuBool("DrawingsE", "Draw E", true));
                drawingMenu.Add(new MenuBool("DrawingsE2", "Draw E Min.", true));
                drawingMenu.Add(new MenuBool("DrawingsR", "Draw R", true));
                drawingMenu.Add(new MenuBool("DrawingsP", "Draw Challenged", true));
                _config.Add(drawingMenu);
            }
        }
    }
}