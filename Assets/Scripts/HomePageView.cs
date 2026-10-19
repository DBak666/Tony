using System;
using UnityEngine;
using UnityEngine.UI;

namespace AmazeRebuild
{
    public sealed class HomePageView : MonoBehaviour
    {
        [Header("Navigation")]
        public Button settings, league, daily, shark, continueGame, levelPicker, gm;
        [Header("Live labels")]
        public Text leagueProgress, dailyDate, sharkProgress, wallet, continueLabel, footer;
        [Header("Level shortcuts")]
        public Button[] levels;
        public Text[] levelLabels;
        [Header("Theme")]
        public Image background, settingsIcon;
        public Text title;
        public Color cream = new Color32(247, 240, 221, 255);
        public Color mint = new Color32(215, 239, 226, 255);
        public Color dark = new Color32(46, 64, 79, 255);
        public Color normalInk = new Color32(25, 44, 58, 255);
        public Color darkInk = new Color32(245, 244, 231, 255);
        public Color currentLevelColor = new Color32(150, 62, 211, 255);
        public Color otherLevelColor = new Color32(207, 144, 68, 255);

        static void BindButton(Button button, Action action)
        { button.onClick.AddListener(() => action()); }

        // Called once on each prefab instance. Layout, art, and static copy remain authored in the prefab.
        public void Bind(AmazeGame game, int theme, Action pickLevel)
        {
            cream = new Color32(227, 241, 255, 255);
            currentLevelColor = new Color32(47, 153, 243, 255);
            otherLevelColor = new Color32(69, 187, 224, 255);
            if (theme == 0) CandyBackdrop.Attach((RectTransform)transform);
            foreach (var label in GetComponentsInChildren<Text>(true))
                if (label.color.r > label.color.b * 1.25f && label.color.g < .8f)
                    label.color = new Color32(65, 107, 151, 255);
            foreach (var button in new[] { continueGame, levelPicker, gm })
            {
                button.image.color = new Color32(49, 149, 239, 255);
                CandyButtonSurface.Attach(button.image);
                var shadow = button.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(.08f, .18f, .30f, .28f); shadow.effectDistance = new Vector2(0, -4);
            }
            BindButton(settings, game.Settings); BindButton(league, game.ShowLeague);
            BindButton(daily, game.ShowDaily); BindButton(shark, game.ShowShark);
            BindButton(continueGame, () => game.StartLevel(game.MainLevel, true));
            BindButton(levelPicker, pickLevel); BindButton(gm, game.ShowGM);
            leagueProgress.text = "本周 " + game.Activities.leagueScore + " 分";
            dailyDate.text = DateTime.Today.ToString("M月d日");
            sharkProgress.text = game.Activities.sharkStage + " / 5 关";
            wallet.text = "活动币 " + game.Activities.tokens + "    奖章 " + game.Activities.medals;
            continueLabel.text = "继续\n关卡" + game.MainLevel;
            footer.text = "离线重建 · " + game.LevelCount + " 个原始关卡";
            for (int i = 0; i < levels.Length; i++)
            {
                int n = Mathf.Min(game.MainLevel + i, game.LevelCount);
                levelLabels[i].text = n.ToString();
                levels[i].image.color = i == 0 || n % 5 == 0 ? currentLevelColor : otherLevelColor;
                BindButton(levels[i], () => game.StartLevel(n, n == game.MainLevel));
            }
            background.color = theme == 1 ? mint : theme == 2 ? dark : cream;
            title.color = settingsIcon.color = theme == 2 ? darkInk : normalInk;
        }
    }
}
