# Part 14 — Recurring content and monetization

Open **Pause → Shop → Challenges**. Switch Daily/Weekly, open Events for the rotating challenge and cosmetics, or open Season Pass. Daily login remains in the Shop. Rewarded ads are explicitly unavailable per the user's instruction; no ad SDK, placement, simulated completion or ad currency grant is enabled.

## Daily and weekly challenges

| Activity | Daily target | Weekly target |
| --- | ---: | ---: |
| Ride distance | 1,000 m | 10,000 m |
| Bank stunt points | 500 | 5,000 |
| Complete new mission/challenge tiers | 2 | 10 |

Each daily goal awards 200 coins and 100 season XP. Each weekly goal awards 1,000 coins, 2 gems and 300 season XP. Rewards require an explicit Claim and can only be claimed once in their period. Only banked stunts count. Career and Freestyle newly rewarded tiers both count; retries, failures and duplicate final-tier completions do not. Ride distance uses existing session velocity-based distance accounting, excluding paused/crashed riding and teleport displacement.

Daily goals reset at midnight UTC. Weekly goals reset Monday 00:00 UTC. Progress and unclaimed rewards expire at reset; screens display the reset date. Device time moving backward cannot reopen older challenge periods. These are offline device dates, not server-verified time, so advancing the device clock can advance content.

## Daily login

The existing seven-day login streak still awards 100–700 coins, with 5 gems on day seven. Each successful daily login now adds 25 season XP. Claiming a second time awards nothing. Existing save history is retained; prior login claims are not retroactively paid XP.

## Rotating event

One weekly event runs alongside the ordinary challenges, rotating in this order:

- Explorer Week: ride 5,000 m.
- Stunt Festival: bank 2,500 stunt points.
- Career Rally: complete five new Career/Freestyle tiers.

Each event awards 1,500 coins, 3 gems and 500 season XP once. It is a rotating gameplay goal; this implementation does not replace world scenery with holiday art or introduce downloaded content.

## 28-day season pass

Seasons are deterministic UTC blocks of 28 days shared by local saves, not 28 days from purchase. The screen shows the end date. A season has ten tiers at 250 XP each; XP caps at 2,500. Earn XP from logins and claimed daily, weekly and event goals.

- Free track: 250 coins per tier, plus 2 gems at tiers 5 and 10.
- Bonus track: unlock for **25 earned gems**, valid only for the displayed season. Each tier gives 500 coins and 1 gem. Tier 10 also permanently unlocks Violet frame paint.
- Both tracks are claimed separately. Buying the bonus track late permits claiming already-earned tiers.
- New seasons reset XP, claim flags and bonus access. Currency and earned paints remain owned. Unclaimed rewards expire; there is no automatic renewal or real-money subscription.

## Cosmetics and ads

Sunset frame paint costs 10 gems under Events. Neon paint remains 5 gems in the Shop. Violet comes from the final bonus-season tier. Owned paints are selected by cycling Paint in the Garage; unowned paints are skipped. Costs and ownership are saved together, and duplicate purchases are rejected. Original stock paints remain free. Helmet/decal assets and real-money StoreKit products are not included.

Rewarded ads are a disabled, clearly labelled entry. Enabling ads later requires choosing/configuring a real provider and implementing its verified completion callback. Nothing in this part pretends an ad was watched or charges real money.

## Persistence and integration

`RecurringProgress` is embedded in the existing `Cyclomania.Economy.v1` wallet. Old saves initialize the added fields without resetting balances, bicycle purchases, mission reward history or login streaks. Challenge claims, currency, season XP, bonus-pass spending, claim flags and cosmetic entitlements commit in one wallet snapshot.

Banked-stunt and mission activity is recorded in its original wallet reward transaction. Distance is buffered and saved every five seconds and flushed on content-screen entry, background/focus loss and HUD destruction. An abrupt process kill can lose the last unsaved distance interval. Save failure leaves the pending buffer for retry and reports the wallet error. PlayerPrefs remains local storage, with the limitations documented in Economy.md; this is not cloud or receipt-validated commerce.

## Validation

Core tests cover old-save migration, activity routing, invalid samples, claims, save round trips, daily/weekly/season rollover, clock rollback, event rotation, prices, tier gating, bonus access, permanent cosmetic ownership and normalization. Full-scene checks cover challenge navigation/claims, disabled ads, cosmetic purchases, bonus-pass purchase, season claims and modal bounds, alongside existing gameplay regression checks.

Run `python3 Tests/CoreCycling/run.py`, `python3 Tests/Missions/run.py`, and `python3 Tests/TouchControls/check_layout.py`. Physical iPhone/iPad UI and app-restart testing remain pending.
