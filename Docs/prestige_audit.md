# Prestige Audit - Passport-Based Prestige

## Executive summary

Idle Airport has the core technical hooks needed to implement a passport-based prestige system without a broad rewrite. The safest semantic definition for "passport scanned" is "a passenger successfully processed by security", which currently happens in `PassengerProcessor.CompleteProcessedPassenger()` after the passenger enters `WaitingRoomUIController` and before economy reward is granted.

Recommended integration points:

- Count scanned passports after `CompleteProcessedPassenger()` returns `true`, using both `OnPassengerManuallyProcessed` and `OnPassengerAutoProcessed` unless design explicitly limits prestige to manual scans.
- Apply the global x2 prestige income multiplier in `EconomyController.RewardProcessedPassenger(double totalReward)` before `AddMoney(totalReward)`.
- Adapt the existing `PassengersBlock` / `PassengerCounterText` HUD area, currently driven by `IdleAirportUIController._passengersProcessedText`, to show `[passport icon] currentPassports / requiredPassports`.
- Add future reset support to economy, AI TSA scanner upgrades, stores, queue/scanner held passengers, and waiting room.
- Keep the extra manual scanner visual-only for the first implementation; duplicating a functional manual scanner would require `PassengerProcessor` changes because it currently holds a single `_manualScanner` reference.
- Increase the boarding area visually through `WaitingRoomUIController` sizing/grid configuration and/or the `P_BoardingArea`/`BoardingLoungeSection` `RectTransform`, not by `localScale`.

No prestige blocker was found, but several reset APIs are missing and should be added before implementation.

## Current prestige readiness

The project is ready for a modular prestige layer if the implementation adds small public reset/apply methods to existing systems. There is no current save/load layer, so prestige persistence will need a new persistence decision later.

The current project does not already have a `PassengerProcessed`, `PassengerScanned`, `PassengerCleared`, or prestige-specific event name. It does have two usable passenger success events:

- `PassengerProcessor.OnPassengerManuallyProcessed`
- `PassengerProcessor.OnPassengerAutoProcessed`

These are emitted only after successful security processing.

## Relevant files found

- `Assets/Scripts/GameCore/PassengerProcessor.cs`
- `Assets/Scripts/GameCore/EconomyController.cs`
- `Assets/Scripts/GameCore/IdleAirportUIController.cs`
- `Assets/Scripts/GameCore/AITSAScannerUpgrade.cs`
- `Assets/Scripts/GameCore/ScannerStationUIController.cs`
- `Assets/Scripts/GameCore/PassengerQueueUIController.cs`
- `Assets/Scripts/GameCore/WaitingRoomUIController.cs`
- `Assets/Scripts/GameCore/StoresManager.cs`
- `Assets/Scripts/GameCore/Store.cs`
- `Assets/Scripts/GameCore/PassengerPool.cs`
- `Assets/Scripts/GameCore/ScannerFeedbackView.cs`
- `Assets/Scripts/GameCore/ShopIncomeFeedbackPresenter.cs`
- `Assets/Scripts/GameCore/BoardingFeedbackView.cs`
- `Assets/Scenes/GameScene.unity`
- `Assets/Prefabs/Scanners/P_ManualScanner.prefab`
- `Assets/Prefabs/Scanners/PV_AutoScanner.prefab`
- `Assets/Prefabs/Areas/P_BoardingArea.prefab`
- `Assets/Prefabs/P_ScannerButton.prefab`

## Passenger flow audit

Manual processing is controlled by `PassengerProcessor.TryManualScan()` in `Assets/Scripts/GameCore/PassengerProcessor.cs`. The scan button is handled by `IdleAirportUIController.OnScannerClicked()`, which calls `_passengerProcessor.TryManualScan()`.

AI scanner processing exists. It is controlled by:

- `PassengerProcessor.Update()`, which calls `TryProcessOnePassenger(_aiScanner)` when `CanAutoProcessAIScanner` is true.
- `ScannerStationUIController.TryStartAutoProcessing(...)`, which runs the timed auto processing coroutine.
- `PassengerProcessor.OnAutoScannerCompleted(...)`, which completes the passenger after the auto scan finishes.
- `AITSAScannerUpgrade`, which controls AI scanner ownership, duration, effective scanner count, tokens, and durability.

The exact success point is `PassengerProcessor.CompleteProcessedPassenger(...)`. A passenger is only successful when `enteredWaitingRoom` is true via one of:

- `_waitingRoom.TryReceivePassengerWithReservationImmediate(passenger)`
- `_waitingRoom.TryReceivePassengerWithReservation(passenger)`
- `_waitingRoom.TryReceivePassengerImmediate(passenger)`
- `_waitingRoom.TryReceivePassenger(passenger)`

After that, `CompleteProcessedPassenger()` calls `RewardProcessedPassenger()`, which delegates to `EconomyController.RewardProcessedPassenger(reward.TotalReward)`.

Recommended future passport count point:

- Best service-level hook: subscribe to both `OnPassengerManuallyProcessed` and `OnPassengerAutoProcessed`.
- Best new event location if a single event is added later: invoke `OnPassengerProcessed` inside `CompleteProcessedPassenger()` immediately after `RewardProcessedPassenger()` succeeds, or directly after `enteredWaitingRoom` and reward calculation if the event should include reward data.

Counting recommendation:

- Count any passenger processed by security, manual or AI. This matches the requirement "pasaportes escaneados = pasajeros procesados exitosamente por seguridad".
- Only count manual scanner if design explicitly wants prestige to reward clicks rather than airport throughput. That would be a design constraint, not a technical necessity.

## UI audit

The current processed-passenger UI is in `Assets/Scenes/GameScene.unity`:

- `PassengersBlock`
- `PassengerCounterText`

The updating script is `IdleAirportUIController`:

- Serialized field: `_passengersProcessedText`
- Event subscription: `_economyController.OnTotalPassengersProcessedChanged += OnPassengersChanged`
- Update method: `OnPassengersChanged(int passengers)` writes `NumberFormatter.Format(passengers)` into `_passengersProcessedText`.

Components:

- `PassengerCounterText` is a `TextMeshProUGUI`.
- The scan button is a `Button` from `P_ScannerButton.prefab`, with `Image` graphics and `ImagePulseAnimUI`.
- Existing icon badge prefabs exist under `Assets/Prefabs/Icons`, but no passport-specific UI prefab was found. There is a passport visual asset at `Assets/Art/Sprites/Misc/T_Passport.png`.

Adaptation feasibility:

- `PassengersBlock` is the natural HUD location for `[passport icon] currentPassports / requiredPassports`.
- The current single TMP text can show `current / required` immediately.
- For an actual icon, add an `Image` child using the passport sprite, or use a TMP sprite asset if one is created later.
- A prestige button/panel can naturally live near the existing passengers HUD or the upgrade/shop cards. The HUD is better for a compact ready state; the upgrade/shop card area is better for a larger confirmation panel.

Do not modify the scene until implementation.

## Economy audit

The unique passenger reward path is:

1. `PassengerProcessor.CompleteProcessedPassenger(...)`
2. `PassengerProcessor.RewardProcessedPassenger()`
3. `EconomyController.RewardProcessedPassenger(double totalReward)`
4. `EconomyController.AddPassengers(1)`
5. `EconomyController.AddMoney(totalReward)`

The global x2 prestige multiplier should be applied in `EconomyController.RewardProcessedPassenger(double totalReward)`, before `AddMoney()`. This keeps passenger count unchanged while multiplying AirBucks income.

Applying the multiplier in one point is viable for current income because scanned passenger income and shop passenger bonus both flow into `reward.TotalReward`, then into `EconomyController.RewardProcessedPassenger()`.

Income bypass check:

- No other `AddMoney(...)` calls were found outside `EconomyController`.
- Stores and AI upgrades spend via `EconomyController.SpendMoney(...)`.
- `StoresManager.GetPassengerIncomeBonus()` contributes to passenger reward through `PassengerProcessor.GetPassengerRewardBreakdown()`, so shop bonus does not bypass the economy reward point.

Risk:

- If future non-passenger income is added through direct `AddMoney()`, it will not receive the passenger prestige multiplier unless the multiplier is moved into `AddMoney()` or income types are introduced.

## Reset audit

Recommended future interface: `IPrestigeResettable` is appropriate for run-state systems. Keep permanent prestige state outside these resettable run systems.

| System | File | Run state to reset | Existing reset? | Future `IPrestigeResettable`? |
| --- | --- | --- | --- | --- |
| Economy | `EconomyController.cs` | `_money`, `_totalPassengersProcessed`, possibly `_moneyPerPassenger` | Yes, `Reset()` sets money/passengers to 0 and income to 1 | Yes, but rename or wrap to avoid confusion with Unity `Reset()` semantics |
| AI TSA scanner | `AITSAScannerUpgrade.cs` | `_ownedCount`, `_currentTokens`, `_tokenPacksPurchased`, `_durabilityUpgradeCount`, `_tokensPerScanner` if durability mutates it | No | Yes |
| Stores | `StoresManager.cs`, `Store.cs` | each store `_ownedCount`, possibly `_isUnlocked` depending baseline unlock design | No | Yes |
| Passenger queue | `PassengerQueueUIController.cs` | `_activeQueue`, `_isInitialized`, active passenger visuals | No | Yes |
| Manual scanner held passengers | `ScannerStationUIController.cs` | `_heldPassengers` | Partial: `RecycleHeldPassengers()` | Yes or called by `PassengerProcessor` reset |
| AI scanner in-progress passenger | `ScannerStationUIController.cs` | `_activeAutoPassenger`, `_isBusy`, coroutine | Partial: `CancelAutoProcessing()` | Yes or called by `PassengerProcessor` reset |
| Passenger processor reservations | `PassengerProcessor.cs` | manual reservations, auto reservation, scanner state coordination | Partial on `OnDisable()` only | Yes |
| Waiting/boarding area | `WaitingRoomUIController.cs` | `_passengers`, `_reservedSlots`, `_boardingTimer`, `_wasFull` | No | Yes |
| Passenger pool | `PassengerPool.cs` | available/in-use passengers, active visuals | Partial through individual `Recycle()`/`Release()` | Usually no; expose helper if queue/waiting reset needs it |
| UI controller | `IdleAirportUIController.cs` | displayed text, button/card states, feedback coroutines | No dedicated reset, but event-driven updates exist | Probably no; refresh after reset |
| Shop visuals | `ShopVisualDisplayController.cs` | instantiated visual shop items | No | Maybe, if visuals are not rebuilt from store state |
| Maintenance/tokens | `AITSAScannerUpgrade.cs` | `_currentTokens`, token pack count, durability-derived cap | No | Covered by AI TSA reset |

Important reset risk:

- Queue, scanners, waiting room, and reservations are interdependent. A future prestige reset should pause processing first, cancel scanner coroutines, recycle active passengers, clear reservations, then rebuild initial queue.

## Manual extra scanner visual audit

Current manual scanner representation:

- Prefab: `Assets/Prefabs/Scanners/P_ManualScanner.prefab`
- Scene instance: `P_ManualScanner` under `SecurityScannersPanel`
- Component: `ScannerStationUIController`
- UI type: `RectTransform` + `Image`, with child `ScannerPointMarker`
- Manual mode: `_isAutoScanner: 0`
- Scene reference: `PassengerProcessor._manualScanner`

There is also an auto scanner scene/prefab variant:

- `Assets/Prefabs/Scanners/PV_AutoScanner.prefab`
- Same base manual scanner prefab with `_isAutoScanner: 1` and AI label additions.

Can an extra scanner be visual-only?

- Yes. The safest low-scope implementation is to place/activate a duplicate visual object that does not get assigned to `PassengerProcessor._manualScanner`.
- It should not have active gameplay logic if it is only visual. Options: remove/disable `ScannerStationUIController` on the visual duplicate, or place it under a visual-only parent and do not reference it from gameplay systems.

Functional duplicate risk:

- `PassengerProcessor` currently has a single `_manualScanner` field and refills/processes only that one. A second functional manual scanner would require changing the processor to manage a collection, distributing held passengers, resolving multiple scan buttons or input behavior, and possibly changing feedback positions. That is outside the requested low-scope prestige visual.

Future references needed:

- `Assets/Prefabs/Scanners/P_ManualScanner.prefab`
- Scene parent `SecurityScannersPanel`
- `PassengerProcessor._manualScanner`
- `ScannerStationUIController._scannerPoint`
- `P_ScannerButton.prefab` only if the extra scanner becomes functional later.

## Boarding area audit

Boarding/waiting area representation:

- Scene object: `BoardingLoungeSection`
- Child waiting room controller object: referenced as `_waitingRoom` in `PassengerProcessor`
- Script: `WaitingRoomUIController`
- UI type: `RectTransform`, not world `Transform`, Tilemap, or Grid component.
- `P_BoardingArea.prefab` is also a UI prefab with stretched root `RectTransform`, child images, masked area, and `WaitingRoomStatusText`.

Current scene values in `WaitingRoomUIController`:

- `_areaSize: {x: 300, y: 750}`
- `_cellSize: 45`
- `_columns: 6`
- `_rows: 16`
- `_maxPassengers: 96`
- Capacity: `min(_maxPassengers, _columns * _rows)`, currently 96.

Can it increase visually by 50%?

- Yes, but avoid `localScale` as the primary approach. Scaling a UI container risks scaled text, passenger visuals, hit areas, and pulse feedback.
- Preferred future implementation:
  - For visual size only: increase the relevant `RectTransform.sizeDelta` / anchor offsets of the lounge content area and update `WaitingRoomUIController._areaSize` to match the intended passenger layout.
  - For visual + real capacity: increase `_rows` or `_columns` and `_maxPassengers` consistently, then verify passenger placement via `CalculatePosition(int index)`.
  - If the parent is layout-driven, adjust layout constraints/section dimensions instead of scaling the child.

Risks:

- The root `P_BoardingArea` prefab stretches to its parent (`AnchorMin 0,0`, `AnchorMax 1,1`, `sizeDelta 0,0`), so changing root `sizeDelta` alone may do nothing in scene.
- `BoardingFeedbackView._loungeContainer` is currently unassigned in the scene. Its pulse logic intentionally rejects broad section/container targets, so prestige resizing should not rely on that component.
- Existing passengers use `WaitingRoomUIController.CalculatePosition()` based on `_areaSize`, `_columns`, and `_cellSize`. Enlarging visuals without updating `_areaSize` can leave passengers visually clustered in the old area.
- Increasing capacity requires passenger pool capacity review. Scene `PassengerPool` has `_initialPoolSize: 120` and `_maximumPoolSize: 120`; current gate capacity is 96. A 50% real capacity increase would target 144, exceeding the current pool max.

## Persistence audit

No save/load implementation was found in current gameplay scripts. Searches did not find `PlayerPrefs`, `JsonUtility`, `File.*`, or project save/load classes in `Assets/Scripts/GameCore`.

If persistence is added later, prestige data should persist separately from run-state:

- `PrestigeCount`
- `PassportsScannedThisRun`
- `PassportsRequiredForPrestige`
- `GlobalPrestigeMultiplier`

Design decision still needed:

- If multiplier is cumulative, `GlobalPrestigeMultiplier` should likely derive from `PrestigeCount` (`2^PrestigeCount`) rather than be independently trusted.

## Recommended integration points

1. Add a prestige state/service later that subscribes to `PassengerProcessor.OnPassengerManuallyProcessed` and `OnPassengerAutoProcessed`.
2. Add or expose a read-only passport state for UI: current scanned passports, required passports, can prestige.
3. Update `IdleAirportUIController` or a small dedicated HUD presenter to write passport progress into `PassengerCounterText`.
4. Add a multiplier provider dependency to `EconomyController` or a serialized/service reference, then multiply `totalReward` in `RewardProcessedPassenger()`.
5. Add explicit reset methods to `AITSAScannerUpgrade`, `StoresManager`/`Store`, `PassengerQueueUIController`, `WaitingRoomUIController`, and scanner/processor coordination.
6. Add a visual-only prestige scanner object/prefab reference, activated by prestige count.
7. Add a boarding-area visual expansion method that adjusts `RectTransform` and `WaitingRoomUIController._areaSize` coherently.

## Technical risks

- No current persistence: permanent prestige state has nowhere to live yet.
- `EconomyController.Reset()` exists but uses Unity's special method name; future prestige reset should avoid accidental Editor reset semantics by adding an explicit method such as `ResetRunState()`.
- `AITSAScannerUpgrade` mutates `_tokensPerScanner` when buying durability. Resetting durability must restore the original baseline, not just set durability count to 0.
- `Store` has no setter/reset for `_ownedCount`; resetting stores needs new API without replacing serialized store data.
- Queue and waiting room do not expose clear methods; clearing lists without recycling passengers would leak active UI objects.
- Functional extra manual scanners are not currently supported by `PassengerProcessor`.
- A 50% real boarding capacity increase may exceed `PassengerPool._maximumPoolSize` unless the pool is also increased.
- `BoardingFeedbackView._loungeContainer` is unassigned in the scene, so existing boarding feedback may not pulse the lounge; this is separate from prestige but relevant to visual changes.

## Blockers

No hard blocker for prestige architecture.

Soft blockers before implementation:

- Decide whether passports count manual-only or all successful security processing.
- Decide initial required passport target.
- Decide whether the scanner extra remains visual-only.
- Decide whether the boarding area increase is visual-only or also increases capacity.
- Decide whether multiplier is cumulative (`x2`, `x4`, `x8`) or fixed at `x2`.
- Add/choose persistence approach if prestige must survive app restarts.

## Minimal implementation plan later

1. Create prestige state/service with `PrestigeCount`, `PassportsScannedThisRun`, `PassportsRequiredForPrestige`, `GlobalPrestigeMultiplier`, and events.
2. Subscribe it to passenger success events from `PassengerProcessor`.
3. Apply multiplier in `EconomyController.RewardProcessedPassenger()`.
4. Add explicit reset methods to economy, AI upgrade, stores, queue, scanners, and waiting room.
5. Add UI presenter support using existing `PassengersBlock` / `PassengerCounterText`.
6. Add visual-only extra manual scanner activation.
7. Add boarding visual expansion method and optionally capacity increase.
8. Add persistence after gameplay behavior is validated.

## Manual Unity checklist

- Open `Assets/Scenes/GameScene.unity`.
- Confirm `PassengerProcessor` references `_queue`, `_waitingRoom`, `_manualScanner`, `_aiScanner`, `_economyController`, `_storesManager`, and `_aiTSAScannerUpgrade`.
- Click manual scanner and confirm `PassengerCounterText` increments only after a passenger reaches the gate.
- Buy/enable AI TSA and confirm AI processed passengers also increment the same passenger counter.
- Confirm `PassengersBlock` has enough space for a passport icon plus `current / required`.
- Inspect `SecurityScannersPanel` and confirm a visual-only duplicate of `P_ManualScanner` has room without overlapping the AI scanner.
- Inspect `BoardingLoungeSection` and waiting room child dimensions before deciding whether the 50% increase expands height, width, or both.
- If capacity also increases, confirm passenger pool max is raised beyond the new gate capacity.
- Confirm no scene/prefab changes are made until the prestige implementation task.
