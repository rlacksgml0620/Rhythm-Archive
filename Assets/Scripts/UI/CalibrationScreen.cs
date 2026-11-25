using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static RhythmArchive.UIFactory;

namespace RhythmArchive
{
    /// <summary>
    /// 자동 싱크 보정. 메트로놈 소리에 맞춰 탭하면, 기기의 오디오 지연 + 손 반응을 측정해
    /// 판정 오프셋으로 저장한다. (블루투스 이어폰처럼 지연이 큰 환경에서 특히 중요)
    /// </summary>
    public class CalibrationScreen
    {
        const float Bpm = 100f;
        const int CountIn = 4;
        const int TapsNeeded = 16;
        const int TotalBeats = CountIn + TapsNeeded + 4;

        public readonly GameObject Root;
        public event Action BackRequested;

        readonly Conductor conductor;
        readonly Text statusText, resultText;
        readonly Image pulse;
        readonly Button startButton, applyButton;
        readonly List<double> deltas = new List<double>();
        AudioClip clip;
        bool running;
        float measuredMs;
        bool hasResult;

        static double BeatSec => 60.0 / Bpm;

        public CalibrationScreen(Transform canvas, Conductor conductor)
        {
            this.conductor = conductor;
            var bg = Panel("CalibrationScreen", canvas, PanelBg);
            Root = bg.gameObject;
            var root = bg.transform;

            Label(root, "SYNC CALIBRATION", 70, Accent, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.At(Top, new Vector2(0, -180), new Vector2(1000, 100));
            Label(root, "Tap anywhere (or press SPACE)\nexactly on each click.\nThe first 4 clicks are a count-in.", 38, TextDim)
                .rectTransform.At(Top, new Vector2(0, -300), new Vector2(1000, 200));

            var pulseRt = Rect("Pulse", root).At(Center, new Vector2(0, 120), new Vector2(320, 320));
            pulse = pulseRt.gameObject.AddComponent<Image>();
            pulse.sprite = RuntimeSprites.Circle;
            pulse.color = Accent;
            pulse.raycastTarget = false;

            statusText = Label(root, "", 52, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            statusText.rectTransform.At(Center, new Vector2(0, -170), new Vector2(1000, 80));
            resultText = Label(root, "", 44, TextDim);
            resultText.rectTransform.At(Center, new Vector2(0, -290), new Vector2(1000, 140));

            startButton = Button(root, "START", 46, StartMeasure, new Color(Accent.r, Accent.g, Accent.b, 0.4f));
            startButton.GetComponent<RectTransform>().At(Bottom, new Vector2(-340, 150), new Vector2(310, 150));
            applyButton = Button(root, "APPLY", 46, Apply);
            applyButton.GetComponent<RectTransform>().At(Bottom, new Vector2(0, 150), new Vector2(310, 150));
            Button(root, "BACK", 46, () => { StopMeasure(); BackRequested?.Invoke(); })
                .GetComponent<RectTransform>().At(Bottom, new Vector2(340, 150), new Vector2(310, 150));
        }

        public void Show()
        {
            Root.SetActive(true);
            hasResult = false;
            running = false;
            statusText.text = "Press START";
            resultText.text = $"Current offset: {FormatMs(GameSettings.OffsetMs)}";
            startButton.SetLabel("START");
            applyButton.interactable = false;
        }

        public void Hide()
        {
            StopMeasure();
            Root.SetActive(false);
        }

        /// <summary>하단 버튼 영역을 제외한 곳의 터치를 탭으로 인정.</summary>
        public static int TapArea(Vector2 screenPos) => screenPos.y > Screen.height * 0.2f ? 0 : -1;

        void StartMeasure()
        {
            if (clip == null) clip = DemoSong.RenderClickTrack(Bpm, TotalBeats);
            deltas.Clear();
            hasResult = false;
            applyButton.interactable = false;
            conductor.ApplyOffset = false;
            conductor.Play(clip, 0.5);
            running = true;
            startButton.SetLabel("RETRY");
            resultText.text = "";
        }

        void StopMeasure()
        {
            if (running) conductor.Stop();
            running = false;
            conductor.ApplyOffset = true;
        }

        void Apply()
        {
            if (!hasResult) return;
            GameSettings.OffsetMs = measuredMs;
            resultText.text = $"Applied!  Offset = {FormatMs(GameSettings.OffsetMs)}";
            applyButton.interactable = false;
        }

        static string FormatMs(float ms) => (ms > 0 ? "+" : "") + ms.ToString("0") + " ms";

        public void Tick(InputReader input)
        {
            if (!running)
            {
                pulse.rectTransform.localScale = Vector3.one * 0.8f;
                return;
            }

            double t = conductor.AudioTime;
            double beatPos = t / BeatSec;
            int beatIndex = (int)Math.Floor(beatPos);

            // 박자마다 원이 커졌다가 줄어든다.
            double phase = beatPos - Math.Floor(beatPos);
            float s = t < 0 ? 0.8f : Mathf.Lerp(1.15f, 0.8f, (float)Math.Min(1.0, phase * 2.5));
            pulse.rectTransform.localScale = new Vector3(s, s, 1);
            pulse.color = beatIndex >= 0 && beatIndex < CountIn ? Accent2 : Accent;

            if (beatIndex < CountIn) statusText.text = t < 0 ? "Get ready..." : $"Count-in  {beatIndex + 1}";
            else statusText.text = $"Taps  {deltas.Count} / {TapsNeeded}";

            if (input.AnyPressed)
            {
                int nearest = (int)Math.Round(beatPos);
                double delta = t - nearest * BeatSec;
                if (nearest >= CountIn && nearest < TotalBeats && Math.Abs(delta) < BeatSec * 0.4)
                    deltas.Add(delta);
            }

            if (deltas.Count >= TapsNeeded || t > TotalBeats * BeatSec + 0.3) Finish();
        }

        void Finish()
        {
            StopMeasure();
            if (deltas.Count < TapsNeeded / 2)
            {
                statusText.text = "Not enough taps";
                resultText.text = "Try again and tap along with the clicks.";
                return;
            }

            // 중앙값 사용: 실수로 한두 번 어긋난 탭에 흔들리지 않음
            deltas.Sort();
            double median = deltas.Count % 2 == 1
                ? deltas[deltas.Count / 2]
                : (deltas[deltas.Count / 2 - 1] + deltas[deltas.Count / 2]) / 2.0;
            double mean = 0;
            foreach (var d in deltas) mean += d;
            mean /= deltas.Count;
            double variance = 0;
            foreach (var d in deltas) variance += (d - mean) * (d - mean);
            double stdMs = Math.Sqrt(variance / deltas.Count) * 1000.0;

            measuredMs = Mathf.Round((float)(median * 1000.0));
            hasResult = true;
            applyButton.interactable = true;
            statusText.text = $"Measured: {FormatMs(measuredMs)}";
            resultText.text = $"Consistency ±{stdMs:0} ms   ·   Current {FormatMs(GameSettings.OffsetMs)}\nPress APPLY to use this value.";
        }
    }
}
