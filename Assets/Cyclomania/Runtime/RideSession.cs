using UnityEngine;

namespace Cyclomania
{
    // Session-only HUD data. Career/economy systems can supply objectives and balances later.
    public sealed class RideSession
    {
        public float DistanceMeters { get; private set; }
        public int Coins { get; private set; }
        public int Gems { get; private set; }
        public string Objective { get; private set; } = "FREE RIDE";
        public string ObjectiveDetail { get; private set; } = "Explore the city at your own pace.";
        public float ObjectiveProgress { get; private set; } = -1f;

        public void RecordTravel(Vector3 velocity, float seconds, bool riding)
        {
            if (!riding || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            float meters = new Vector2(velocity.x, velocity.z).magnitude * seconds;
            if (!float.IsNaN(meters) && !float.IsInfinity(meters)) DistanceMeters += meters;
        }
        public void SetCurrency(int coins, int gems) { Coins = Mathf.Max(0, coins); Gems = Mathf.Max(0, gems); }
        public void SetObjective(string title, string detail, float progress = -1f)
        {
            Objective = string.IsNullOrWhiteSpace(title) ? "FREE RIDE" : title;
            ObjectiveDetail = detail ?? "";
            ObjectiveProgress = progress < 0 || float.IsNaN(progress) ? -1f : Mathf.Clamp01(progress);
        }
        public void Restart()
        {
            DistanceMeters = 0;
            SetObjective("FREE RIDE", "Explore the city at your own pace.");
        }
    }
}
