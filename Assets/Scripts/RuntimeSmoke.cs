using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace AmazeRebuild
{
    // Standalone QA entry: -amazeSmoke. Preserves the user's save across the test.
    public sealed class RuntimeSmoke : MonoBehaviour
    {
        IEnumerator Start()
        {
            string original = PlayerPrefs.GetString(AmazeGame.SaveKey, "");
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../QA"));
            Directory.CreateDirectory(folder);
            var game = AmazeGame.Instance;
            yield return new WaitForSeconds(5f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "home.png"));
            yield return new WaitForSeconds(.25f);
            game.StartLevel(5);
            yield return new WaitForSeconds(.3f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "level-005.png"));
            int blocker = -1;
            for (int i = 0; i < game.Model.Paths.Length; i++) if (!game.Model.CanExit(i)) { blocker = i; break; }
            if (blocker < 0) throw new Exception("QA: expected a blocked arrow");
            game.TapArrow(blocker);
            yield return new WaitForSeconds(.8f);
            if (game.Lives != 2) throw new Exception("QA: blocked arrow must consume one life");
            int hint = game.Model.Hint(); int before = game.Model.Remaining;
            game.TapArrow(hint);
            yield return new WaitForSeconds(1);
            if (game.Model.Remaining != before - 1) throw new Exception("QA: clear arrow was not removed");
            game.Home(); game.StartLevel(5, true);
            if (game.Lives != 2 || game.Model.Remaining != before - 1) throw new Exception("QA: save/resume mismatch");
            game.StartLevel(1);
            while (game.Model.Remaining > 0)
            {
                int h = game.Model.Hint(); if (h < 0) throw new Exception("QA: unsolvable tutorial");
                game.TapArrow(h); yield return new WaitForSeconds(1);
            }
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "win.png"));
            yield return new WaitForSeconds(.3f);
            game.StartLevel(5);
            for (int n = 0; n < 3; n++)
            {
                int b = -1; for (int i = 0; i < game.Model.Paths.Length; i++) if (!game.Model.CanExit(i)) { b = i; break; }
                game.TapArrow(b); yield return new WaitForSeconds(.8f);
            }
            if (game.Lives != 0) throw new Exception("QA: loss state was not reached");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "lose.png"));
            yield return new WaitForSeconds(.4f);
            File.WriteAllText(Path.Combine(folder, "runtime-smoke.txt"), "PASS: home/game/win/loss state flow; valid move; blocked move; life decrement; save/resume; tutorial completion. Screenshots require separate visual inspection.\n");
            // Disable the component before quit so its in-memory QA state cannot overwrite restored prefs.
            game.enabled = false;
            Destroy(game); yield return null;
            if (string.IsNullOrEmpty(original)) PlayerPrefs.DeleteKey(AmazeGame.SaveKey); else PlayerPrefs.SetString(AmazeGame.SaveKey, original);
            PlayerPrefs.Save(); Application.Quit(0);
        }
    }
}
