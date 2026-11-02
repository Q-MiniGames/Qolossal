# Sound check before the calm pass (12 Oct 2026, read-only)

Nothing was changed. Sources: `Assets/Audio/Resources/SfxLibrary.asset` (last written 10 Oct, the second selection),
`Assets/Scripts/Audio/Sfx.cs`, `Assets/Audio/Qolossal.mixer`, `Tools/Audio/sfx_mapping.py`.

## What is on and off now

- **83 sounds play, 53 are silent** (every take Rejected; hooks still fire, `Sfx` refuses them quietly).
- Silent: all footsteps, Jump, Land_Hard, LedgeGrab, Climb, Glide_Loop, all thread sounds, Hurt, Death, Respawn, Sit;
  Staff swing, Spear thrust, Sling shot, ChargeUp; Thornwing rear/dive, Spitter swell/spit, Shellback walk,
  Guardian arm raise/hurt; most world props (thorns, platforms, plates, springs, flowers, moving platform);
  all 10 area ambience beds; UI_Pause/Unpause, Chart_PieceAdd.
- Still playing, by bus: Player 3 (Dash, Glide_Open, Land_Soft), Combat 13 (weapon hits, Mace/Reedblade swings,
  blocks, pogo, enemy defeat), Enemy 17 (incl. loops: GustMoth wings, Thornwing hover, Grub burrow, Newt ripple),
  World 12, Pickup 14, Town 10, UI 7, Story 4, Ambience 3 (Night_Hum, Water_Stream, Waterfall).

## How they are played

| Thing | Now | Why it can annoy |
|---|---|---|
| Takes per sound | 1 for 79 of 83 (Door_Enter, HeartSeed_Get, ChargeRelease have 2) | The same take every time is the main repetition tell |
| Pitch spread | 0-6% (UI, Story, Ambience 0%; World 3%; Combat/Enemy 5%) | Too small to hide one repeated take |
| Volume spread | none in code (only the fixed per-take gain) | Every repeat lands at the same level |
| Length | most 0.48-1.0 s; long: Quake_Stir 6 s, Guardian_Defeat 4 s, Knot_Idle loop 4 s, Relic_Get 3 s, Smithy_Upgrade 2.5 s, Waymark 2 s | Tails ring on after the action |
| Loudness targets | Combat and Story -20 dBFS, Enemy/World/Pickup -22, Player/Town -24, UI -26, steps were -35 | Combat/Enemy sit loud against the calm music |
| Per-take gain | many quiet takes lifted +10 to +38 dB (Staff_Hit +25, Pogo +25, GustMoth_Wings +37, Night_Hum +39) | Lifting a quiet take lifts its hiss and harsh top end too |
| Mixer | groups Effects, UI, Ambience boosted +12 dB headroom; only Attenuation effects | No low-pass, no compressor, no ducking under music |
| Spam control | per-sound cooldown 0.03-0.15 s, 2-3 voices | Fine for most; loops on several enemies at once can stack |

The gain values above 1x are multiplied before `Mathf.Clamp01`, so anything beyond the mixer's +12 dB is cut off at full
volume (inferred from `Sfx.cs:235`); those takes play at their maximum.

## Recommended plan (for the user to agree tomorrow)

1. **Mix first (no new audio, fully reversible):** lower the SFX buses against music (start Combat/Enemy -6 dB,
   World/Pickup -4 dB), add a gentle low-pass (about 8-10 kHz) and a light compressor on Effects, and duck music
   only for Story moments. Cap per-take lift at about +12 dB.
2. **Randomise every play:** pitch ±5% and volume ±10% for all short sounds (UI ±2% pitch), and avoid repeating the last take
   (already in code).
3. **Trim tails:** play short sounds through a fade cap (like the footstep's `PlayShort`), around 0.4 s for swings/hits,
   1-1.5 s for pickups; shorten Guardian_Defeat, Relic_Get, Quake_Stir.
4. **New takes where it matters, in calm style:** 4-6 short, dry, soft variations for the most frequent sounds:
   footstep (one surface first), jump, land, weapon swings, weapon hits, Amber_Pickup, UI_Move/Confirm.
   Prompts: soft, warm, wooden/leafy, no reverb, under 0.3 s.
5. **Bring sounds back one group at a time** (Qori movement, then combat, then enemies, then ambience), the user
   auditioning each in play before the next.

Step 1-3 need only code and mixer edits; step 4 needs new ElevenLabs takes and the user's audition.
