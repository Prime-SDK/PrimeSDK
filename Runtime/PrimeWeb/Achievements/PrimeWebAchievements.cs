using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using PrimeGames.SDK.Common;
using UnityEngine;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IAchievements))]
    public class PrimeWebAchievements : CommonAchievements {

        [DllImport(Naming.InternalDll)] private static extern void primeSDK_achievements_happyTime();
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_achievements_unlock(string achievementId);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_achievements_getScore(int senderId, string boardId, DelegateInt onScore);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_achievements_setScore(string boardId, int score);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_achievements_getLeaderboard(int senderId, string boardId, DelegateString onLeaderboard);

        private class GetScoreInfo {
            public Action<int> onScore;
        }

        private class GetLeaderboardInfo {
            public Action<Leaderboard> onLeaderboard;
        }

        private static readonly Dictionary<int, GetScoreInfo> getScoreCallbacks = new();
        private static readonly Dictionary<int, GetLeaderboardInfo> getLeaderboardCallbacks = new();

        [MonoPInvokeCallback(typeof(DelegateInt))]
        private static void OnGetScore(int senderId, int score) {
            try {
                if (getScoreCallbacks.TryGetValue(senderId, out GetScoreInfo callback)) {
                    callback.onScore?.Invoke(score);
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebAchievements), nameof(OnGetScore), exception);
            }
        }

        [MonoPInvokeCallback(typeof(DelegateString))]
        private static void OnGetLeaderboard(int senderId, string json) {
            try {
                if (getLeaderboardCallbacks.TryGetValue(senderId, out GetLeaderboardInfo callback)) {
                    Leaderboard leaderboard = JsonUtility.FromJson<Leaderboard>(json);
                    callback.onLeaderboard?.Invoke(leaderboard);
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebAchievements), nameof(OnGetLeaderboard), exception);
            }
        }

        public PrimeWebAchievements() {
            SetInitialized();
        }

        protected override void HappyTimeImpl() {
            primeSDK_achievements_happyTime();
        }

        protected override void UnlockImpl(string achievementId) {
            primeSDK_achievements_unlock(achievementId);
        }

        protected override void GetScoreImpl(string boardId, Action<int> onScore) {
            int senderId = getScoreCallbacks.Count;
            getScoreCallbacks[senderId] = new GetScoreInfo {
                onScore = onScore
            };
            primeSDK_achievements_getScore(senderId, boardId, OnGetScore);
        }

        protected override void SetScoreImpl(string boardId, int score) {
            primeSDK_achievements_setScore(boardId, score);
        }

        protected override void GetLeaderboardImpl(string boardId, Action<Leaderboard> onLeaderboard) {
            int senderId = getLeaderboardCallbacks.Count;
            getLeaderboardCallbacks[senderId] = new GetLeaderboardInfo() {
                onLeaderboard = onLeaderboard
            };
            primeSDK_achievements_getLeaderboard(senderId, boardId, OnGetLeaderboard);
        }

    }

}