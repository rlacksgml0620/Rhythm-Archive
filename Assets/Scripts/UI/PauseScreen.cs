using System;
using UnityEngine;
using static RhythmArchive.UIFactory;

namespace RhythmArchive
{
    public class PauseScreen
    {
        public readonly GameObject Root;
        public event Action ResumeRequested, RetryRequested, QuitRequested;

        public PauseScreen(Transform canvas)
        {
            var bg = Panel("PauseScreen", canvas, new Color(0, 0, 0, 0.78f));
            Root = bg.gameObject;
            var root = bg.transform;

            Label(root, "PAUSED", 100, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.At(Center, new Vector2(0, 300), new Vector2(800, 140));
            Button(root, "RESUME", 56, () => ResumeRequested?.Invoke(), new Color(Accent.r, Accent.g, Accent.b, 0.4f))
                .GetComponent<RectTransform>().At(Center, new Vector2(0, 80), new Vector2(620, 140));
            Button(root, "RETRY", 56, () => RetryRequested?.Invoke())
                .GetComponent<RectTransform>().At(Center, new Vector2(0, -90), new Vector2(620, 140));
            Button(root, "QUIT", 56, () => QuitRequested?.Invoke())
                .GetComponent<RectTransform>().At(Center, new Vector2(0, -260), new Vector2(620, 140));
        }

        public void Show() => Root.SetActive(true);
        public void Hide() => Root.SetActive(false);
    }
}
