using System;
using UnityEngine;
using UnityEngine.UI;
using static RhythmArchive.UIFactory;

namespace RhythmArchive
{
    /// <summary>플레이 중 점수/콤보/판정/진행도 표시.</summary>
    public class HudScreen
    {
        public readonly GameObject Root;
        public event Action PauseRequested;

        readonly Text scoreText, accuracyText, titleText, comboText, comboLabel, judgeText, fastSlowText, countdownText;
        readonly RectTransform progressFill;

        float judgeTimer;
        const float JudgeShowTime = 0.45f;

        public HudScreen(Transform canvas)
        {
            var root = Rect("HudScreen", canvas).Stretch();
            Root = root.gameObject;

            var progressBg = Rect("ProgressBg", root);
            progressBg.anchorMin = new Vector2(0, 1);
            progressBg.anchorMax = new Vector2(1, 1);
            progressBg.pivot = new Vector2(0.5f, 1);
            progressBg.sizeDelta = new Vector2(0, 8);
            progressBg.anchoredPosition = Vector2.zero;
            progressBg.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.1f);
            progressFill = Rect("ProgressFill", progressBg);
            progressFill.anchorMin = Vector2.zero;
            progressFill.anchorMax = new Vector2(0, 1);
            progressFill.offsetMin = Vector2.zero;
            progressFill.offsetMax = Vector2.zero;
            progressFill.gameObject.AddComponent<Image>().color = Accent;

            scoreText = Label(root, "0000000", 64, TextMain, TextAnchor.UpperLeft, FontStyle.Bold);
            scoreText.rectTransform.At(TopLeft, new Vector2(40, -40), new Vector2(600, 80));
            accuracyText = Label(root, "100.00%", 32, TextDim, TextAnchor.UpperLeft);
            accuracyText.rectTransform.At(TopLeft, new Vector2(44, -118), new Vector2(600, 50));
            titleText = Label(root, "", 30, TextDim, TextAnchor.UpperLeft);
            titleText.rectTransform.At(TopLeft, new Vector2(44, -168), new Vector2(700, 50));

            Button(root, "II", 56, () => PauseRequested?.Invoke())
                .GetComponent<RectTransform>().At(TopRight, new Vector2(-36, -36), new Vector2(140, 140));

            comboText = Label(root, "", 150, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            comboText.rectTransform.At(Center, new Vector2(0, 300), new Vector2(800, 180));
            comboLabel = Label(root, "COMBO", 34, TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
            comboLabel.rectTransform.At(Center, new Vector2(0, 190), new Vector2(400, 50));

            judgeText = Label(root, "", 92, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            judgeText.rectTransform.At(Center, new Vector2(0, -170), new Vector2(900, 120));
            fastSlowText = Label(root, "", 36, TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
            fastSlowText.rectTransform.At(Center, new Vector2(0, -250), new Vector2(400, 50));

            countdownText = Label(root, "", 160, Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            countdownText.rectTransform.At(Center, new Vector2(0, 40), new Vector2(600, 200));
        }

        public void Show(string title)
        {
            Root.SetActive(true);
            titleText.text = title;
            judgeText.text = "";
            fastSlowText.text = "";
            comboText.text = "";
            comboLabel.enabled = false;
            judgeTimer = 0;
        }

        public void Hide() => Root.SetActive(false);

        public void OnJudged(Judgement j, double delta)
        {
            judgeText.text = j.ToString().ToUpperInvariant();
            judgeText.color = Playfield.JudgementColor(j);
            if (j == Judgement.Miss || j == Judgement.Perfect || double.IsNaN(delta)) fastSlowText.text = "";
            else
            {
                fastSlowText.text = delta < 0 ? "FAST" : "SLOW";
                fastSlowText.color = delta < 0 ? Accent : Accent2;
            }
            judgeTimer = JudgeShowTime;
        }

        public void Tick(GameplayController game)
        {
            var score = game.Score;
            if (score == null) return;

            scoreText.text = score.Score.ToString("D7");
            accuracyText.text = score.Accuracy.ToString("0.00") + "%";

            bool showCombo = score.Combo >= 2;
            comboText.text = showCombo ? score.Combo.ToString() : "";
            comboLabel.enabled = showCombo;

            var a = progressFill.anchorMax;
            a.x = (float)game.Progress;
            progressFill.anchorMax = a;

            // 판정 텍스트: 처음에 크게 튀어나왔다가 사라짐
            if (judgeTimer > 0)
            {
                judgeTimer -= Time.unscaledDeltaTime;
                float t = 1f - judgeTimer / JudgeShowTime;
                float scale = t < 0.15f ? Mathf.Lerp(1.3f, 1f, t / 0.15f) : 1f;
                judgeText.rectTransform.localScale = new Vector3(scale, scale, 1);
                var c = judgeText.color;
                c.a = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
                judgeText.color = c;
                if (judgeTimer <= 0) { judgeText.text = ""; fastSlowText.text = ""; }
            }

            // 시작 전 카운트다운
            double st = game.SongTime;
            countdownText.text = st < -0.2 ? Math.Ceiling(-st).ToString("0") : "";
        }
    }
}
