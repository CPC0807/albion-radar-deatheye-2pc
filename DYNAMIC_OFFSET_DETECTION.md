# Dynamic Mob Offset Detection

## Goal

`MobOffsetDetector` is responsible for deciding whether the current `ao-bin-dumps/mobs.xml` still matches the mob `typeId` values coming from the game. The detector must not silently guess. If the signal is weak or contradictory, the radar now stops at startup.

## Detection Model

The current implementation still uses a single global offset:

```text
rawTypeId = xmlIndex + offset
typeId = rawTypeId - offset
```

To validate that assumption, the detector compares a distributed set of anchor mobs from early, middle, and late sections of `mobs.xml`.

## Anchor Strategy

Anchors are stored as `AnchorMob` entries with:

- `UniqueName`
- `ExpectedIndex`
- human-readable description

Current anchors:

| UniqueName | ExpectedIndex | Description |
| --- | ---: | --- |
| `T3_MOB_TR_HERETIC_MAGE_BOSS` | 0 | Early tracking boss |
| `T4_MOB_TR_HERETIC_SHADOWMASK_SUMMON` | 3 | Early summon |
| `T4_MOB_TR_SILVER_KEEPER_EARTHDAUGHTER_BOSS` | 9 | Early keeper boss |
| `T5_MOB_TREASURE_BEAR` | 402 | Early harvestable mob |
| `T7_MOB_ROAMING_HERETIC_THIEF` | 1000 | Mid roaming mob |
| `T8_MOB_UNDEAD_SPECIAL_PULLER_VETERAN` | 2000 | Late-mid veteran mob |
| `T5_MOB_MD_UNDEAD_SKELETON_MELEE` | 3000 | Dungeon mob |
| `T5_MOB_RD_MORGANA_GIANT` | 4000 | Roads mob |

This wider spread is important because a change after index `402` can no longer hide from detection.

## Success Rules

`DetectOffset(...)` now returns `MobOffsetDetectionResult` instead of a plain integer.

The result contains:

- `Success`
- `DetectedOffset`
- `MatchedAnchors`
- `TotalAnchors`
- `WinningShift`
- `VoteBreakdown`
- `FailureReason`
- `Matches`

Detection only succeeds when all of these conditions are true:

1. At least 4 anchors were found in `mobs.xml`.
2. One shift has a clear majority.
3. The winning shift has at least 70% of matched anchor votes.
4. The detected offset is inside the allowed sanity range.

Detection fails when:

- too few anchors are found
- top vote counts are tied
- anchors disagree too much
- the computed offset is implausible

## Startup Behavior

`Init` now uses fail-fast behavior:

1. Load `mobs.xml`
2. Run `MobOffsetDetector.DetectOffset(...)`
3. If detection fails, call `Diagnostics.ReportFatal(...)` and stop startup
4. If detection succeeds, run `MobOffsetDetector.VerifyOffset(...)`
5. If verification fails, stop startup
6. Only then assign `Init.MobTypeIdOffset`

This keeps the radar from running with an unreliable mob mapping.

## Runtime Protection

`NewMobEvent` still calculates:

```csharp
TypeId = RawTypeId - Init.MobTypeIdOffset;
```

But `MobsHandler.AddMob(...)` now enforces fail-closed behavior:

- if `typeId < 0`, reject the mob
- if `typeId >= mobInfos.Count`, reject the mob
- if the lookup fails, reject the mob

Rejected mobs are not added with `null` metadata anymore. The log now includes:

- raw typeId
- calculated typeId
- loaded mob count
- active offset

## Expected Logs

Successful startup:

```text
[MobOffsetDetector] Starting offset detection...
[MobOffsetDetector] Anchor: T5_MOB_RD_MORGANA_GIANT (Roads mob)
  Expected index: 4000, Actual index: 4000, Shift: 0
[MobOffsetDetector] Detected Offset: 16
[MobOffsetDetector] Summary: success=True, matched=8/8, winningShift=0, detectedOffset=16, votes=[0:8]
[MobOffsetDetector] Verifying offset 16...
[MobOffsetDetector] Verification passed.
```

Failed startup:

```text
[MobOffsetDetector] Detection failed: Winning shift 0 only has 3/5 votes (60%); need at least 70%.
```

Then the app shows a fatal message and exits.

Runtime rejection:

```text
[MobsHandler] ERROR: Rejected mob because calculated typeId is out of range.
  Raw typeId: 99999
  Calculated typeId: 99983
  Loaded mob count: 4530
  Active offset: 16
```

## Limitations

This is still a single-offset model. It protects against weak or inconsistent data, but it does not solve a future format where `typeId` mapping is no longer representable as one global offset.
