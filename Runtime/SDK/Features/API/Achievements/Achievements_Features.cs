using PrimeGames.SDK.SourceGenerator;
using PrimeGames.SDK.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class Achievements_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<Achievements_Features> { }

        public Achievements_Features() {
            SetInfo("Achievements", nameof(IAchievements), nameof(AchievementsProvider));

            CreateButton(nameof(IAchievements.HappyTime), () => {
                PrimeSDK.Achievements.HappyTime();
            });
            CreateButton(nameof(IAchievements.Unlock), () => {
                PrimeSDK.Achievements.Unlock("example_achievement");
            });
            CreateButton(nameof(IAchievements.GetScore), () => {
                PrimeSDK.Achievements.GetScore("score", score => {

                });
            });
            CreateButton(nameof(IAchievements.SetScore), () => {
                PrimeSDK.Achievements.SetScore("score", Random.Range(0, 1000));
            });
            CreateButton(nameof(IAchievements.GetLeaderboard), () => {
                PrimeSDK.Achievements.GetLeaderboard("score", leaderboard => {

                });
            });
        }

    }

}