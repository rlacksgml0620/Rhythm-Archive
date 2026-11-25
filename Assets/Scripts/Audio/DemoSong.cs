using System.Collections.Generic;
using UnityEngine;

namespace RhythmArchive
{
    /// <summary>
    /// 내장 데모곡 "Neon Pulse" (140 BPM, 32마디, 약 55초).
    /// 음악과 채보를 같은 악보 데이터에서 만들어서 노트가 항상 소리와 맞는다.
    /// 코드로 합성하므로 저작권 문제가 없다.
    /// </summary>
    public static class DemoSong
    {
        public const string Id = "demo_neon_pulse";
        public const string Title = "Neon Pulse";
        public const string Artist = "Rhythm Archive";
        public const float Bpm = 140f;
        public const int Bars = 32;

        public static readonly double Beat = 60.0 / Bpm;

        enum Section { Intro, Verse, Chorus, Break, Outro }

        static Section SectionOf(int bar) =>
            bar < 4 ? Section.Intro :
            bar < 12 ? Section.Verse :
            bar < 20 ? Section.Chorus :
            bar < 24 ? Section.Break :
            bar < 30 ? Section.Chorus :
            Section.Outro;

        // 코드 진행: Am - F - C - G
        static readonly int[] BassRoot = { 45, 41, 48, 43 };
        static readonly int[][] PadChord =
        {
            new[] { 57, 60, 64 }, new[] { 57, 60, 65 }, new[] { 55, 60, 64 }, new[] { 55, 59, 62 },
        };
        // 리드 음 4개 = 레인 4개 (낮은 음 → 왼쪽, 높은 음 → 오른쪽)
        static readonly int[][] LeadTones =
        {
            new[] { 69, 72, 76, 81 }, new[] { 65, 69, 72, 77 }, new[] { 67, 72, 76, 79 }, new[] { 67, 71, 74, 79 },
        };
        static readonly int[] LeadPatA = { 0, 1, 2, 3, 2, 1, 2, -1 };
        static readonly int[] LeadPatB = { 3, 2, 1, 0, 1, 2, 3, -1 };
        static readonly int[] IntroHoldLane = { 0, 3, 1, 2 };
        static readonly int[] VersePat = { 0, 2, 1, 3, 3, 1, 2, 0 };
        static readonly int[] Stairs = { 0, 1, 2, 3, 2, 1 };

        static double T(int bar, double beat) => (bar * 4 + beat) * Beat;
        static int[] LeadPat(int bar) => bar % 2 == 0 ? LeadPatA : LeadPatB;
        static int[] BreakTones(int bar) => bar % 2 == 0 ? new[] { 0, 2 } : new[] { 3, 1 };

        public static SongEntry CreateEntry()
        {
            var entry = new SongEntry
            {
                Id = Id,
                Title = Title,
                Artist = Artist,
                Bpm = Bpm,
                LoadClip = Render,
            };
            entry.Charts.Add(BuildChart("EASY", 3));
            entry.Charts.Add(BuildChart("NORMAL", 6));
            entry.Charts.Add(BuildChart("HARD", 9));
            return entry;
        }

        // ------------------------------------------------------------------ 음악

        public static AudioClip Render()
        {
            var s = new Synth(Bars * 4 * Beat + 2.5);
            double b = Beat;

            for (int bar = 0; bar < Bars; bar++)
            {
                int ch = bar % 4;
                var sec = SectionOf(bar);
                bool last = bar == Bars - 1;

                // 패드
                if (!last) s.Pad(T(bar, 0), 4 * b, PadChord[ch], sec == Section.Chorus ? 0.035 : 0.05);

                // 하이햇
                if (bar >= 2 && sec != Section.Break && !last)
                    for (int st = 0; st < 8; st++) s.Hat(T(bar, st * 0.5), st % 2 == 0 ? 0.10 : 0.06);
                if (sec == Section.Break)
                    for (int beat = 0; beat < 4; beat++) s.Hat(T(bar, beat + 0.5), 0.06, 40);

                switch (sec)
                {
                    case Section.Intro:
                        s.Lead(T(bar, 0), 2 * b, LeadTones[ch][IntroHoldLane[bar]], 0.16, soft: true);
                        break;

                    case Section.Verse:
                    case Section.Chorus:
                        for (int beat = 0; beat < 4; beat++) s.Kick(T(bar, beat), 0.9);
                        s.Snare(T(bar, 1), 0.4);
                        s.Snare(T(bar, 3), 0.4);
                        if (bar == 19 || bar == 29) { s.Snare(T(bar, 3.5), 0.3); s.Snare(T(bar, 3.75), 0.3); }
                        for (int st = 0; st < 8; st++)
                            s.Bass(T(bar, st * 0.5), 0.45 * b, BassRoot[ch] + (st % 2 == 1 ? 12 : 0), 0.28);
                        if (sec == Section.Chorus)
                        {
                            var pat = LeadPat(bar);
                            for (int st = 0; st < 8; st++)
                                if (pat[st] >= 0) s.Lead(T(bar, st * 0.5), 0.45 * b, LeadTones[ch][pat[st]], 0.13);
                        }
                        break;

                    case Section.Break:
                    {
                        s.Bass(T(bar, 0), 3.8 * b, BassRoot[ch], 0.22);
                        var tones = BreakTones(bar);
                        for (int k = 0; k < 2; k++)
                            s.Lead(T(bar, k * 2), 1.8 * b, LeadTones[ch][tones[k]], 0.15, soft: true);
                        break;
                    }

                    case Section.Outro:
                        if (!last)
                        {
                            s.Kick(T(bar, 0), 0.9);
                            s.Kick(T(bar, 2), 0.9);
                            s.Bass(T(bar, 0), 3.8 * b, BassRoot[ch], 0.22);
                            s.Lead(T(bar, 0), 3 * b, LeadTones[ch][0], 0.12, soft: true);
                            s.Lead(T(bar, 0), 3 * b, LeadTones[ch][3], 0.12, soft: true);
                        }
                        else
                        {
                            // 마지막 한 방: A 마이너 코드
                            s.Kick(T(bar, 0), 1.0);
                            s.Snare(T(bar, 0), 0.4);
                            s.Bass(T(bar, 0), 2 * b, 45, 0.25);
                            s.Pad(T(bar, 0), 2 * b, PadChord[0], 0.06);
                            s.Lead(T(bar, 0), 2 * b, 72, 0.14, soft: true);
                            s.Lead(T(bar, 0), 2 * b, 76, 0.14, soft: true);
                        }
                        break;
                }
            }
            return s.ToClip(Title);
        }

        // ------------------------------------------------------------------ 채보

        public static ChartData BuildChart(string difficulty, int level)
        {
            var chart = new ChartData { difficulty = difficulty, level = level };
            var n = chart.notes;
            bool easy = difficulty == "EASY";
            bool hard = difficulty == "HARD";

            void Tap(int bar, double beat, int lane) => n.Add(new NoteData(T(bar, beat), lane));
            void Hold(int bar, double beat, int lane, double beats) =>
                n.Add(new NoteData(T(bar, beat), lane, beats * Beat));

            for (int bar = 0; bar < Bars; bar++)
            {
                switch (SectionOf(bar))
                {
                    case Section.Intro:
                    {
                        int hl = easy ? (bar % 2 == 0 ? 1 : 2) : IntroHoldLane[bar];
                        Hold(bar, 0, hl, 2);
                        if (bar >= 2 && !easy)
                        {
                            if (hard)
                            {
                                int k = 0;
                                for (int st = 1; st < 8; st++)
                                {
                                    int lane = (hl + 1 + k % 3) % 4;
                                    Tap(bar, st * 0.5, lane);
                                    k++;
                                }
                            }
                            else
                            {
                                Tap(bar, 2, (hl + 2) % 4);
                                Tap(bar, 3, (hl + 1) % 4);
                            }
                        }
                        break;
                    }

                    case Section.Verse:
                        if (easy)
                        {
                            Tap(bar, 0, bar % 2 == 0 ? 1 : 2);
                            Tap(bar, 2, bar % 2 == 0 ? 2 : 1);
                        }
                        else if (!hard)
                        {
                            for (int beat = 0; beat < 4; beat++) Tap(bar, beat, VersePat[(bar * 4 + beat) % 8]);
                        }
                        else
                        {
                            for (int st = 0; st < 8; st++)
                            {
                                int lane = Stairs[(bar * 8 + st) % Stairs.Length];
                                Tap(bar, st * 0.5, lane);
                                if (st == 0 && bar % 2 == 0) Tap(bar, 0, 3 - lane);
                            }
                        }
                        break;

                    case Section.Chorus:
                    {
                        var pat = LeadPat(bar);
                        for (int st = 0; st < 8; st++)
                        {
                            int tone = pat[st];
                            if (tone < 0) continue;
                            if (easy && st % 4 != 0) continue;
                            if (!easy && !hard && st % 2 != 0) continue;
                            Tap(bar, st * 0.5, tone);
                            if (hard && st % 4 == 0) Tap(bar, st * 0.5, (tone + 2) % 4);
                        }
                        break;
                    }

                    case Section.Break:
                    {
                        var tones = BreakTones(bar);
                        for (int k = 0; k < 2; k++)
                        {
                            Hold(bar, k * 2, tones[k], 1.5);
                            if (hard) Tap(bar, k * 2 + 1, (tones[k] + 2) % 4);
                        }
                        break;
                    }

                    case Section.Outro:
                        if (bar < Bars - 1)
                        {
                            if (easy) Hold(bar, 0, 1, 3);
                            else { Hold(bar, 0, 0, 3); Hold(bar, 0, 3, 3); }
                        }
                        else
                        {
                            Tap(bar, 0, 1);
                            if (!easy) Tap(bar, 0, 2);
                        }
                        break;
                }
            }

            chart.Normalize();
            return chart;
        }

        /// <summary>싱크 보정 화면용 메트로놈(4박 카운트 + 측정 박자).</summary>
        public static AudioClip RenderClickTrack(float bpm, int beats)
        {
            double beat = 60.0 / bpm;
            var s = new Synth(beats * beat + 0.5);
            for (int i = 0; i < beats; i++)
                s.Click(i * beat, i % 4 == 0 ? 0.9 : 0.6, i % 4 == 0 ? 1760 : 1320);
            return s.ToClip("Metronome");
        }
    }
}
