using System.Collections.Generic;
using UnityEngine;

namespace RhythmArchive
{
    /// <summary>
    /// 레인 배경, 판정선, 키 빔, 타격 이펙트 등 플레이 화면의 "무대"를 그린다.
    /// 화면 비율이 바뀌어도 레인 폭을 다시 계산한다.
    /// </summary>
    public class Playfield : MonoBehaviour
    {
        public const int LaneCount = InputReader.LaneCount;
        public const float JudgeY = -3.1f;
        public const float NoteHeight = 0.32f;

        public static readonly Color[] LaneColors =
        {
            new Color(0.35f, 0.85f, 1f),
            new Color(1f, 0.45f, 0.75f),
            new Color(1f, 0.45f, 0.75f),
            new Color(0.35f, 0.85f, 1f),
        };

        public Camera Cam { get; private set; }
        public float LaneWidth { get; private set; }
        public float LeftX { get; private set; }
        public float TopY { get; private set; }
        public float BottomY { get; private set; }

        SpriteRenderer background;
        readonly SpriteRenderer[] laneBgs = new SpriteRenderer[LaneCount];
        readonly SpriteRenderer[] separators = new SpriteRenderer[LaneCount + 1];
        readonly SpriteRenderer[] beams = new SpriteRenderer[LaneCount];
        readonly float[] beamAlpha = new float[LaneCount];
        readonly bool[] held = new bool[LaneCount];
        SpriteRenderer judgeLine, judgeGlow;

        class Effect
        {
            public SpriteRenderer sr;
            public float age;
            public float life;
            public bool active;
        }
        readonly List<Effect> effects = new List<Effect>();

        int lastW, lastH;
        float lastAspect;

        public void Build(Camera cam)
        {
            Cam = cam;

            background = MakeSprite("Background", RuntimeSprites.Square, new Color(0.03f, 0.035f, 0.07f), -10);
            for (int i = 0; i < LaneCount; i++)
            {
                laneBgs[i] = MakeSprite("LaneBg" + i, RuntimeSprites.Square,
                    i % 2 == 0 ? new Color(0.07f, 0.08f, 0.14f) : new Color(0.06f, 0.065f, 0.12f), 0);
                beams[i] = MakeSprite("Beam" + i, RuntimeSprites.VerticalFade, Color.clear, 2);
            }
            for (int i = 0; i <= LaneCount; i++)
                separators[i] = MakeSprite("Separator" + i, RuntimeSprites.Square, new Color(1f, 1f, 1f, 0.12f), 1);

            judgeGlow = MakeSprite("JudgeGlow", RuntimeSprites.Square, new Color(0.6f, 0.9f, 1f, 0.12f), 3);
            judgeLine = MakeSprite("JudgeLine", RuntimeSprites.Square, new Color(0.85f, 0.95f, 1f, 0.95f), 3);

            Layout();
        }

        SpriteRenderer MakeSprite(string name, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        void Layout()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            lastAspect = Cam.aspect;

            float halfH = Cam.orthographicSize;
            float halfW = halfH * Cam.aspect;
            TopY = halfH;
            BottomY = -halfH;
            LaneWidth = Mathf.Min(1.15f, halfW * 2f * 0.92f / LaneCount);
            LeftX = -LaneWidth * LaneCount / 2f;
            float fieldW = LaneWidth * LaneCount;

            Place(background, 0, 0, halfW * 2f + 1f, halfH * 2f + 1f);
            for (int i = 0; i < LaneCount; i++)
            {
                Place(laneBgs[i], LaneCenterX(i), 0, LaneWidth, halfH * 2f);
                var beamSize = beams[i].sprite.bounds.size;
                beams[i].transform.position = new Vector3(LaneCenterX(i), JudgeY, 0);
                beams[i].transform.localScale = new Vector3(LaneWidth / beamSize.x, 3.2f / beamSize.y, 1);
            }
            for (int i = 0; i <= LaneCount; i++)
                Place(separators[i], LeftX + i * LaneWidth, 0, 0.02f, halfH * 2f);

            Place(judgeLine, 0, JudgeY, fieldW, 0.05f);
            Place(judgeGlow, 0, JudgeY, fieldW, 0.4f);
        }

        static void Place(SpriteRenderer sr, float x, float y, float w, float h)
        {
            sr.transform.position = new Vector3(x, y, 0);
            sr.transform.localScale = new Vector3(w, h, 1);
        }

        public float LaneCenterX(int lane) => LeftX + LaneWidth * (lane + 0.5f);

        /// <summary>
        /// 플레이 중 화면 터치 → 레인. 화면 상단(일시정지 버튼 영역)은 무시하고,
        /// 레인 바깥 좌우 여백은 가장 가까운 레인으로 처리한다.
        /// </summary>
        public int LaneFromScreen(Vector2 screenPos)
        {
            if (screenPos.y > Screen.height * 0.88f) return -1;
            var world = Cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
            int lane = Mathf.FloorToInt((world.x - LeftX) / LaneWidth);
            return Mathf.Clamp(lane, 0, LaneCount - 1);
        }

        public void SetHeld(int lane, bool isHeld) => held[lane] = isHeld;

        public void Flash(int lane) => beamAlpha[lane] = 0.55f;

        public void ClearEffects()
        {
            for (int i = 0; i < LaneCount; i++) { beamAlpha[i] = 0; held[i] = false; }
            foreach (var e in effects) { e.active = false; e.sr.enabled = false; }
        }

        public void SpawnHitEffect(int lane, Judgement j)
        {
            if (j == Judgement.Miss) return;
            Effect e = null;
            foreach (var candidate in effects)
                if (!candidate.active) { e = candidate; break; }
            if (e == null)
            {
                e = new Effect { sr = MakeSprite("HitFx", RuntimeSprites.Circle, Color.white, 6) };
                effects.Add(e);
            }
            e.active = true;
            e.age = 0;
            e.life = 0.28f;
            e.sr.enabled = true;
            e.sr.transform.position = new Vector3(LaneCenterX(lane), JudgeY, 0);
            e.sr.color = JudgementColor(j);
        }

        public static Color JudgementColor(Judgement j) => j switch
        {
            Judgement.Perfect => new Color(1f, 0.86f, 0.35f),
            Judgement.Great => new Color(0.45f, 0.9f, 1f),
            Judgement.Good => new Color(0.55f, 1f, 0.55f),
            _ => new Color(1f, 0.35f, 0.35f),
        };

        void Update()
        {
            if (Cam == null) return;
            if (Screen.width != lastW || Screen.height != lastH || !Mathf.Approximately(Cam.aspect, lastAspect))
                Layout();

            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < LaneCount; i++)
            {
                float target = held[i] ? 0.35f : 0f;
                beamAlpha[i] = Mathf.Max(target, beamAlpha[i] - dt * 3f);
                var c = LaneColors[i];
                c.a = beamAlpha[i];
                beams[i].color = c;
            }

            foreach (var e in effects)
            {
                if (!e.active) continue;
                e.age += dt;
                float t = e.age / e.life;
                if (t >= 1f)
                {
                    e.active = false;
                    e.sr.enabled = false;
                    continue;
                }
                float size = LaneWidth * Mathf.Lerp(0.6f, 1.5f, 1f - (1f - t) * (1f - t));
                e.sr.transform.localScale = new Vector3(size, size, 1);
                var c = e.sr.color;
                c.a = 1f - t;
                e.sr.color = c;
            }
        }
    }
}
