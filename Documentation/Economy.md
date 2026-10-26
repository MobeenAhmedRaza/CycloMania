# Part 12 — Economy and purchases

Open **Pause → Shop** for balances, the daily reward, bicycles, drivetrain/brake/handling upgrades and neon frame paint. Browse bicycles with the arrows; equip bicycles and select paint in the Garage. Disabled buttons indicate an owned item, maximum tier, locked bicycle or insufficient funds.

## Earnings and prices

- Banked stunts earn 1 coin per 10 points in free ride, Career and Freestyle. Fractional progress carries over between combos and saves. Crashed/unbanked combos earn nothing.
- Each newly completed mission/challenge tier earns coins equal to its Career/Freestyle point reward. Every fifth tier also earns 2 gems. Replaying a rewarded tier, including level 100, gives no further mission payout.
- Claim one reward per UTC date: 100–700 coins over a seven-day streak, plus 5 gems on day seven. The next day starts another cycle; a missed day resets it. Earlier device dates cannot repeat claims. This offline clock is not server-verified.
- City is free. BMX costs 500 coins, Mountain 1,500, Trials 3,000 and Fatbike 5,000.
- Each upgrade category has three tiers costing 300, 1,000 and 2,500 coins. Purchases advance one tier immediately; existing speed and acceleration formulas are unchanged.
- Neon frame paint costs 5 gems once, usable on all unlocked bicycles. The original five paints remain free.

Existing lifetime stunt-point unlocks and upgrades remain an alternative free progression path. Purchases let players use mission earnings to unlock items earlier. Coins and gems are separate from Career, Freestyle and lifetime stunt points. No past points are converted or charged retroactively.

## Saving

`Cyclomania.Economy.v1` stores balances, purchased entitlements, mission reward high-water marks, daily streak and stunt remainder together. Each transaction serializes a proposed copy before exposing its results. Purchased entitlements overlay the garage save on load, preserving equipped purchased bicycles and upgrades. Save failures appear in the shop. Empty EconomySaveKey disables persistence for automated fixtures.

PlayerPrefs is local storage, not a secure or cloud-backed economy. Mission progression and the wallet are separate saves; a storage failure during reward saving can require recovery and is not a cross-save atomic transaction. Production receipts and server validation are not implemented.

## Scope

This is the playable earned-currency economy. Part 14 adds recurring challenges, a gem-unlocked season pass and additional paints; see `Recurring-Content.md`. StoreKit gem packs, rewarded-ad providers, premium bicycle assets, helmets/decals and time-skip systems are not implemented. The current game has no upgrade timers to skip. No buttons simulate payment or ad completion. Multiplayer fuel is excluded as requested.

## Validation

Core economy checks cover malformed saves, balance normalization, stunt remainder, mission reward deduplication across reload, rare gems, daily streaks, insufficient funds, exact charges, upgrade constraints, cosmetic ownership, overflow, purchased-item restoration and existing progression compatibility. Full-scene checks cover Shop navigation, daily claims, HUD currency binding and Garage navigation alongside existing mission/settings tests. Device touch presentation and restart behavior still require iPhone/iPad playtesting.
