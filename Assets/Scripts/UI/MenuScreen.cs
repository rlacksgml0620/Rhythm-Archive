using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static RhythmArchive.UIFactory;

namespace RhythmArchive
{
    /// <summary>곡 선택 + 난이도 + 속도/싱크 설정 화면.</summary>
    public class MenuScreen
    {
        public readonly GameObject Root;
        public event Action<SongEntry, ChartData> PlayRequested;
        public event Action CalibrateRequested;

        readonly List<(Button button, SongEntry song)> songButtons = new List<(Button, SongEntry)>();
        readonly RectTransform diffRow;
        readonly List<Button> diffButtons = new List<Button>();
        readonly Text bestText, speedText, offsetText;

        SongEntry selectedSong;
        int selectedChart;

        public MenuScreen(Transform canvas)
        {
            var bg = Panel("MenuScreen", canvas, PanelBg);
            Root = bg.gameObject;
            var root = bg.transform;

            Label(root, "RHYTHM ARCHIVE", 92, Accent, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.At(Top, new Vector2(0, -170), new Vector2(1000, 140));
            Label(root, "Tap the notes on the beat", 36, TextDim)
                .rectTransform.At(Top, new Vector2(0, -280), new Vector2(1000, 60));

            // 곡 목록(스크롤)
            Label(root, "SONGS", 34, TextDim, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.At(Top, new Vector2(0, -360), new Vector2(960, 50));
            var list = Rect("SongList", root).At(Top, new Vector2(0, -420), new Vector2(960, 500));
            list.gameObject.AddComponent<RectMask2D>();
            var listBg = list.gameObject.AddComponent<Image>();
            listBg.color = new Color(1, 1, 1, 0.03f);
            var content = Rect("Content", list);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = list;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            foreach (var song in SongLibrary.Songs)
            {
                var s = song;
                var b = Button(content,
                    $"{s.Title}\n<size=30><color=#A0A8C0>{s.Artist}  ·  BPM {s.Bpm:0}</color></size>", 48,
                    () => SelectSong(s));
                b.gameObject.AddComponent<LayoutElement>().preferredHeight = 150;
                songButtons.Add((b, s));
            }

            // 난이도
            Label(root, "DIFFICULTY", 34, TextDim, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.At(Top, new Vector2(0, -960), new Vector2(960, 50));
            diffRow = Rect("Difficulties", root).At(Top, new Vector2(0, -1020), new Vector2(960, 150));
            var hlg = diffRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            bestText = Label(root, "", 36, TextDim);
            bestText.rectTransform.At(Top, new Vector2(0, -1195), new Vector2(960, 50));

            // 속도
            speedText = SettingRow(root, "SPEED", -1290,
                () => { GameSettings.NoteSpeed -= 0.5f; Refresh(); },
                () => { GameSettings.NoteSpeed += 0.5f; Refresh(); });

            // 싱크 오프셋
            offsetText = SettingRow(root, "OFFSET", -1420,
                () => { GameSettings.OffsetMs -= 5f; Refresh(); },
                () => { GameSettings.OffsetMs += 5f; Refresh(); });

            Button(root, "AUTO SYNC CALIBRATION", 38, () => CalibrateRequested?.Invoke())
                .GetComponent<RectTransform>().At(Top, new Vector2(0, -1550), new Vector2(700, 110));

            var play = Button(root, "PLAY", 80, OnPlay, new Color(Accent.r, Accent.g, Accent.b, 0.85f));
            play.GetComponent<RectTransform>().At(Bottom, new Vector2(0, 150), new Vector2(700, 190));
            play.GetComponentInChildren<Text>().color = new Color(0.03f, 0.05f, 0.1f);

            Label(root, "PC: D  F  J  K  keys", 28, TextDim)
                .rectTransform.At(Bottom, new Vector2(0, 50), new Vector2(960, 50));

            if (songButtons.Count > 0) SelectSong(songButtons[0].song);
        }

        Text SettingRow(Transform root, string title, float y, Action minus, Action plus)
        {
            Label(root, title, 40, TextMain, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.At(Top, new Vector2(-230, y), new Vector2(500, 110));
            Button(root, "-", 64, minus).GetComponent<RectTransform>().At(Top, new Vector2(60, y), new Vector2(130, 110));
            var value = Label(root, "", 44, Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            value.rectTransform.At(Top, new Vector2(240, y), new Vector2(220, 110));
            Button(root, "+", 64, plus).GetComponent<RectTransform>().At(Top, new Vector2(420, y), new Vector2(130, 110));
            return value;
        }

        void SelectSong(SongEntry song)
        {
            if (selectedSong != song)
            {
                selectedSong = song;
                selectedChart = Mathf.Min(1, song.Charts.Count - 1); // 기본: NORMAL
                RebuildDifficulties();
            }
            Refresh();
        }

        void RebuildDifficulties()
        {
            foreach (var b in diffButtons) UnityEngine.Object.Destroy(b.gameObject);
            diffButtons.Clear();
            for (int i = 0; i < selectedSong.Charts.Count; i++)
            {
                int idx = i;
                var c = selectedSong.Charts[i];
                var b = Button(diffRow, $"{c.difficulty}\n<size=34>Lv.{c.level}</size>", 44, () =>
                {
                    selectedChart = idx;
                    Refresh();
                });
                diffButtons.Add(b);
            }
        }

        public static Color DifficultyColor(string diff) => diff switch
        {
            "EASY" => new Color(0.4f, 0.95f, 0.55f),
            "NORMAL" => new Color(1f, 0.8f, 0.3f),
            "HARD" => new Color(1f, 0.4f, 0.4f),
            _ => new Color(0.75f, 0.5f, 1f),
        };

        public void Refresh()
        {
            foreach (var (button, song) in songButtons)
                button.SetBg(song == selectedSong ? new Color(Accent.r, Accent.g, Accent.b, 0.3f) : ButtonBg);

            for (int i = 0; i < diffButtons.Count; i++)
            {
                var c = DifficultyColor(selectedSong.Charts[i].difficulty);
                diffButtons[i].SetBg(i == selectedChart ? new Color(c.r, c.g, c.b, 0.55f) : ButtonBg);
            }

            if (selectedSong != null && selectedChart >= 0 && selectedChart < selectedSong.Charts.Count)
            {
                var chart = selectedSong.Charts[selectedChart];
                int best = GameSettings.GetBestScore(selectedSong.Id, chart.difficulty);
                bestText.text = best > 0
                    ? $"BEST  {best:N0}  ({ScoreKeeper.RankOf(best)})   ·   {chart.notes.Count} notes"
                    : $"NO RECORD   ·   {chart.notes.Count} notes";
            }

            speedText.text = GameSettings.NoteSpeed.ToString("0.0");
            float off = GameSettings.OffsetMs;
            offsetText.text = (off > 0 ? "+" : "") + off.ToString("0") + "ms";
        }

        void OnPlay()
        {
            if (selectedSong == null || selectedSong.Charts.Count == 0) return;
            PlayRequested?.Invoke(selectedSong, selectedSong.Charts[selectedChart]);
        }

        public void Show()
        {
            Root.SetActive(true);
            Refresh();
        }

        public void Hide() => Root.SetActive(false);
    }
}
