using System;
using UnityEngine;
using UnityEngine.UI;
using static RhythmArchive.UIFactory;

namespace RhythmArchive
{
    public class ResultScreen
    {
        public readonly GameObject Root;
        public event Action RetryRequested, MenuRequested;

        readonly Text titleText, rankText, clearText, scoreText, bestText, labelsText, valuesText;

        public ResultScreen(Transform canvas)
        {
            var bg = Panel("ResultScreen", canvas, PanelBg);
            Root = bg.gameObject;
            var root = bg.transform;

            titleText = Label(root, "", 44, TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
            titleText.rectTransform.At(Top, new Vector2(0, -150), new Vector2(1000, 70));
            rankText = Label(root, "", 260, Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            rankText.rectTransform.At(Top, new Vector2(0, -250), new Vector2(1000, 300));
            clearText = Label(root, "", 56, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            clearText.rectTransform.At(Top, new Vector2(0, -560), new Vector2(1000, 80));
            scoreText = Label(root, "", 100, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            scoreText.rectTransform.At(Top, new Vector2(0, -650), new Vector2(1000, 130));
            bestText = Label(root, "", 40, new Color(1f, 0.86f, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
            bestText.rectTransform.At(Top, new Vector2(0, -785), new Vector2(1000, 60));

            labelsText = Label(root, "", 46, TextDim, TextAnchor.UpperLeft, FontStyle.Bold);
            labelsText.lineSpacing = 1.2f;
            labelsText.rectTransform.At(Top, new Vector2(-150, -900), new Vector2(400, 600));
            valuesText = Label(root, "", 46, TextMain, TextAnchor.UpperRight, FontStyle.Bold);
            valuesText.lineSpacing = 1.2f;
            valuesText.rectTransform.At(Top, new Vector2(150, -900), new Vector2(400, 600));

            Button(root, "RETRY", 52, () => RetryRequested?.Invoke())
                .GetComponent<RectTransform>().At(Bottom, new Vector2(-210, 150), new Vector2(380, 160));
            Button(root, "MENU", 52, () => MenuRequested?.Invoke(), new Color(Accent.r, Accent.g, Accent.b, 0.4f))
                .GetComponent<RectTransform>().At(Bottom, new Vector2(210, 150), new Vector2(380, 160));
        }

        public void Show(SongEntry song, ChartData chart, ScoreKeeper score, bool newBest)
        {
            Root.SetActive(true);
            titleText.text = $"{song.Title}  ·  <color=#{ColorUtility.ToHtmlStringRGB(MenuScreen.DifficultyColor(chart.difficulty))}>{chart.difficulty} Lv.{chart.level}</color>";

            string rank = ScoreKeeper.RankOf(score.Score);
            rankText.text = rank;
            rankText.color = rank.StartsWith("S") ? new Color(1f, 0.86f, 0.35f) : rank == "A" ? Accent : TextMain;

            if (score.IsAllPerfect) { clearText.text = "ALL PERFECT"; clearText.color = new Color(1f, 0.86f, 0.35f); }
            else if (score.IsFullCombo) { clearText.text = "FULL COMBO"; clearText.color = Accent; }
            else { clearText.text = "CLEAR"; clearText.color = TextMain; }

            scoreText.text = score.Score.ToString("N0");
            bestText.text = newBest ? "NEW BEST!" : "";

            labelsText.text = "PERFECT\nGREAT\nGOOD\nMISS\n\nMAX COMBO\nFAST / SLOW";
            valuesText.text =
                $"{score.Count(Judgement.Perfect)}\n{score.Count(Judgement.Great)}\n" +
                $"{score.Count(Judgement.Good)}\n{score.Count(Judgement.Miss)}\n\n" +
                $"{score.MaxCombo}\n{score.FastCount} / {score.SlowCount}";
        }

        public void Hide() => Root.SetActive(false);
    }
}
