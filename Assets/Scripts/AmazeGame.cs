using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AmazeRebuild
{
    [Serializable] public sealed class SaveData { public int level = 5, lives = 3, best = 5; public int[] removed = new int[0]; }
    public sealed partial class AmazeGame : MonoBehaviour
    {
        public const string SaveKey = "AmazeRebuild.Save.v1";
        public static AmazeGame Instance { get; private set; }
        public BoardModel Model { get; private set; }
        public BoardView Board { get; private set; }
        public int Lives => save.lives;
        public int CurrentLevel => save.level;
        public int LevelCount => catalog.levels.Length;
        public bool Busy => busy;
        public string ScreenName { get; private set; }
        RectTransform root, page, modal;
        Text remainingLabel, statusLabel;
        Image[] lifeIcons;
        Font font;
        SaveData save;
        LevelCatalog catalog;
        bool busy, sound = true, grid;
        int theme;
        AudioSource audioSource;
        AudioClip goodClip, badClip;
        readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Color Cream = new Color32(227, 241, 255, 255);
        static readonly Color Brown = new Color32(65, 107, 151, 255);
        static readonly Color Navy = new Color32(25, 44, 58, 255);
        static readonly Color Purple = new Color32(49, 149, 239, 255);
        static readonly Color Blue = new Color32(57, 181, 220, 255);
        Color Background => ScreenName == "Game" ? (theme == 1 ? new Color32(38, 54, 57, 255) : theme == 2 ? new Color32(28, 33, 48, 255) : new Color32(59, 62, 86, 255)) : theme == 1 ? new Color32(215, 239, 226, 255) : theme == 2 ? new Color32(46, 64, 79, 255) : Cream;
        Color Ink => ScreenName == "Game" || theme == 2 ? new Color32(245, 244, 231, 255) : Navy;
        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            font = Resources.Load<Font>("Fonts/Chinese");
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            catalog = JsonUtility.FromJson<LevelCatalog>(Resources.Load<TextAsset>("catalog").text);
            try { save = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey, "")); } catch { save = null; }
            if (save == null) save = new SaveData();
            save.level = Mathf.Clamp(save.level, 1, catalog.levels.Length);
            save.lives = Mathf.Clamp(save.lives, 0, 3);
            sound = PlayerPrefs.GetInt("AmazeRebuild.Sound", 1) != 0;
            theme = PlayerPrefs.GetInt("AmazeRebuild.Theme", 0);
            grid = PlayerPrefs.GetInt("AmazeRebuild.MinimalGrid", 0) != 0;
            audioSource = gameObject.AddComponent<AudioSource>();
            goodClip = Tone(720, .09f); badClip = Tone(190, .16f);
            InitActivities(); BuildCanvas(); Home();
            if (Environment.GetCommandLineArgs().Contains("-amazeSmoke")) gameObject.AddComponent<RuntimeSmoke>();
        }
        void BuildCanvas()
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(750, 1334);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (!FindObjectOfType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            root = Rect("PhoneFrame", canvasGO.transform, 0, 0, 750, 1334);
            root.anchorMin = root.anchorMax = new Vector2(.5f, .5f); root.pivot = new Vector2(.5f, .5f); root.anchoredPosition = Vector2.zero;
            var bg = root.gameObject.AddComponent<Image>(); bg.color = Background;
            CandyBackdrop.Attach(root);
            root.gameObject.AddComponent<RectMask2D>();
            Camera.main.backgroundColor = new Color32(31, 36, 43, 255);
        }
        static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        Sprite Sprite(string name)
        {
            if (sprites.TryGetValue(name, out var sprite)) return sprite;
            var texture = Resources.Load<Texture2D>("Art/" + name);
            if (!texture) return null;
            bool sliced = name == "CommonBtnBigNew" || name == "CommonAreaBg";
            sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, sliced ? new Vector4(35, 35, 35, 35) : Vector4.zero);
            sprites[name] = sprite; return sprite;
        }
        Image Image(Transform parent, string name, float x, float y, float w, float h, Color color, string art = null)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            if (art != null) { image.sprite = Sprite(art); if (art == "CommonBtnBigNew" || art == "CommonAreaBg") image.type = UnityEngine.UI.Image.Type.Sliced; }
            return image;
        }
        Text Text(Transform parent, string value, float x, float y, float w, float h, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false)
        {
            var text = Rect(value.Length > 16 ? value.Substring(0, 16) : value, parent, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color; text.alignment = alignment;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow; return text;
        }
        Button Button(Transform parent, string label, float x, float y, float w, float h, Action action, Color? tint = null, string art = "CommonBtnBigNew", int size = 30)
        {
            var img = Image(parent, label, x, y, w, h, tint ?? Purple, art); img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>(); button.targetGraphic = img;
            var colors = button.colors; colors.pressedColor = new Color(.82f, .82f, .82f); button.colors = colors;
            if (art == "CommonBtnBigNew" && ScreenName != "Game")
            {
                CandyButtonSurface.Attach(img);
                var shadow = img.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(.08f, .18f, .30f, .28f); shadow.effectDistance = new Vector2(0, -4);
            }
            if (label.Length > 0) Text(img.transform, label, 4, 0, w - 8, h, size, Color.white, TextAnchor.MiddleCenter, true);
            button.onClick.AddListener(() => action()); return button;
        }
        void IconButton(Transform parent, string name, float x, float y, Action action, Color color, float size = 48)
        { Button(parent, "", x, y, size, size, action, color, name).GetComponent<Image>().preserveAspect = true; }
        void ClearPage(string name)
        {
            CloseModal(); if (page) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            page = Rect(name, root, 0, 0, 750, 1334); ScreenName = name;
            root.GetComponent<Image>().color = Background;
            root.Find("Candy sky").gameObject.SetActive(theme == 0 && ScreenName != "Game");
        }
        public void Home()
        {
            if (busy) return;
            ExitActivity();
            activities.Refresh(DateTime.Today); activities.Store();
            var prefab = Resources.Load<HomePageView>("UI/HomePage");
            if (!prefab) throw new InvalidOperationException("Missing Assets/Resources/UI/HomePage.prefab");
            CloseModal();
            if (page) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            var view = Instantiate(prefab, root, false);
            page = (RectTransform)view.transform;
            ScreenName = "Home"; Board = null; Model = null;
            root.GetComponent<Image>().color = Background;
            root.Find("Candy sky").gameObject.SetActive(theme == 0 && ScreenName != "Game");
            view.Bind(this, theme, () => LevelPicker(0));
        }
        public void StartLevel(int number, bool resume = false)
        {
            if (busy) return;
            ExitActivity();
            StartBoard(number, resume);
        }
        void StartBoard(int number, bool resume = false)
        {
            if (busy) return;
            number = Mathf.Clamp(number, 1, catalog.levels.Length);
            bool restore = resume && number == save.level && save.lives > 0;
            if (!restore) { save.lives = 3; save.removed = new int[0]; }
            save.level = number;
            Model = new BoardModel(LevelData.Load(number));
            if (restore) Model.RestoreRemoved(save.removed ?? new int[0]);
            if (Model.Remaining == 0) { save.removed = new int[0]; Model = new BoardModel(LevelData.Load(number)); }
            ClearPage("Game");
            Color boardInk = new Color32(214, 220, 237, 255);
            IconButton(page, "CommonIconBack", 32, 35, ReturnFromBoard, Ink, 42);
            Text(page, BoardTitle(number), 232, 22, 285, 57, 34, boardInk, bold: true);
            IconButton(page, "CommonIconSet", 670, 31, Settings, Ink, 43);
            lifeIcons = new Image[3];
            for (int i = 0; i < 3; i++) lifeIcons[i] = Image(page, "Life " + i, 43 + i * 37, 101, 24, 32, Blue, "CommonIconHeart1");
            remainingLabel = Text(page, "", 453, 91, 245, 50, 22, boardInk, TextAnchor.MiddleRight);
            var clip = Rect("BoardViewport", page, 13, 166, 724, 850); clip.gameObject.AddComponent<RectMask2D>();
            var boardRect = Rect("ArrowBoard", clip, 0, 0, 724, 850);
            boardRect.pivot = new Vector2(.5f, .5f); boardRect.anchoredPosition = new Vector2(362, -425);
            Board = boardRect.gameObject.AddComponent<BoardView>(); Board.Ink = Ink; Board.ShowGrid = grid; Board.Bind(Model); Board.Tapped = TapArrow;
            statusLabel = Text(page, number < 4 ? "点击箭头，让它沿箭头方向离开" : "先移走前方没有阻挡的箭头", 50, 1023, 650, 44, 24, boardInk);
            IconButton(page, "MinusIcon", 55, 1144, () => Board.SetZoom(Board.Zoom / 1.18f), boardInk, 36);
            IconButton(page, "PlusIcon", 134, 1144, () => Board.SetZoom(Board.Zoom * 1.18f), boardInk, 36);
            var hint = Button(page, "", 320, 1100, 110, 110, Hint, new Color32(83, 89, 119, 255), "CommonBtnProps");
            Image(hint.transform, "Bulb", 28, 14, 55, 72, Color.white, "bulb");
            Text(page, "提示", 320, 1213, 110, 36, 23, boardInk);
            IconButton(page, "style3_guideline", 502, 1128, ToggleGrid, boardInk, 50);
            IconButton(page, "CommonIconSkin", 641, 1131, ThemeSettings, boardInk, 47);
            if (activityMode == "Main") BuildGMQuickNavigation();
            else Text(page, "拖动平移 · 滚轮 / 双指缩放", 100, 1273, 550, 31, 18, boardInk);
            UpdateHUD(); Save();
        }
        void UpdateHUD()
        {
            if (lifeIcons != null) for (int i = 0; i < lifeIcons.Length; i++) if (lifeIcons[i]) lifeIcons[i].color = i < save.lives ? Blue : (Color)new Color32(210, 205, 188, 255);
            if (remainingLabel) remainingLabel.text = "剩余 " + Model.Remaining + " / " + Model.Paths.Length;
        }
        public void TapArrow(int index)
        {
            if (busy || ScreenName != "Game" || modal || save.lives == 0 || index < 0 || index >= Model.Paths.Length || Model.Removed[index]) return;
            StartCoroutine(Move(index));
        }
        IEnumerator Move(int index)
        {
            busy = true; Board.HintIndex = -1; Board.AnimatedIndex = index;
            int blocker = Model.Blocker(index, out int freeSteps);
            bool success = blocker < 0;
            float distance = success ? Model.Paths[index].Count + Mathf.Max(Model.Level.XSize, Model.Level.YSize) + 9 : freeSteps + .6f;
            float duration = success ? Mathf.Clamp(distance * .013f, .24f, .7f) : .22f;
            if (success) { Model.TryRemove(index); statusLabel.text = "很好！继续寻找出口"; }
            else { Board.FailedIndex = index; statusLabel.text = "前方被挡住了，先移走阻挡的箭头"; save.lives--; }
            if (sound) audioSource.PlayOneShot(success ? goodClip : badClip, .28f);
            UpdateHUD();
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            { Board.Travel = distance * Mathf.SmoothStep(0, 1, t / duration); Board.Refresh(); yield return null; }
            if (!success)
            {
                for (float t = 0; t < .22f; t += Time.unscaledDeltaTime)
                { Board.Travel = distance * (1 - Mathf.SmoothStep(0, 1, t / .22f)); Board.Refresh(); yield return null; }
            }
            Board.Travel = 0; Board.AnimatedIndex = Board.FailedIndex = -1; Board.Refresh(); busy = false; Save();
            if (Model.Remaining == 0) Win(); else if (save.lives == 0) Lose();
        }
        public void Hint()
        {
            if (busy || Board == null || modal) return;
            Board.HintIndex = Model.Hint(); Board.Refresh();
            statusLabel.text = Board.HintIndex < 0 ? "当前没有可移动箭头" : "金色箭头可以安全离开";
        }
        void Win()
        {
            if (activityMode != "Main") { ActivityWin(); return; }
            activities.Refresh(DateTime.Today); activities.AwardLeague(save.level);
            int completed = save.level;
            save.best = Mathf.Max(save.best, Mathf.Min(catalog.levels.Length, completed + 1));
            save.removed = new int[0]; save.lives = 3;
            save.level = Mathf.Min(catalog.levels.Length, completed + 1);
            Persist();
            var panel = Modal("关卡完成！");
            Image(panel, "Star", 239, 108, 122, 122, new Color32(243, 181, 44, 255), "ProgressStar");
            Text(panel, "所有箭头都找到出口了", 40, 254, 520, 56, 28, Brown);
            Button(panel, completed == catalog.levels.Length ? "返回首页" : "下一关", 70, 356, 460, 85, () => { CloseModal(); if (completed == catalog.levels.Length) Home(); else StartLevel(save.level); });
        }
        void Lose()
        {
            if (ActivityLose()) return;
            var panel = Modal("再试一次");
            Text(panel, "生命值已用完\n观察箭头前方的路径，再重新挑战", 40, 133, 520, 122, 28, Brown);
            Button(panel, "重新开始", 70, 306, 460, 82, () => { CloseModal(); RetryBoard(); });
            Button(panel, "返回首页", 70, 408, 460, 62, () => { CloseModal(); Home(); }, new Color32(179, 139, 96, 255), size: 25);
        }
        RectTransform Modal(string title, float height = 550)
        {
            CloseModal();
            modal = Rect("ModalOverlay", root, 0, 0, 750, 1334);
            var shade = modal.gameObject.AddComponent<Image>(); shade.color = new Color(0, 0, 0, .46f);
            var panel = Image(modal, "Panel", 75, (1334 - height) / 2, 600, height, Cream, "CommonAreaBg").rectTransform;
            Text(panel, title, 38, 28, 524, 63, 38, Brown, bold: true); return panel;
        }
        void CloseModal() { if (modal) { modal.gameObject.SetActive(false); Destroy(modal.gameObject); modal = null; } }
        void Message(string title, string message)
        { var p = Modal(title); Text(p, message, 45, 125, 510, 218, 25, Brown); Button(p, "知道了", 95, 402, 410, 80, CloseModal); }
        public void Settings()
        {
            if (busy) return;
            var p = Modal("设置", 770);
            Button(p, "音效：" + (sound ? "开" : "关"), 60, 120, 480, 76, () => { sound = !sound; PlayerPrefs.SetInt("AmazeRebuild.Sound", sound ? 1 : 0); PlayerPrefs.Save(); Settings(); }, Brown);
            Button(p, "辅助网格：" + (grid ? "开" : "关"), 60, 219, 480, 76, () => { ToggleGrid(); Settings(); }, Brown);
            Button(p, "切换主题", 60, 318, 480, 76, ThemeSettings, Brown);
            Button(p, "选择关卡", 60, 417, 480, 76, () => LevelPicker(0), Brown);
            Button(p, "GM 关卡工具", 60, 516, 480, 76, ShowGM, Brown);
            Button(p, "返回", 60, 650, 480, 76, CloseModal);
        }
        void ToggleGrid()
        { grid = !grid; PlayerPrefs.SetInt("AmazeRebuild.MinimalGrid", grid ? 1 : 0); PlayerPrefs.Save(); if (Board) { Board.ShowGrid = grid; Board.Refresh(); } }
        void ThemeSettings()
        {
            if (busy) return;
            var p = Modal("选择主题");
            string[] labels = { "冰蓝亮面", "薄荷绿", "深海蓝" };
            Color[] colors = { Brown, new Color32(70, 144, 102, 255), Navy };
            for (int i = 0; i < 3; i++) { int choice = i; Button(p, labels[i], 60, 120 + i * 100, 480, 77, () => { theme = choice; PlayerPrefs.SetInt("AmazeRebuild.Theme", theme); PlayerPrefs.Save(); CloseModal(); if (ScreenName == "Game") StartBoard(save.level, true); else Home(); }, colors[i]); }
            Button(p, "返回", 155, 435, 290, 63, CloseModal);
        }
        void LevelPicker(int pageIndex)
        {
            if (busy) return;
            var p = Modal("选择关卡", 845); int pages = (catalog.levels.Length + 19) / 20;
            pageIndex = Mathf.Clamp(pageIndex, 0, pages - 1);
            for (int i = 0; i < 20; i++)
            {
                int n = pageIndex * 20 + i + 1; if (n > catalog.levels.Length) break;
                Button(p, n.ToString(), 38 + (i % 4) * 135, 117 + (i / 4) * 110, 119, 86, () => { CloseModal(); StartLevel(n); }, n == save.level ? Purple : Brown, size: 29);
            }
            Button(p, "上一页", 40, 685, 175, 63, () => LevelPicker(pageIndex - 1), Brown, size: 23);
            Text(p, (pageIndex + 1) + " / " + pages, 223, 685, 153, 63, 23, Brown);
            Button(p, "下一页", 385, 685, 175, 63, () => LevelPicker(pageIndex + 1), Brown, size: 23);
            Button(p, "返回", 187, 765, 226, 58, CloseModal, size: 23);
        }
        public void Save()
        {
            if (Model != null && ScreenName == "Game") save.removed = Enumerable.Range(0, Model.Removed.Length).Where(i => Model.Removed[i]).ToArray();
            Persist();
        }
        void Persist() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(mainSave ?? save)); if (activities != null) activities.Store(); PlayerPrefs.Save(); }
        void OnApplicationPause(bool pause) { if (pause && save != null) Persist(); }
        void OnApplicationQuit() { if (save != null) Persist(); }
        void Update()
        {
            if (!busy && activityMode == "Shark" && sessionDay != ActivityData.Day(DateTime.Today)) { ShowShark(); return; }
            if (Input.GetKeyDown(KeyCode.F1) && !busy) ShowGM();
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (modal) CloseModal(); else if (!busy) { if (ScreenName == "Game") ReturnFromBoard(); else Home(); }
        }
        static AudioClip Tone(float frequency, float length)
        {
            const int rate = 22050; int n = Mathf.CeilToInt(rate * length); var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Sin(i * frequency * Mathf.PI * 2 / rate) * (1 - (float)i / n) * .3f;
            var clip = AudioClip.Create("Tone", n, 1, rate, false); clip.SetData(data, 0); return clip;
        }
    }
}
