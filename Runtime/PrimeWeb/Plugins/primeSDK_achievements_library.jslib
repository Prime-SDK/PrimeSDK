const primeSDK_achievements_library = {

    primeSDK_achievements_happyTime: function () {
        Module.primeSDK.achievements.happyTime();
    },

    primeSDK_achievements_unlock: function (achievementId_utf8) {
        const achievementId = UTF8ToString(achievementId_utf8);
        Module.primeSDK.achievements.unlock(achievementId);
    },

    primeSDK_achievements_getScore: function (senderId, boardId_utf8, onScore_ptr) {
        const onScore = (score) => {
            Module.invokeMonoPCallback(senderId, onScore_ptr, score);
        };
        const boardId = UTF8ToString(boardId_utf8);
        (async () => {
            const score = await Module.primeSDK.achievements.getScore(boardId);
            onScore(score);
        })();
    },

    primeSDK_achievements_setScore: function (boardId_utf8, score) {
        const boardId = UTF8ToString(boardId_utf8);
        Module.primeSDK.achievements.setScore(boardId, score);
    },

    primeSDK_achievements_getLeaderboard: function (senderId, boardId_utf8, onLeaderboard_ptr) {
        const onLeaderboard = (leaderboard) => {
            const json = JSON.stringify(leaderboard);
            const json_utf8 = Module.allocateString(json);
            Module.invokeMonoPCallback(senderId, onLeaderboard_ptr, json_utf8);
        };
        const boardId = UTF8ToString(boardId_utf8);
        (async () => {
            const leaderboard = await Module.primeSDK.achievements.getLeaderboard(boardId);
            onLeaderboard(leaderboard);
        })();
    }

};
mergeInto(LibraryManager.library, primeSDK_achievements_library);