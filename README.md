# Fog of Pawn – RimWorld 1.6

Fog-of-Pawn obscures some details of newly-joined pawns so the player doesn't have perfect knowledge on day-one.  As the colony interacts with the pawn, the fog gradually lifts – creating emergent stories like *"I hired a doctor... turns out he can't even suture a scratch!"*  

---
## Current Feature Matrix
| Area | Status |
|------|--------|
| **CompPawnFog** side-car (save/load) | ✅ stable |
| Skills fog + swap-during-draw patch | ✅ stable |
| Settings UI (sliders, toggles) | ✅ stable |
| Dev gizmos (print profile, tier setters, XP boost) | ✅ stable |
| Tiered deception system (Truthful / Slight / Scammer / Sleeper) | ✅ |
| Social-interaction & passive time reveal | ✅ |
| Grammar-based reveal messages | ✅ |
| Trait concealment | ✅ new |
| Narrative hooks (mood buffs, special letters, fun events) | 🚧 stubs |

---
## Deception Tiers (v2)
1. **Truthful** – ~90 % of pawns (slider adjustable)  
   • All skills & passions reported accurately.
2. **Slightly-Deceived** – up to 3 random skills are exaggerated or understated.  
   • Passions can be faked for those skills.  
   • All other skills start fully revealed.
3. **Deceiver – Scammer**  
   • Low-value pawn (< 200 skill score) reports 8–12 in every low skill, faking competence.  
4. **Deceiver – Sleeper**  
   • High-value pawn (> 300 skill score) sandbags their elite skills, reporting 3–6 with no passions.

Deceiver tier is restricted to pawns that *join* the player (wanderers, refugees, quests) unless the toggle is disabled.

---
## Developer Utilities
* Dev-mode gizmos on each pawn:  
  • Print Deception Profile  
  • Set Tier → Truthful / Slight / Scammer / Sleeper (with pawn-value validation)  
  • +XP Shooting (forces reveal check)
* Console logs follow `[FogOfPawn REFLECT|FAIL|DEBUG|PROFILE]` tags – only in DevMode or with `VERBOSE_LOG` symbol.

---
## Build / Test
```
# Windows PowerShell
./build.ps1            # compile + sync DLL to ./1.6/Assemblies
```
Launch RimWorld with Dev mode on, spawn pawns, and use the gizmos or settings sliders to test.

---
## Roadmap (Next Milestones)
2. **Narrative Hooks**  
   • `OnScammerRemoved` – +10 colony mood thought when scammer dies/exiled.  
   • `OnSleeperFullyRevealed` – blue letter announcing secret past.  
4. **Polish & Release (M7)**  
   • Localization stubs for new strings.  
   • Compatibility passes (RIMHUD, Character Editor).  
   • Steam Workshop page.

---
### Contributing
Pull requests welcome – please follow the logging guideline in `code-guideline.txt` and keep reflection look-ups gated behind `[REFLECT]` traces. 

Trait concealment patch – implemented (swap-during-draw + tooltip mask).
Narrative hooks:  