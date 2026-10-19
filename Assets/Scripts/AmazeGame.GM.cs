using UnityEngine;
using UnityEngine.UI;

namespace AmazeRebuild
{
    public sealed partial class AmazeGame
    {
        void BuildGMQuickNavigation()
        {
            var previous = Button(page, "上一关", 38, 1263, 210, 52,
                () => QuickGMJump(-1), Brown, size: 24);
            previous.name = "GMPreviousLevel";
            previous.interactable = CurrentLevel > 1;
            var open = Button(page, "GM 跳关", 270, 1263, 210, 52, ShowGM, size: 24);
            open.name = "GMOpenLevelPicker";
            var next = Button(page, "下一关", 502, 1263, 210, 52,
                () => QuickGMJump(1), Brown, size: 24);
            next.name = "GMNextLevel";
            next.interactable = CurrentLevel < LevelCount;
        }

        void QuickGMJump(int offset)
        {
            if (!TryGMJump((CurrentLevel + offset).ToString(), out string error) && statusLabel)
                statusLabel.text = error;
        }

        // All GM entry points share validation and the normal board/save lifecycle.
        public bool TryGMJump(string input, out string error)
        {
            if (busy) { error = "箭头正在移动，请稍后再试。"; return false; }
            if (!int.TryParse(input, out int number) || number < 1 || number > LevelCount)
            { error = "请输入 1–" + LevelCount + " 的整数关卡编号。"; return false; }
            StartLevel(number, false);
            error = "";
            return true;
        }

        public void ShowGM()
        {
            if (busy) return;
            var panel = Modal("GM 关卡工具", 830);
            Text(panel, "当前关卡：" + CurrentLevel + "    范围：1–" + LevelCount,
                40, 105, 520, 48, 25, Brown);
            var box = Image(panel, "GMLevelInput", 60, 177, 480, 76, Color.white);
            box.raycastTarget = true;
            var input = box.gameObject.AddComponent<InputField>();
            input.targetGraphic = box;
            input.textComponent = Text(box.transform, "", 18, 0, 444, 76, 32, Navy, TextAnchor.MiddleLeft);
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 10;
            input.text = CurrentLevel.ToString();
            var feedback = Text(panel, "跳转会重置本关，恢复 3 点生命并保存进度。",
                45, 265, 510, 80, 23, Brown);
            System.Action<string> jump = value =>
            {
                if (!TryGMJump(value, out string error)) feedback.text = error;
            };
            Button(panel, "跳转并保存", 60, 357, 480, 76, () => jump(input.text));
            var previous = Button(panel, "上一关", 60, 454, 230, 65,
                () => jump((CurrentLevel - 1).ToString()), Brown, size: 25);
            var next = Button(panel, "下一关", 310, 454, 230, 65,
                () => jump((CurrentLevel + 1).ToString()), Brown, size: 25);
            previous.interactable = CurrentLevel > 1;
            next.interactable = CurrentLevel < LevelCount;
            Button(panel, "重开当前关卡", 60, 540, 480, 65,
                () => jump(CurrentLevel.ToString()), Brown, size: 25);
            Button(panel, "活动调试入口", 60, 628, 480, 60, ShowActivityGM, Brown, size: 25);
            Button(panel, "关闭", 155, 734, 290, 60, CloseModal, size: 25);
        }
    }
}
