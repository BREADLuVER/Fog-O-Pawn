This is a cheatsheet for playing the Fog-O-Pawn mod. It is based on a review of the mod's source code and is intended to be a concise gameplay guide.

### The Core Mechanic: The Fog of Pawn
The central idea is that your colonists (and others) are not always what they seem. Key attributes can be hidden, only to be revealed over time through various events. This affects:
- **Skills & Passions**: A pawn might report a skill of 5 but have a real skill of 15, or vice-versa. They can also hide their passions.
- **Traits**: Abrasive? Psychopath? You won't know until their true character is revealed.
- **Health & Genes**: Certain health conditions and a pawn's genetic makeup are hidden.

There are four types of pawns:
1.  **Truthful**: WYSIWYG. Most pawns are this type.
2.  **Slightly Deceived**: Minor, often inconsequential, discrepancies in their skills.
3.  **Deceiver Imposter**: A pawn with deliberately faked low-tier skills. They are a liability who can be unmasked.
4.  **Deceiver Sleeper**: A highly-skilled pawn masquerading as someone else, with a hidden agenda that unfolds through a multi-stage story.

### How to Reveal the Truth
You don't need to do anything special to start revealing information; it happens organically. Keep an eye out for letters and notifications. The primary reveal mechanics are:

- **Time & Mood**: The longer a pawn is with you, the more likely a trait or skill will be revealed. This chance increases if the pawn is in a very good mood.
- **Skill Use**: When a pawn gains enough experience in a skill, their true skill level may be revealed.
- **Social Interaction**: Pawns chatting with each other can also lead to reveals.

For **Imposters** and **Sleepers**, any single reveal will trigger a "cascade," exposing all of their hidden attributes at once in a dramatic fashion.

### Dealing with Imposters
Imposters are pawns of low value who are pretending to be more useful than they are.
- **The Reveal**: When unmasked, they receive a permanent **-15 social debuff** with all colonists ("Damaged Reputation"). Their social impact is also permanently lowered.
- **The Payoff**: They will drop a **Disguise Kit** item upon being revealed. This also removes a hidden wealth penalty they carried.

### Dealing with Sleepers: The Storyline
Sleepers are the core of the mod's story. They are valuable, highly skilled pawns whose true nature is revealed through a specific incident chain.

**Important:** Sleepers are **NOT** revealed by taking damage. This was an old feature that has been removed. Their story is now purely event-driven.

1.  **The Incident**: At some point, an incident will fire for a sleeper in your colony, kicking off their story.
2.  **The Buildup**: Over the next 1-2 seasons, you will receive letters about strange happenings related to the pawn (e.g., "Suspicion", "Anomaly"). These are story beats and require no action.
3.  **The Ascension & Choice**: After the buildup, the pawn's true, high-level skills are revealed. They also gain a powerful **positive trait** (like Tough, Iron-willed, or Bloodlust) in an event called the "Ascension". You will then get a choice:
    - **Keep Them**: You accept their new identity. The sleeper is integrated into the colony, and everyone gets a mood buff for trusting them. This is how you gain a very powerful pawn.
    - **Attempt to Capture**: The sleeper turns hostile and immediately goes berserk. You must fight and imprison them.
    - **Exile Them**: You banish them from the colony.

### Mechanics NOT in the Mod
To avoid confusion with older versions or other mods, be aware of what's *not* a feature:
- **No Hidden Addictions**: The mod does not hide addictions.
- **No Reveal on Damage**: Taking damage or being downed will not reveal a Sleeper.

# Key Fixes
1.  **Special Joiner Timing**: Adjusted the incident scheduler (`GameComponent_FogTracker.cs`) to ensure the Sleeper/Imposter joiner events only start after Day 60 (1 year), with a guaranteed event by Day 90 (1.5 years). This prevents the "too early" and "too frequent" reports.

2.  **Missing Translations**: Added the missing Chinese and English text for the special joiner event. Players will now see a proper, translated message ("Suspicious Joiner") instead of a code tag.

3.  **Build Script**: Ran the `build.ps1` script to compile the changes into the DLL for version 1.6.

# Next Steps
The changes above should resolve the bugs your players were seeing.

The other piece of feedback was a feature request: **allowing players to refuse the special joiner**. I can implement this next if you'd like. It would involve changing the event to show a choice letter with "Accept" and "Refuse" buttons instead of the pawn joining automatically.

Let me know if you want to proceed with that change! 