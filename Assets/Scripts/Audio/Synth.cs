using System;
using UnityEngine;

namespace RhythmArchive
{
    /// <summary>
    /// 아주 단순한 소프트웨어 신디사이저. 오디오 파일 없이도 데모곡/메트로놈을
    /// 코드로 만들어 내기 위해 사용한다(저작권 걱정 없는 내장 음원).
    /// </summary>
    public class Synth
    {
        public const int SampleRate = 44100;

        readonly float[] buf;
        readonly System.Random rng = new System.Random(1234);

        public Synth(double seconds)
        {
            buf = new float[Math.Max(1, (int)(seconds * SampleRate))];
        }

        public int Length => buf.Length;

        static double Freq(int midi) => 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);
        static int Index(double t) => (int)Math.Round(t * SampleRate);

        void Add(int idx, double v)
        {
            if (idx >= 0 && idx < buf.Length) buf[idx] += (float)v;
        }

        double Noise() => rng.NextDouble() * 2.0 - 1.0;

        public void Kick(double t, double vol)
        {
            int s0 = Index(t), n = (int)(0.35 * SampleRate);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                double tt = (double)i / SampleRate;
                double f = 48 + 110 * Math.Exp(-tt * 28);
                phase += 2 * Math.PI * f / SampleRate;
                double env = Math.Exp(-tt * 8);
                double click = i < 60 ? (1 - i / 60.0) * 0.4 : 0;
                Add(s0 + i, (Math.Sin(phase) * env + click) * vol);
            }
        }

        public void Snare(double t, double vol)
        {
            int s0 = Index(t), n = (int)(0.22 * SampleRate);
            double lp = 0;
            for (int i = 0; i < n; i++)
            {
                double tt = (double)i / SampleRate;
                lp += 0.5 * (Noise() - lp);
                double noise = lp * Math.Exp(-tt * 18);
                double tone = Math.Sin(2 * Math.PI * 185 * tt) * Math.Exp(-tt * 30);
                Add(s0 + i, (noise * 0.8 + tone * 0.5) * vol);
            }
        }

        public void Hat(double t, double vol, double decay = 70)
        {
            int s0 = Index(t), n = (int)(0.08 * SampleRate);
            double prev = 0;
            for (int i = 0; i < n; i++)
            {
                double tt = (double)i / SampleRate;
                double x = Noise();
                double hp = x - prev; // 간단한 고역 통과
                prev = x;
                Add(s0 + i, hp * Math.Exp(-tt * decay) * vol);
            }
        }

        public void Click(double t, double vol, double freq)
        {
            int s0 = Index(t), n = (int)(0.05 * SampleRate);
            for (int i = 0; i < n; i++)
            {
                double tt = (double)i / SampleRate;
                Add(s0 + i, Math.Sin(2 * Math.PI * freq * tt) * Math.Exp(-tt * 90) * vol);
            }
        }

        public void Bass(double t, double len, int midi, double vol)
        {
            double f = Freq(midi);
            double release = 0.03;
            int s0 = Index(t), n = (int)((len + release) * SampleRate);
            double phase = 0, y = 0;
            for (int i = 0; i < n; i++)
            {
                double tt = (double)i / SampleRate;
                phase += f / SampleRate;
                phase -= Math.Floor(phase);
                double saw = 2 * phase - 1;
                y += 0.09 * (saw - y);
                double env = Math.Min(1, tt / 0.003) * (0.55 + 0.45 * Math.Exp(-tt * 6));
                if (tt > len) env *= Math.Max(0, 1 - (tt - len) / release);
                Add(s0 + i, y * env * vol);
            }
        }

        /// <param name="soft">true면 삼각파(부드러운 벨), false면 사각파(리드).</param>
        public void Lead(double t, double len, int midi, double vol, bool soft = false)
        {
            double f = Freq(midi);
            double release = soft ? 0.25 : 0.06;
            int s0 = Index(t), n = (int)((len + release) * SampleRate);
            double p1 = 0, p2 = 0, y = 0;
            for (int i = 0; i < n; i++)
            {
                double tt = (double)i / SampleRate;
                p1 += f / SampleRate; p1 -= Math.Floor(p1);
                p2 += f * 1.004 / SampleRate; p2 -= Math.Floor(p2);
                double x = soft
                    ? (4 * Math.Abs(p1 - 0.5) - 1) * 0.5 + (4 * Math.Abs(p2 - 0.5) - 1) * 0.5
                    : (p1 < 0.5 ? 0.5 : -0.5) + (p2 < 0.5 ? 0.5 : -0.5);
                y += (soft ? 0.5 : 0.22) * (x - y);
                double env = Math.Min(1, tt / 0.005) * Math.Exp(-tt * (soft ? 1.5 : 3.5));
                if (tt > len) env *= Math.Max(0, 1 - (tt - len) / release);
                Add(s0 + i, y * env * vol);
            }
        }

        public void Pad(double t, double len, int[] midis, double vol)
        {
            double attack = Math.Min(0.25, len / 3), release = 0.35;
            int s0 = Index(t), n = (int)((len + release) * SampleRate);
            foreach (int m in midis)
            {
                double f = Freq(m);
                for (int i = 0; i < n; i++)
                {
                    double tt = (double)i / SampleRate;
                    double env = Math.Min(1, tt / attack);
                    if (tt > len) env *= Math.Max(0, 1 - (tt - len) / release);
                    double x = Math.Sin(2 * Math.PI * f * tt) + 0.6 * Math.Sin(2 * Math.PI * f * 1.003 * tt)
                             + 0.25 * Math.Sin(2 * Math.PI * f * 2 * tt);
                    Add(s0 + i, x * env * vol);
                }
            }
        }

        /// <summary>소프트 클리핑 후 정규화해서 AudioClip으로 만든다.</summary>
        public AudioClip ToClip(string name)
        {
            float peak = 0.0001f;
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (float)Math.Tanh(buf[i]);
                peak = Math.Max(peak, Math.Abs(buf[i]));
            }
            float gain = 0.9f / peak;
            int fade = Math.Min(buf.Length, SampleRate / 10);
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] *= gain;
                int fromEnd = buf.Length - 1 - i;
                if (fromEnd < fade) buf[i] *= (float)fromEnd / fade;
            }
            var clip = AudioClip.Create(name, buf.Length, 1, SampleRate, false);
            clip.SetData(buf, 0);
            return clip;
        }
    }
}
