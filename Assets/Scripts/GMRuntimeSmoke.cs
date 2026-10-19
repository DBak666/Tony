using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AmazeRebuild
{
    public sealed class GMRuntimeSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Environment.GetCommandLineArgs().Contains("-amazeGMSmoke"))
                new GameObject("GM Smoke").AddComponent<GMRuntimeSmoke>();
        }

        IEnumerator Start()
        {
            bool hadSave = PlayerPrefs.HasKey(AmazeGame.SaveKey);
            string original = PlayerPrefs.GetString(AmazeGame.SaveKey, "");
            var game = AmazeGame.Instance;
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../QA"));
            Directory.CreateDirectory(folder);
            var steps = Verify(game, folder);
            Exception failure = null;
            while (true)
            {
                bool more;
                try { more = steps.MoveNext(); }
                catch (Exception e) { failure = e; break; }
                if (!more) break;
                yield return steps.Current;
            }
            game.enabled = false;
            Destroy(game);
            yield return null;
            if (hadSave) PlayerPrefs.SetString(AmazeGame.SaveKey, original);
            else PlayerPrefs.DeleteKey(AmazeGame.SaveKey);
            PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(folder, "gm-smoke.txt"), failure == null
                ? "PASS: first/last level; invalid and overflow input; busy rejection; life/removed reset; persistence and resume; GM UI. Original save restored."
                : "FAIL: " + failure);
            Application.Quit(failure == null ? 0 : 1);
        }

        static void Check(bool value, string message)
        { if (!value) throw new Exception(message); }

        IEnumerator Verify(AmazeGame game, string folder)
        {
            yield return null;
            Check(game.TryGMJump("5", out _), "jump 5");
            int blocked = Array.FindIndex(game.Model.Removed, _ => false);
            for (int i = 0; i < game.Model.Paths.Length; i++)
                if (!game.Model.CanExit(i)) { blocked = i; break; }
            Check(blocked >= 0, "blocked move fixture");
            game.TapArrow(blocked);
            Check(!game.TryGMJump("1", out _) && game.CurrentLevel == 5, "busy rejection");
            yield return new WaitForSeconds(1);
            Check(game.Lives == 2, "life fixture");
            game.TapArrow(game.Model.Hint());
            yield return new WaitForSeconds(1);
            Check(game.Model.Remaining < game.Model.Paths.Length, "removed fixture");
            Check(game.TryGMJump("5", out _), "restart");
            Check(game.Lives == 3 && game.Model.Remaining == game.Model.Paths.Length, "reset");
            foreach (string input in new[] { "", "abc", "0", "-1", "1.5", "999999999999999", (game.LevelCount + 1).ToString() })
            {
                string before = PlayerPrefs.GetString(AmazeGame.SaveKey);
                Check(!game.TryGMJump(input, out _) && game.CurrentLevel == 5, "invalid: " + input);
                Check(before == PlayerPrefs.GetString(AmazeGame.SaveKey), "invalid changed save");
            }
            Check(game.TryGMJump("1", out _), "first level");
            game.ShowGM();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "gm-panel.png"));
            yield return new WaitForSeconds(.3f);
            Check(game.TryGMJump(game.LevelCount.ToString(), out _), "last level");
            var saved = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(AmazeGame.SaveKey));
            Check(saved.level == game.LevelCount && saved.lives == 3 && saved.removed.Length == 0, "persist");
            game.Home(); game.StartLevel(saved.level, true);
            Check(game.CurrentLevel == game.LevelCount && game.Model.Remaining > 0 && game.Board, "resume");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "gm-last-level.png"));
            yield return new WaitForSeconds(.3f);
        }
    }
}
