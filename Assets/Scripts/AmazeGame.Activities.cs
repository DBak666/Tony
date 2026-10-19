using System;
using System.Linq;
using UnityEngine;

namespace AmazeRebuild
{
    public sealed partial class AmazeGame
    {
        ActivityData activities;
        SaveData mainSave;
        string activityMode = "Main", sessionDay;
        DailyRecord dailySession;
        DateTime calendarMonth = DateTime.Today;
        public string ActivityMode => activityMode;
        public ActivityData Activities => activities;
        public int MainLevel => mainSave.level;
        void InitActivities() { mainSave = save; activities = ActivityData.Load(); activities.Refresh(DateTime.Today); activities.Store(); }
        void ExitActivity()
        {
            if (activityMode == "Main") return;
            Persist(); save = mainSave; activityMode = "Main"; dailySession = null;
            Model = null; Board = null;
        }
        void ActivityHeader(string title)
        {
            if (activityMode != "Main") ExitActivity();
            activities.Refresh(DateTime.Today); activities.Store();
            ClearPage(title); Board = null; Model = null;
            IconButton(page, "CommonIconBack", 32, 35, Home, Ink, 42);
            Text(page, title, 120, 22, 510, 60, 35, Brown, bold: true);
            Text(page, "活动币 " + activities.tokens + "  ·  奖章 " + activities.medals, 80, 92, 590, 45, 24, Brown);
        }
        public void ShowLeague()
        {
            if (busy) return;
            ActivityHeader("联赛");
            string[] tiers = { "青铜", "白银", "黄金" };
            Text(page, tiers[activities.tier] + "联赛 · 本地模拟榜单", 70, 162, 610, 54, 30, Brown, bold: true);
            Text(page, "每周一结算；同一主线关卡本周首次通关 +10 分。\n前 3 名晋级，后 3 名降级；同分时对手优先。", 48, 222, 654, 84, 22, Brown);
            var rows = Enumerable.Range(0, 9).Select(i => new LeagueRow { name = "模拟玩家 " + (i + 1), score = activities.RivalScore(i) }).ToList();
            rows.Add(new LeagueRow { name = "我", score = activities.leagueScore, mine = true });
            rows = rows.OrderByDescending(r => r.score).ThenBy(r => r.mine).ToList();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i]; float y = 323 + i * 57;
                Image(page, "RankRow", 60, y, 630, 50, row.mine ? new Color32(237, 210, 250, 255) : new Color32(240, 224, 198, 255));
                Text(page, (i + 1) + "    " + row.name, 80, y, 385, 50, 24, Brown, TextAnchor.MiddleLeft);
                Text(page, row.score + " 分", 490, y, 160, 50, 24, Brown);
            }
            Text(page, string.IsNullOrEmpty(activities.lastSettlement) ? "参与结算奖励 25 活动币；前 3 名奖励 100。" : activities.lastSettlement, 50, 920, 650, 65, 23, Brown);
            Button(page, "挑战主线赚积分", 110, 1035, 530, 86, () => StartLevel(mainSave.level, true));
            Button(page, "奖励兑换", 215, 1150, 320, 65, ShowRewards, Brown, size: 25);
        }
        sealed class LeagueRow { public string name; public int score; public bool mine; }
        public void ShowDaily()
        {
            if (busy) return;
            ActivityHeader("每日挑战");
            DateTime first = new DateTime(calendarMonth.Year, calendarMonth.Month, 1);
            Text(page, first.ToString("yyyy 年 M 月"), 160, 166, 430, 55, 31, Brown, bold: true);
            Button(page, "上月", 45, 167, 110, 55, () => { calendarMonth = first.AddMonths(-1); ShowDaily(); }, Brown, size: 22);
            Button(page, "下月", 595, 167, 110, 55, () => { calendarMonth = first.AddMonths(1); ShowDaily(); }, Brown, size: 22).interactable = first.AddMonths(1) <= DateTime.Today;
            string[] weekdays = { "一", "二", "三", "四", "五", "六", "日" };
            for (int i = 0; i < 7; i++) Text(page, weekdays[i], 36 + i * 98, 257, 90, 42, 25, Brown);
            int offset = ((int)first.DayOfWeek + 6) % 7;
            for (int d = 1; d <= DateTime.DaysInMonth(first.Year, first.Month); d++)
            {
                DateTime date = first.AddDays(d - 1); int cell = offset + d - 1;
                bool complete = activities.days.Any(r => r.day == ActivityData.Day(date) && r.complete);
                var button = Button(page, d + (complete ? "\n完成" : ""), 36 + cell % 7 * 98, 317 + cell / 7 * 95, 88, 80,
                    () => StartDaily(date), complete ? new Color32(65, 151, 105, 255) : Purple, size: 22);
                button.interactable = date <= DateTime.Today;
            }
            Text(page, "每天固定一关，可补玩过去日期。\n首次完成奖励 20 活动币和 1 枚奖章。\n活动进度单独保存，随时返回继续。", 65, 938, 620, 132, 26, Brown);
            Button(page, "挑战今天", 110, 1135, 530, 85, () => StartDaily(DateTime.Today));
        }
        public bool StartDaily(DateTime date)
        {
            if (busy || date.Date > DateTime.Today) return false;
            ExitActivity();
            dailySession = activities.Daily(date, LevelCount);
            if (dailySession.complete) { Message("已完成", "这一天的挑战已完成，奖励已经到账。请选择其他日期。"); return false; }
            activityMode = "Daily"; sessionDay = dailySession.day;
            save = dailySession.board;
            StartBoard(save.level, true); return true;
        }
        public void ShowShark()
        {
            if (busy) return;
            ActivityHeader("大逃鲨");
            Image(page, "Shark", 205, 175, 340, 420, Color.white, "SharkCard");
            Text(page, "今日逃生进度 " + activities.sharkStage + " / 5", 60, 620, 630, 70, 35, Brown, bold: true);
            for (int i = 0; i < 5; i++) Image(page, "Stage", 120 + i * 108, 730, 76, 76, i < activities.sharkStage ? Purple : new Color32(212, 194, 166, 255), "Circle");
            Text(page, "连续通过 5 关即可逃生。每关 3 点生命。\n生命耗尽则本轮重来；中途退出可继续。\n每天重置，逃生奖励每天可领取一次。", 45, 850, 660, 150, 25, Brown);
            if (activities.sharkStage >= 5)
                Button(page, activities.sharkClaimed ? "今日奖励已领取" : "领取 100 活动币", 110, 1060, 530, 85,
                    () => { if (activities.ClaimShark()) activities.Store(); ShowShark(); }).interactable = !activities.sharkClaimed;
            else Button(page, activities.sharkBoard == null ? "开始逃生" : "继续逃生", 110, 1060, 530, 85, () => StartShark());
            Button(page, "奖励兑换", 215, 1170, 320, 65, ShowRewards, Brown, size: 25);
        }
        public bool StartShark()
        {
            if (busy) return false;
            ExitActivity(); activities.Refresh(DateTime.Today);
            if (activities.sharkStage >= 5) { ShowShark(); return false; }
            if (activities.sharkBoard == null)
                activities.sharkBoard = new SaveData { level = ActivityData.LevelFor(DateTime.Today, 31 + activities.sharkStage + activities.sharkRuns * 5, LevelCount) };
            activityMode = "Shark"; sessionDay = activities.sharkDay; save = activities.sharkBoard;
            StartBoard(save.level, true); return true;
        }
        string BoardTitle(int number) => activityMode == "Daily" ? "每日 " + sessionDay.Substring(5) : activityMode == "Shark" ? "大逃鲨 " + (activities.sharkStage + 1) + "/5" : "关卡 " + number;
        void ReturnFromBoard() { if (activityMode == "Daily") ShowDaily(); else if (activityMode == "Shark") ShowShark(); else Home(); }
        void RetryBoard() { StartBoard(save.level, false); }
        void ActivityWin()
        {
            if (activityMode == "Shark" && sessionDay != ActivityData.Day(DateTime.Today)) { ShowShark(); return; }
            bool daily = activityMode == "Daily";
            string message;
            if (daily) { activities.CompleteDaily(dailySession); message = "每日挑战完成！\n20 活动币和 1 枚奖章已到账。"; }
            else
            {
                activities.sharkStage++; activities.sharkBoard = null;
                message = activities.sharkStage >= 5 ? "成功逃生！返回活动领取奖励。" : "成功通过本关！\n继续挑战下一关。";
            }
            // Leave the finished board before creating the settlement UI, preventing duplicate rewards.
            ExitActivity();
            if (daily) ShowDaily(); else ShowShark();
            var panel = Modal(daily ? "挑战完成" : "逃生进度");
            Text(panel, message, 50, 130, 500, 160, 28, Brown);
            Button(panel, daily ? "返回日历" : activities.sharkStage >= 5 ? "前往领奖" : "继续逃生", 70, 365, 460, 85,
                () => { if (daily) ShowDaily(); else if (activities.sharkStage >= 5) ShowShark(); else StartShark(); });
        }
        bool ActivityLose()
        {
            if (activityMode != "Shark") return false;
            activities.sharkStage = 0; activities.sharkRuns++; activities.sharkBoard = null;
            ExitActivity();
            ShowShark();
            var panel = Modal("逃生失败");
            Text(panel, "本轮进度已重置。\n观察阻挡关系，再挑战一次吧！", 45, 130, 510, 160, 28, Brown);
            Button(panel, "重新逃生", 70, 320, 460, 80, () => StartShark());
            Button(panel, "返回活动", 100, 425, 400, 60, ShowShark, Brown, size: 24);
            return true;
        }
        void ShowRewards()
        {
            var panel = Modal("活动奖励");
            Text(panel, "活动币：" + activities.tokens + "\n20 活动币可恢复主线的 3 点生命。\n不会清除主线已消除的箭头。", 40, 125, 520, 180, 26, Brown);
            Button(panel, "兑换主线生命", 70, 325, 460, 75, () =>
            {
                if (activities.tokens < 20 || mainSave.lives >= 3) return;
                activities.tokens -= 20; mainSave.lives = 3; Persist(); ShowRewards();
            }).interactable = activities.tokens >= 20 && mainSave.lives < 3;
            Button(panel, "关闭", 150, 435, 300, 65, CloseModal, Brown, size: 24);
        }
        void ShowActivityGM()
        {
            var panel = Modal("GM 活动调试", 650);
            Button(panel, "每日挑战", 60, 115, 480, 70, ShowDaily, Brown);
            Button(panel, "大逃鲨", 60, 205, 480, 70, ShowShark, Brown);
            Button(panel, "联赛榜单", 60, 295, 480, 70, ShowLeague, Brown);
            Button(panel, "重置今日逃生", 60, 385, 480, 70, () =>
            { ExitActivity(); activities.sharkStage = 0; activities.sharkBoard = null; activities.sharkClaimed = false; activities.Store(); ShowShark(); }, Brown);
            Button(panel, "返回", 150, 535, 300, 65, ShowGM);
        }
    }
}
