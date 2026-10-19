using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using AmazeRebuild;

public static class HomePrefabBuilder
{
    public const string PathName = "Assets/Resources/UI/HomePage.prefab";
    static Font font;
    static readonly Color Brown = new Color32(147, 96, 47, 255), Purple = new Color32(150, 62, 211, 255);
    static RectTransform R(string name, Transform parent, float x, float y, float w, float h)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
    }
    static Sprite Art(string name)
    {
        string target = "Assets/HomeUI/Sprites/" + name + ".png";
        if (!File.Exists(target)) AssetDatabase.CopyAsset("Assets/Resources/Art/" + name + ".png", target);
        var importer = (TextureImporter)AssetImporter.GetAtPath(target);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        if (name == "CommonBtnBigNew") importer.spriteBorder = new Vector4(35, 35, 35, 35);
        importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(target);
    }
    static Image I(string name, Transform parent, float x, float y, float w, float h, Color color, string art = null)
    {
        var image = R(name, parent, x, y, w, h).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
        if (art != null) { image.sprite = Art(art); if (art == "CommonBtnBigNew") image.type = Image.Type.Sliced; }
        return image;
    }
    static Text T(string name, string value, Transform parent, float x, float y, float w, float h, int size, Color color, bool bold = false)
    {
        var text = R(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
        text.font = font; text.text = value; text.fontSize = size; text.color = color;
        text.alignment = TextAnchor.MiddleCenter; text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.raycastTarget = false; return text;
    }
    static Button B(string name, string label, Transform parent, float x, float y, float w, float h, Color color, int size = 30, string art = "CommonBtnBigNew")
    {
        var image = I(name, parent, x, y, w, h, color, art); image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.pressedColor = new Color(.82f,.82f,.82f); button.colors = colors;
        if (label != null) T("Label", label, image.transform, 4, 0, w-8, h, size, Color.white, true);
        return button;
    }
    [MenuItem("Amaze GO/Open Home Prefab")]
    public static void Open() { AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(PathName)); }
    // One-time migration only. Never run automatically: designer edits must remain intact.
    public static void CreateAndBuild()
    {
        if (File.Exists(PathName)) throw new InvalidOperationException("Home prefab already exists; refusing to overwrite designer changes.");
        Directory.CreateDirectory("Assets/Resources/UI"); Directory.CreateDirectory("Assets/HomeUI/Sprites"); AssetDatabase.Refresh();
        font = Resources.Load<Font>("Fonts/Chinese");
        var root = R("HomePage", null, 0, 0, 750, 1334);
        var view = root.gameObject.AddComponent<HomePageView>();
        view.background = I("Background", root, 0, 0, 750, 1334, view.cream);
        var header = R("Header", root, 0, 0, 750, 120);
        I("EnergyIcon", header, 530, 40, 27, 35, new Color32(57,181,220,255), "CommonIconHeart1");
        T("EnergyCount", "1", header, 567, 30, 44, 52, 34, Brown, true);
        view.settings = B("Settings", null, header, 661, 37, 45, 45, view.normalInk, art:"CommonIconSet");
        view.settingsIcon = view.settings.image; view.settingsIcon.preserveAspect = true;
        var cards = R("Activities", root, 0, 142, 750, 346);
        string[] names = { "League", "Daily", "Shark" }, titles = { "联赛", "每日挑战", "大逃鲨" };
        string[] artwork = { "League_Home_card_beige", "HomeDaiyCardBeigeLock", "SharkCard" };
        var buttons = new Button[3]; var labels = new Text[3];
        for (int i=0;i<3;i++)
        {
            buttons[i] = B(names[i], null, cards, 24+i*238, 0, 226, 346, Color.white, art:artwork[i]);
            T("Title",titles[i],buttons[i].transform,8,16,210,50,29,Color.white,true);
            I("StatusPill",buttons[i].transform,10,263,206,65,Brown,"CommonBtnBigNew");
            labels[i]=T("Status",i==0?"本周 0 分":i==1?"10月9日":"0 / 5 关",buttons[i].transform,14,263,198,65,23,Color.white,true);
        }
        view.league=buttons[0]; view.daily=buttons[1]; view.shark=buttons[2];
        view.leagueProgress=labels[0]; view.dailyDate=labels[1]; view.sharkProgress=labels[2];
        view.wallet=T("Wallet","活动币 0    奖章 0",root,70,515,610,45,25,Brown);
        view.title=T("Title","Amaze GO!",root,100,636,550,80,53,view.normalInk,true);
        T("Subtitle","找到出口，让思绪自由",root,100,715,550,45,24,Brown);
        var progress=R("LevelProgress",root,0,857,750,70);
        I("Line",progress,106,23,540,22,new Color32(233,208,169,255),"line");
        view.levels=new Button[5];view.levelLabels=new Text[5];
        for(int i=0;i<5;i++)
        {
            view.levels[i]=B("Level"+(i+1),(5+i).ToString(),progress,117+i*107,0,66,66,i==0?Purple:view.otherLevelColor,33,"Circle");
            var label=view.levels[i].GetComponentInChildren<Text>(); view.levelLabels[i]=label;
            label.resizeTextForBestFit=true;label.resizeTextMinSize=18;label.resizeTextMaxSize=33;label.horizontalOverflow=HorizontalWrapMode.Overflow;
        }
        var actions=R("Actions",root,0,982,750,255);
        view.continueGame=B("Continue","继续\n关卡5",actions,78,0,594,110,Purple,36);
        view.continueLabel=view.continueGame.GetComponentInChildren<Text>();
        view.levelPicker=B("LevelPicker","选择关卡",actions,230,141,290,60,new Color32(173,126,78,255),25);
        view.gm=B("GM","GM 关卡",actions,230,211,290,44,Brown,22);
        view.footer=T("Footer","离线重建 · 997 个原始关卡",root,100,1240,550,32,19,Brown);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,PathName); UnityEngine.Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets(); Validate(); GMBuildValidation.Build();
    }
    public static void Validate()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PathName);
        if (!prefab) throw new Exception("Missing home prefab");
        var view=prefab.GetComponent<HomePageView>();
        var data=new SerializedObject(view); var it=data.GetIterator();
        while(it.NextVisible(true)) if(it.propertyType==SerializedPropertyType.ObjectReference && it.objectReferenceValue==null) throw new Exception("Missing binding: "+it.propertyPath);
        foreach(var image in prefab.GetComponentsInChildren<Image>(true))
            if(image.sprite && !AssetDatabase.Contains(image.sprite)) throw new Exception("Nonpersistent sprite: "+image.name);
        if(view.levels.Length!=5 || prefab.GetComponentsInChildren<Button>().Length!=12) throw new Exception("Home button count mismatch");
        Debug.Log("HOME PREFAB VALIDATION PASS");
    }
}
