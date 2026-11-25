using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmArchive
{
    /// <summary>
    /// 플레이 중 노트 생성/이동, 입력 판정, 롱노트 처리, 곡 종료 판정을 담당한다.
    /// GameManager가 매 프레임 Tick()을 호출한다.
    /// </summary>
    public class GameplayController : MonoBehaviour
    {
        enum NoteState { Pending, Holding, Hit, Missed }

        class NoteRuntime
        {
            public NoteData data;
            public NoteState state;
            public NoteView view;
        }

        /// <summary>(레인, 판정, 오차(초) - 롱노트 끝은 NaN)</summary>
        public event Action<int, Judgement, double> Judged;
        public event Action Finished;

        public ScoreKeeper Score { get; private set; }
        public bool IsPlaying { get; private set; }
        public double SongTime { get; private set; }
        public double EndTime { get; private set; }

        Conductor conductor;
        Playfield field;

        readonly List<NoteRuntime> notes = new List<NoteRuntime>();
        readonly Queue<NoteRuntime>[] laneQueues = new Queue<NoteRuntime>[InputReader.LaneCount];
        readonly NoteRuntime[] holding = new NoteRuntime[InputReader.LaneCount];
        readonly List<NoteRuntime> visible = new List<NoteRuntime>();
        readonly Stack<NoteView> pool = new Stack<NoteView>();
        int spawnIndex;
        double lastNoteTime;
        double holdGraceUntil = double.NegativeInfinity;
        Transform noteRoot;

        /// <summary>판정선까지 남은 시간 1초당 이동 거리(유닛).</summary>
        static float UnitsPerSecond => GameSettings.NoteSpeed * 1.8f;

        public void Init(Conductor conductor, Playfield field)
        {
            this.conductor = conductor;
            this.field = field;
            noteRoot = new GameObject("Notes").transform;
            noteRoot.SetParent(transform, false);
            for (int i = 0; i < laneQueues.Length; i++) laneQueues[i] = new Queue<NoteRuntime>();
        }

        public void StartSong(SongEntry song, ChartData chart, double leadIn)
        {
            Clear();
            foreach (var n in chart.notes)
            {
                var rt = new NoteRuntime { data = n, state = NoteState.Pending };
                notes.Add(rt);
                laneQueues[n.lane].Enqueue(rt);
            }
            Score = new ScoreKeeper(chart.JudgementCount);
            lastNoteTime = chart.LastTime;
            EndTime = Math.Max(lastNoteTime, 0);

            var clip = song.GetClip();
            if (clip != null) EndTime = Math.Max(EndTime, clip.length);

            conductor.ApplyOffset = true;
            conductor.Play(clip, leadIn);
            IsPlaying = true;
        }

        public void Stop()
        {
            IsPlaying = false;
            conductor.Stop();
            Clear();
        }

        void Clear()
        {
            foreach (var rt in visible) Release(rt);
            visible.Clear();
            notes.Clear();
            foreach (var q in laneQueues) q.Clear();
            Array.Clear(holding, 0, holding.Length);
            spawnIndex = 0;
            holdGraceUntil = double.NegativeInfinity;
            field.ClearEffects();
        }

        /// <summary>
        /// 일시정지 후 재개 직후에는 되감긴 구간 동안 롱노트를 떼도 끊지 않는다
        /// (다시 손가락을 올릴 시간을 준다).
        /// </summary>
        public void NotifyResumed() => holdGraceUntil = SongTime;

        public double Progress => EndTime <= 0 ? 0 : Math.Max(0, Math.Min(1, SongTime / EndTime));

        public void Tick(InputReader input)
        {
            if (!IsPlaying || conductor.IsPaused) return;

            double t = conductor.SongTime;
            SongTime = t;

            // 1) 놓친 노트 처리
            for (int lane = 0; lane < laneQueues.Length; lane++)
            {
                var q = laneQueues[lane];
                while (q.Count > 0 && q.Peek().data.time < t - JudgeWindows.Good)
                {
                    var rt = q.Dequeue();
                    rt.state = NoteState.Missed;
                    Register(lane, Judgement.Miss, double.NaN);
                    if (rt.data.IsHold) Register(lane, Judgement.Miss, double.NaN);
                }
            }

            // 2) 새로 누른 입력 판정
            for (int lane = 0; lane < InputReader.LaneCount; lane++)
            {
                field.SetHeld(lane, input.Held[lane]);
                if (!input.Pressed[lane]) continue;
                field.Flash(lane);

                var q = laneQueues[lane];
                if (q.Count == 0) continue;
                var rt = q.Peek();
                double delta = t - rt.data.time;
                if (delta < -JudgeWindows.EarliestAccepted) continue; // 너무 이르면 무시

                q.Dequeue();
                var j = JudgeWindows.Evaluate(delta);
                Register(lane, j, delta);
                field.SpawnHitEffect(lane, j);

                if (rt.data.IsHold)
                {
                    rt.state = NoteState.Holding;
                    holding[lane] = rt;
                }
                else
                {
                    rt.state = NoteState.Hit;
                }
            }

            // 3) 롱노트 유지 판정
            for (int lane = 0; lane < holding.Length; lane++)
            {
                var rt = holding[lane];
                if (rt == null) continue;
                double end = rt.data.EndTime;
                if (t >= end)
                {
                    rt.state = NoteState.Hit;
                    holding[lane] = null;
                    Register(lane, Judgement.Perfect, double.NaN);
                    field.SpawnHitEffect(lane, Judgement.Perfect);
                }
                else if (!input.Held[lane] && t >= holdGraceUntil)
                {
                    holding[lane] = null;
                    if (end - t <= JudgeWindows.Good)
                    {
                        rt.state = NoteState.Hit;
                        Register(lane, Judgement.Perfect, double.NaN);
                        field.SpawnHitEffect(lane, Judgement.Perfect);
                    }
                    else
                    {
                        rt.state = NoteState.Missed; // 너무 일찍 뗌
                        Register(lane, Judgement.Miss, double.NaN);
                    }
                }
                else
                {
                    field.SetHeld(lane, true);
                }
            }

            UpdateVisuals(t);

            // 4) 곡 종료
            bool allDone = Score.Judged >= Score.TotalJudgements;
            bool audioDone = conductor.Clip == null || conductor.AudioTime >= conductor.ClipLength - 0.05;
            if ((allDone && audioDone && t > lastNoteTime + 1.0) || t > EndTime + 3.0)
            {
                IsPlaying = false;
                conductor.Stop();
                Finished?.Invoke();
            }
        }

        void Register(int lane, Judgement j, double delta)
        {
            Score.Register(j, delta);
            Judged?.Invoke(lane, j, delta);
        }

        void UpdateVisuals(double t)
        {
            float ups = UnitsPerSecond;
            double lookahead = (field.TopY - Playfield.JudgeY + 1f) / ups;

            while (spawnIndex < notes.Count && notes[spawnIndex].data.time - t <= lookahead)
            {
                var rt = notes[spawnIndex++];
                if (rt.state == NoteState.Hit) continue;
                rt.view = pool.Count > 0 ? pool.Pop() : new NoteView(noteRoot);
                rt.view.SetActive(true);
                visible.Add(rt);
            }

            for (int i = visible.Count - 1; i >= 0; i--)
            {
                var rt = visible[i];
                float headY = Playfield.JudgeY + (float)(rt.data.time - t) * ups;
                float tailY = Playfield.JudgeY + (float)(rt.data.EndTime - t) * ups;

                bool remove = rt.state == NoteState.Hit
                           || (rt.state == NoteState.Missed && tailY < field.BottomY - 1f);
                if (remove)
                {
                    Release(rt);
                    visible.RemoveAt(i);
                    continue;
                }

                if (rt.state == NoteState.Holding) headY = Playfield.JudgeY;
                rt.view.Layout(field.LaneCenterX(rt.data.lane), field.LaneWidth, headY, tailY,
                    rt.data.IsHold, Playfield.LaneColors[rt.data.lane], rt.state == NoteState.Missed);
            }
        }

        void Release(NoteRuntime rt)
        {
            if (rt.view == null) return;
            rt.view.SetActive(false);
            pool.Push(rt.view);
            rt.view = null;
        }
    }
}
