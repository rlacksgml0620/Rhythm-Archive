using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmArchive
{
    /// <summary>노트 하나. duration이 0보다 크면 롱노트(홀드).</summary>
    [Serializable]
    public class NoteData
    {
        public double time;      // 초 (곡 파일에서 timeUnit이 "beat"이면 박자 단위로 적고 로드 시 변환)
        public int lane;         // 0 ~ 3
        public double duration;  // 0 = 단노트

        public NoteData() { }

        public NoteData(double time, int lane, double duration = 0)
        {
            this.time = time;
            this.lane = lane;
            this.duration = duration;
        }

        public bool IsHold => duration > 0.05;
        public double EndTime => time + (IsHold ? duration : 0);
    }

    /// <summary>한 난이도의 채보.</summary>
    [Serializable]
    public class ChartData
    {
        public string difficulty = "NORMAL";
        public int level = 1;
        public List<NoteData> notes = new List<NoteData>();

        /// <summary>판정 횟수(롱노트는 시작+끝 2회).</summary>
        public int JudgementCount
        {
            get
            {
                int n = 0;
                foreach (var note in notes) n += note.IsHold ? 2 : 1;
                return n;
            }
        }

        public double LastTime
        {
            get
            {
                double t = 0;
                foreach (var note in notes) t = Math.Max(t, note.EndTime);
                return t;
            }
        }

        /// <summary>시간순 정렬, 레인 범위 보정, 같은 레인에서 겹치는 노트 제거.</summary>
        public void Normalize(int laneCount = InputReader.LaneCount, double minGap = 0.06)
        {
            notes.Sort((a, b) => a.time != b.time ? a.time.CompareTo(b.time) : a.lane.CompareTo(b.lane));
            var laneFree = new double[laneCount];
            for (int i = 0; i < laneCount; i++) laneFree[i] = double.NegativeInfinity;

            var result = new List<NoteData>(notes.Count);
            foreach (var n in notes)
            {
                if (n.lane < 0 || n.lane >= laneCount) n.lane = Mathf.Clamp(n.lane, 0, laneCount - 1);
                if (n.duration < 0) n.duration = 0;
                if (n.time < laneFree[n.lane]) continue; // 같은 레인 노트/롱노트와 겹침
                result.Add(n);
                laneFree[n.lane] = n.EndTime + minGap;
            }
            notes = result;
        }
    }

    /// <summary>
    /// Resources/Songs 폴더에 넣는 곡 JSON 형식.
    /// 자세한 설명은 Docs/CHART_FORMAT.md 참고.
    /// </summary>
    [Serializable]
    public class SongFile
    {
        public string id;
        public string title = "Untitled";
        public string artist = "Unknown";
        public string audio;            // Resources 기준 경로(확장자 제외). 예: "Songs/my_song"
        public float bpm = 120f;
        public float offset;            // 첫 박(beat 0)의 시각(초)
        public string timeUnit = "beat"; // "beat" 또는 "second"
        public List<ChartData> charts = new List<ChartData>();
    }

    /// <summary>게임에서 사용하는 곡 정보(로드된 상태).</summary>
    public class SongEntry
    {
        public string Id;
        public string Title;
        public string Artist;
        public float Bpm;
        public List<ChartData> Charts = new List<ChartData>();
        public Func<AudioClip> LoadClip;

        AudioClip cached;

        public AudioClip GetClip()
        {
            if (cached == null && LoadClip != null) cached = LoadClip();
            return cached;
        }
    }
}
