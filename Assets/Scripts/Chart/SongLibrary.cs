using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmArchive
{
    /// <summary>
    /// 플레이 가능한 곡 목록. 내장 데모곡 + Resources/Songs 폴더의 JSON 곡 파일을 불러온다.
    /// </summary>
    public static class SongLibrary
    {
        static List<SongEntry> songs;

        public static IReadOnlyList<SongEntry> Songs
        {
            get
            {
                if (songs == null) Load();
                return songs;
            }
        }

        static void Load()
        {
            songs = new List<SongEntry> { DemoSong.CreateEntry() };

            foreach (var asset in Resources.LoadAll<TextAsset>("Songs"))
            {
                try
                {
                    var entry = FromJson(asset.text, asset.name);
                    if (entry != null) songs.Add(entry);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SongLibrary] '{asset.name}' 곡 파일을 읽지 못했습니다: {e.Message}");
                }
            }
        }

        public static SongEntry FromJson(string json, string fallbackId)
        {
            var file = JsonUtility.FromJson<SongFile>(json);
            if (file == null || file.charts == null || file.charts.Count == 0)
            {
                Debug.LogWarning($"[SongLibrary] '{fallbackId}'에 채보(charts)가 없습니다.");
                return null;
            }
            if (string.IsNullOrEmpty(file.audio))
            {
                Debug.LogWarning($"[SongLibrary] '{fallbackId}'에 audio 경로가 없습니다.");
                return null;
            }

            bool beats = !string.Equals(file.timeUnit, "second", StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(file.timeUnit, "seconds", StringComparison.OrdinalIgnoreCase);
            double secPerBeat = 60.0 / Math.Max(1f, file.bpm);

            var entry = new SongEntry
            {
                Id = string.IsNullOrEmpty(file.id) ? fallbackId : file.id,
                Title = file.title,
                Artist = file.artist,
                Bpm = file.bpm,
            };

            foreach (var chart in file.charts)
            {
                if (chart?.notes == null) continue;
                if (beats)
                {
                    foreach (var n in chart.notes)
                    {
                        n.time = file.offset + n.time * secPerBeat;
                        n.duration *= secPerBeat;
                    }
                }
                else
                {
                    foreach (var n in chart.notes) n.time += file.offset;
                }
                chart.difficulty = string.IsNullOrEmpty(chart.difficulty) ? "NORMAL" : chart.difficulty.ToUpperInvariant();
                chart.Normalize();
                entry.Charts.Add(chart);
            }

            string audioPath = file.audio;
            entry.LoadClip = () =>
            {
                var clip = Resources.Load<AudioClip>(audioPath);
                if (clip == null) Debug.LogError($"[SongLibrary] 오디오를 찾을 수 없습니다: Resources/{audioPath}");
                else if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
                return clip;
            };
            return entry.Charts.Count > 0 ? entry : null;
        }
    }
}
