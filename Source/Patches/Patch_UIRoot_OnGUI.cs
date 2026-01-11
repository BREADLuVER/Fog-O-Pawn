using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Master RenderContext activation patch.
    /// We patch Root.OnGUI which is the Unity entry point for ALL UI rendering.
    /// This ensures RenderContext.IsRendering is true for:
    /// - Map gizmos (SimpleSidearms, etc.)
    /// - Inspect pane (RimHUD, etc.)
    /// - Windows
    /// - Everything else in the UI
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_Root_OnGUI
    {
        private static bool _firstRenderLogged = false;
        
        static Patch_Root_OnGUI()
        {
            try
            {
                var harmony = new Harmony("FogOfPawn.RootOnGUI");
                
                // Patch Root.OnGUI - this is the Unity OnGUI entry point
                Type rootType = typeof(Root);
                var onGUIMethod = AccessTools.Method(rootType, "OnGUI");
                
                if (onGUIMethod != null)
                {
                    harmony.Patch(onGUIMethod,
                        prefix: new HarmonyMethod(typeof(Patch_Root_OnGUI), nameof(Prefix)),
                        finalizer: new HarmonyMethod(typeof(Patch_Root_OnGUI), nameof(Finalizer)));
                    
                    Log.Message("[FogOfPawn] Root.OnGUI successfully patched - RenderContext will be active for ALL UI!");
                }
                else
                {
                    Log.Warning("[FogOfPawn] Could not find Root.OnGUI method!");
                }
                
                // Also patch UIRoot_Play.UIRootOnGUI as backup for in-game UI
                Type uiRootPlayType = AccessTools.TypeByName("RimWorld.UIRoot_Play");
                if (uiRootPlayType != null)
                {
                    var uiRootOnGUIMethod = AccessTools.Method(uiRootPlayType, "UIRootOnGUI");
                    if (uiRootOnGUIMethod != null)
                    {
                        harmony.Patch(uiRootOnGUIMethod,
                            prefix: new HarmonyMethod(typeof(Patch_Root_OnGUI), nameof(Prefix)),
                            finalizer: new HarmonyMethod(typeof(Patch_Root_OnGUI), nameof(Finalizer)));
                        
                        Log.Message("[FogOfPawn] UIRoot_Play.UIRootOnGUI patched as backup");
                    }
                }
                
                // Patch GizmoGridDrawer as an additional safety measure for gizmos (SimpleSidearms, etc.)
                Type gizmoDrawerType = typeof(GizmoGridDrawer);
                var drawGizmoGridMethod = AccessTools.Method(gizmoDrawerType, "DrawGizmoGrid");
                if (drawGizmoGridMethod != null)
                {
                    harmony.Patch(drawGizmoGridMethod,
                        prefix: new HarmonyMethod(typeof(Patch_Root_OnGUI), nameof(Prefix)),
                        finalizer: new HarmonyMethod(typeof(Patch_Root_OnGUI), nameof(Finalizer)));
                    Log.Message("[FogOfPawn] GizmoGridDrawer.DrawGizmoGrid patched for extra RenderContext safety");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[FogOfPawn] Failed to patch Root.OnGUI: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        public static void Prefix()
        {
            RenderContext.BeginRender();
            
            // Log first time to confirm patch is working
            if (!_firstRenderLogged)
            {
                _firstRenderLogged = true;
                Log.Message($"[FogOfPawn] Root.OnGUI Prefix RUNNING! RenderContext.IsRendering={RenderContext.IsRendering}");
            }
        }
        
        public static void Finalizer()
        {
            RenderContext.EndRender();
        }
    }
    
    
    /// <summary>
    /// Additional patches for specific UI areas that may need extra coverage.
    /// These add redundant RenderContext activation for safety.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_CharacterCard_Dynamic
    {
        static Patch_CharacterCard_Dynamic()
        {
            try
            {
                var harmony = new Harmony("FogOfPawn.CharacterCard");
                
                // Try to find CharacterCardUtility - it handles pawn bio display
                Type cardType = AccessTools.TypeByName("RimWorld.CharacterCardUtility") 
                             ?? AccessTools.TypeByName("CharacterCardUtility");
                
                if (cardType != null)
                {
                    var drawMethod = AccessTools.Method(cardType, "DrawCharacterCard");
                    if (drawMethod != null)
                    {
                        harmony.Patch(drawMethod,
                            prefix: new HarmonyMethod(typeof(Patch_CharacterCard_Dynamic), nameof(Prefix)),
                            finalizer: new HarmonyMethod(typeof(Patch_CharacterCard_Dynamic), nameof(Finalizer)));
                        Log.Message("[FogOfPawn] CharacterCardUtility.DrawCharacterCard patched for RenderContext");
                    }
                }
                
                // Also try SkillUI
                Type skillUIType = AccessTools.TypeByName("RimWorld.SkillUI") 
                                ?? AccessTools.TypeByName("SkillUI");
                
                if (skillUIType != null)
                {
                    // Find any DrawSkills method
                    var methods = skillUIType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        if (method.Name.StartsWith("DrawSkill"))
                        {
                            harmony.Patch(method,
                                prefix: new HarmonyMethod(typeof(Patch_CharacterCard_Dynamic), nameof(Prefix)),
                                finalizer: new HarmonyMethod(typeof(Patch_CharacterCard_Dynamic), nameof(Finalizer)));
                            Log.Message($"[FogOfPawn] {skillUIType.Name}.{method.Name} patched for RenderContext");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FogOfPawn] Failed to patch CharacterCard: {ex.Message}");
            }
        }
        
        static void Prefix()
        {
            RenderContext.BeginRender();
        }
        
        static void Finalizer()
        {
            RenderContext.EndRender();
        }
    }
}
