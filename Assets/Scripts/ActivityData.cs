using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmazeRebuild
{
    [Serializable] public sealed class DailyRecord
    {
        public string day;
        public bool complete;
        public SaveData board;
    }
    [Serializable] public sealed class ActivityData
    {
        public const string Key = "AmazeRebuild.Activities.v1";
        public int tokens, medals, tier, leagueScore, sharkStage, sharkRuns;
        public string week = "", sharkDay = "", lastSettlement = "";
        public bool sharkClaimed;
        public List<int> leagueLevels = new List<int>();
        public List<DailyRecord> days = new List<DailyRecord>();
        public SaveData sharkBoard;
        public static string Day(DateTime date) => date.ToString("yyyy-MM-dd");
        public static string Week(DateTime date) => Day(date.Date.AddDays(-((int)date.DayOfWeek + 6) % 7));
        public static ActivityData Load()
        {
            ActivityData data = null;
            try { data = JsonUtility.FromJson<ActivityData>(PlayerPrefs.GetString(Key, "")); } catch { }
            if (data == null) data = new ActivityData();
            if (data.days == null) data.days = new List<DailyRecord>();
            if (data.leagueLevels == null) data.leagueLevels = new List<int>();
            data.tier = Mathf.Clamp(data.tier, 0, 2);
            return data;
        }
        public void Store() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(this)); PlayerPrefs.Save(); }
        public static int LevelFor(DateTime date, int salt, int count)
        {
            // Stable across processes; unlike string.GetHashCode this also survives restarts.
            long day = date.Date.Ticks / TimeSpan.TicksPerDay;
            return (int)((day * 37 + salt * 101L) % count) + 1;
        }
        public int RivalScore(int index) => (10 - index) * 10 + tier * 15;
        public int Rank { get { int rank = 1; for (int i = 0; i < 9; i++) if (RivalScore(i) >= leagueScore) rank++; return rank; } }
        public void Refresh(DateTime now)
        {
            string current = Week(now);
            // Monotonic rollover prevents changing the clock backwards from granting rewards.
            if (string.IsNullOrEmpty(week)) week = current;
            else if (string.CompareOrdinal(current, week) > 0)
            {
                int rank = Rank;
                if (leagueScore > 0)
                {
                    int reward = rank <= 3 ? 100 : 25;
                    tokens += reward; medals++;
                    lastSettlement = "上周第 " + rank + " 名，获得 " + reward + " 活动币";
                    if (rank <= 3) tier = Mathf.Min(2, tier + 1);
                    else if (rank >= 8) tier = Mathf.Max(0, tier - 1);
                }
                week = current; leagueScore = 0; leagueLevels.Clear();
            }
            string day = Day(now);
            if (string.IsNullOrEmpty(sharkDay) || string.CompareOrdinal(day, sharkDay) > 0)
            { sharkDay = day; sharkStage = 0; sharkRuns = 0; sharkClaimed = false; sharkBoard = null; }
        }
        public DailyRecord Daily(DateTime date, int count)
        {
            string key = Day(date);
            var record = days.Find(d => d.day == key);
            if (record == null)
            {
                record = new DailyRecord { day = key, board = new SaveData { level = LevelFor(date, 7, count) } };
                days.Add(record);
            }
            return record;
        }
        public bool CompleteDaily(DailyRecord record)
        {
            if (record.complete) return false;
            record.complete = true; tokens += 20; medals++; return true;
        }
        public bool AwardLeague(int level)
        {
            if (leagueLevels.Contains(level)) return false;
            leagueLevels.Add(level); leagueScore += 10; return true;
        }
        public bool ClaimShark()
        {
            if (sharkStage < 5 || sharkClaimed) return false;
            sharkClaimed = true; tokens += 100; medals++; return true;
        }
    }
}
