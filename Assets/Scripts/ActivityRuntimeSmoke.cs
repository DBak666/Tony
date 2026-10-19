using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AmazeRebuild
{
    public sealed partial class AmazeGame
    {
        // Explicit command-line QA entry. Both existing save keys are restored after the run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void BootActivityQA()
        {
            if (Environment.GetCommandLineArgs().Contains("-amazeActivitySmoke"))
                Instance.StartCoroutine(Instance.ActivityQA());
        }
        IEnumerator ActivityQA()
        {
            bool hadMain = PlayerPrefs.HasKey(SaveKey), hadActivities = PlayerPrefs.HasKey(ActivityData.Key);
            string oldMain = PlayerPrefs.GetString(SaveKey, ""), oldActivities = PlayerPrefs.GetString(ActivityData.Key, "");
            Exception failure = null;
            var steps = VerifyActivities();
            while (true)
            {
                bool more;
                try { more = steps.MoveNext(); } catch (Exception e) { failure = e; break; }
                if (!more) break;
                yield return steps.Current;
            }
            enabled = false;
            // Prevent normal shutdown callbacks from writing the QA in-memory state.
            save = null;
            if (hadMain) PlayerPrefs.SetString(SaveKey, oldMain); else PlayerPrefs.DeleteKey(SaveKey);
            if (hadActivities) PlayerPrefs.SetString(ActivityData.Key, oldActivities); else PlayerPrefs.DeleteKey(ActivityData.Key);
            PlayerPrefs.Save();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../QA"));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "activities-smoke.txt"), failure == null
                ? "PASS: all three card callbacks; independent main/activity saves; daily completion/reward deduplication/date bounds/resume; shark five-stage chain/failure/reset/reward deduplication/day rollover; league unique scoring/weekly rollover/promotion/no duplicate settlement; GM exit; theme resume; font glyphs. Original saves restored."
                : "FAIL: " + failure);
            Application.Quit(failure == null ? 0 : 1);
        }
        static void Require(bool value, string why) { if (!value) throw new Exception(why); }
        void CompleteQABoard()
        {
            foreach (int move in Model.Level.Solution) if (!Model.Removed[move]) Require(Model.TryRemove(move), "solution");
            Require(Model.Remaining == 0, "board complete"); Save(); Win();
        }
        IEnumerator VerifyActivities()
        {
            yield return null;
            activities = new ActivityData(); activities.Refresh(DateTime.Today);
            StartLevel(5); mainSave.lives = 2;
            Require(Model.TryRemove(Model.Hint()), "main fixture"); Save();
            string originalMain = JsonUtility.ToJson(mainSave);
            Home();
            for (int card = 0; card < 3; card++)
            {
                var home = page.GetComponent<HomePageView>();
                Require(home != null, "home prefab instance");
                new[] { home.league, home.daily, home.shark }[card].onClick.Invoke();
                Require(ScreenName != "Home", "card callback " + card); Home();
            }
            Require(!StartDaily(DateTime.Today.AddDays(1)), "future rejected");
            DateTime day = DateTime.Today.AddDays(-1);
            Require(StartDaily(day), "daily start");
            int level = save.level, hint = Model.Hint(); Require(Model.TryRemove(hint), "daily move"); save.lives = 2; Save();
            ShowDaily(); Require(JsonUtility.ToJson(mainSave) == originalMain, "main isolation");
            activities = ActivityData.Load();
            Require(StartDaily(day), "daily restart after load");
            Require(save.level == level && Model.Removed[hint] && save.lives == 2, "daily resume");
            StartBoard(save.level, true); Require(Model.Removed[hint], "theme rebuild resume");
            CompleteQABoard();
            Require(activities.tokens == 20 && activities.medals == 1, "daily reward");
            Require(!StartDaily(day) && activities.tokens == 20, "daily duplicate");
            CloseModal();
            Require(StartShark(), "shark start");
            hint = Model.Hint(); Model.TryRemove(hint); Save(); ShowShark();
            activities = ActivityData.Load(); Require(StartShark() && Model.Removed[hint], "shark resume");
            for (int stage = 0; stage < 5; stage++)
            {
                CompleteQABoard(); Require(activities.sharkStage == stage + 1, "shark progress");
                if (stage < 4) Require(StartShark(), "next shark");
            }
            Require(activities.ClaimShark() && !activities.ClaimShark(), "claim once");
            Require(activities.tokens == 120, "shark reward");
            activities.Refresh(DateTime.Today.AddDays(1)); Require(activities.sharkStage == 0 && !activities.sharkClaimed, "daily rollover");
            activities.sharkDay = ActivityData.Day(DateTime.Today);
            StartShark(); activities.sharkStage = 2; save.lives = 0; Lose();
            Require(activities.sharkStage == 0 && activities.sharkBoard == null && activityMode == "Main", "shark loss");
            Require(JsonUtility.ToJson(mainSave) == originalMain, "main preserved through activities");
            StartDaily(DateTime.Today);
            Require(TryGMJump("3", out _) && activityMode == "Main" && mainSave.level == 3, "GM exits activity");
            var league = new ActivityData(); league.Refresh(DateTime.Today);
            for (int n = 1; n <= 12; n++) Require(league.AwardLeague(n), "league score");
            Require(!league.AwardLeague(1) && league.leagueScore == 120 && league.Rank == 1, "league duplicate");
            var nextWeek = DateTime.Today.AddDays(7);
            league.Refresh(nextWeek); Require(league.tokens == 100 && league.tier == 1 && league.leagueScore == 0, "league settlement");
            league.Refresh(nextWeek); league.Refresh(DateTime.Today); Require(league.tokens == 100, "settlement idempotence");
            Home(); ShowLeague(); ShowDaily(); ShowShark(); ShowActivityGM();
            foreach (var label in root.GetComponentsInChildren<Text>())
                foreach (char c in label.text) if (c > 127 && !char.IsWhiteSpace(c)) Require(font.HasCharacter(c), "missing glyph " + c);
            yield return null;
        }
    }
}
