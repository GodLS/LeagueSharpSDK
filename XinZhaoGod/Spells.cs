using LeagueSharp;
using LeagueSharp.SDK.Core.Extensions;
using LeagueSharp.SDK.Core.Wrappers;

namespace XinZhaoGod
{
    class Spells
    {
        private static Spell _Q, _W, _E, _R;
        private static SpellSlot _ignite;

        public static Spell Q { get { return _Q; } }
        public static Spell W { get { return _W; } }
        public static Spell E { get { return _E; } }
        public static Spell R { get { return _R; } }
        public static SpellSlot ignite { get { return _ignite; } }

        public static void Initialize()
        {
            _Q = new Spell(SpellSlot.Q);
            _W = new Spell(SpellSlot.W);
            _E = new Spell(SpellSlot.E, 600);
            _R = new Spell(SpellSlot.R, 500);

            _ignite = ObjectManager.Player.GetSpellSlot("summonerdot");

        }
    }
}
