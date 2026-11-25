using System;

namespace RhythmArchive
{
    public enum Judgement
    {
        Perfect = 0,
        Great = 1,
        Good = 2,
        Miss = 3,
    }

    /// <summary>판정 범위(초). 입력 시각과 노트 시각의 차이 절대값으로 판정한다.</summary>
    public static class JudgeWindows
    {
        public const double Perfect = 0.045;
        public const double Great = 0.090;
        public const double Good = 0.135;

        /// <summary>이보다 이르게 누른 입력은 무시한다(헛손질로 미스 처리하지 않음).</summary>
        public const double EarliestAccepted = Good;

        public static Judgement Evaluate(double delta)
        {
            double a = Math.Abs(delta);
            if (a <= Perfect) return Judgement.Perfect;
            if (a <= Great) return Judgement.Great;
            if (a <= Good) return Judgement.Good;
            return Judgement.Miss;
        }
    }

    /// <summary>점수, 콤보, 판정 개수를 집계한다. 만점은 1,000,000점.</summary>
    public class ScoreKeeper
    {
        public const int MaxScore = 1_000_000;

        readonly int[] counts = new int[4];

        public int TotalJudgements { get; }
        public int Judged { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int FastCount { get; private set; }
        public int SlowCount { get; private set; }

        double weighted;

        public ScoreKeeper(int totalJudgements)
        {
            TotalJudgements = Math.Max(1, totalJudgements);
        }

        public int Count(Judgement j) => counts[(int)j];

        public int Score => (int)Math.Round(MaxScore * weighted / TotalJudgements);

        /// <summary>현재까지 판정된 노트 기준 정확도(0~100).</summary>
        public double Accuracy => Judged == 0 ? 100.0 : 100.0 * weighted / Judged;

        public bool IsFullCombo => Judged == TotalJudgements && counts[(int)Judgement.Miss] == 0;
        public bool IsAllPerfect => Judged == TotalJudgements && counts[(int)Judgement.Perfect] == TotalJudgements;

        /// <param name="delta">입력시각 - 노트시각(초). 양수면 늦음(SLOW). 롱노트 끝처럼 의미 없으면 NaN.</param>
        public void Register(Judgement j, double delta)
        {
            counts[(int)j]++;
            Judged++;
            weighted += Weight(j);

            if (j == Judgement.Miss)
            {
                Combo = 0;
            }
            else
            {
                Combo++;
                if (Combo > MaxCombo) MaxCombo = Combo;
                if (j != Judgement.Perfect && !double.IsNaN(delta))
                {
                    if (delta < 0) FastCount++;
                    else SlowCount++;
                }
            }
        }

        public static double Weight(Judgement j) => j switch
        {
            Judgement.Perfect => 1.0,
            Judgement.Great => 0.7,
            Judgement.Good => 0.3,
            _ => 0.0,
        };

        public static string RankOf(int score)
        {
            if (score >= 990_000) return "SSS";
            if (score >= 970_000) return "SS";
            if (score >= 950_000) return "S";
            if (score >= 900_000) return "A";
            if (score >= 800_000) return "B";
            if (score >= 700_000) return "C";
            return "D";
        }
    }
}
