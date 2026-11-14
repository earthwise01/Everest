using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.InlineRT;
using MonoMod.Utils;
using System;

namespace Celeste {
    class patch_Puffer : Puffer {
        public patch_Puffer(EntityData data, Vector2 offset) : base(data, offset) {
            // ignored by MonoMod
        }

        [MonoModLinkTo("Monocle.Entity", "Added")]
        [MonoModIgnore]
        public extern void base_Added(Scene scene);

        public override void Added(Scene scene) {
            base_Added(scene);
            if (Depth == 0 && ((patch_AreaKey) (object) (scene as Level).Session.Area).LevelSet != "Celeste") {
                Depth = -1; // makes puffer boosts work after player respawn or other depth reset
            }
        }

        [MonoModIgnore] // We don't want to change anything about the method...
        [PatchPufferExplode] // ... except for manually manipulating the method via MonoModRules
        public extern void Explode();
    }
}

namespace MonoMod {
    /// <summary>
    /// Patch puffer explosions to handle explosion interactions with <see cref="ExplosionCollider"/> components.
    /// </summary>
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchPufferExplode))]
    class PatchPufferExplodeAttribute : Attribute { }

    static partial class MonoModRules {
        public static void PatchPufferExplode(ILContext context, CustomAttribute attrib) { 
            TypeDefinition t_ExplosionCollider = MonoModRule.Modder.Module.GetType("Celeste.Mod.Entities.ExplosionCollider");
            MethodDefinition m_ExplosionCollider_TriggerExplosionColliders = t_ExplosionCollider.FindMethod("System.Void TriggerExplosionColliders(Monocle.Entity)");
            
            ILCursor cursor = new(context);

            // jump to just before the collider is reset to normal
            cursor.GotoNext(MoveType.Before,
                instr => instr.MatchLdarg0(),
                instr => instr.MatchLdloc0(),
                instr => instr.MatchCall("Monocle.Entity", "set_Collider"));
            cursor.MoveAfterLabels();
            
            // inject call to ExplosionCollider.TriggerExplosionColliders
            cursor.EmitLdarg0();
            cursor.EmitCall(m_ExplosionCollider_TriggerExplosionColliders);
        }
    }
}