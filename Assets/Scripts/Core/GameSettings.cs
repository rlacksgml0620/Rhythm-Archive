using UnityEngine;

namespace RhythmArchive
{
    /// <summary>
    /// 플레이어 설정(싱크 오프셋, 노트 속도)과 최고 기록을 PlayerPrefs에 저장한다.
    /// </summary>
    public static class GameSettings
    {
        const string KeyOffset = "settings.offsetMs";
        const string KeySpeed = "settings.noteSpeed";

        public const float MinOffsetMs = -300f;
        public const float MaxOffsetMs = 300f;
        public const float MinSpeed = 1f;
        public const float MaxSpeed = 10f;

        /// <summary>
        /// 오디오/입력 싱크 보정값(ms). 양수면 "소리가 늦게 들리는 기기"라는 뜻으로,
        /// 판정과 노트 위치를 그만큼 늦춘다.
        /// </summary>
        public static float OffsetMs
        {
            get => PlayerPrefs.GetFloat(KeyOffset, 0f);
            set
            {
                PlayerPrefs.SetFloat(KeyOffset, Mathf.Round(Mathf.Clamp(value, MinOffsetMs, MaxOffsetMs)));
                PlayerPrefs.Save();
            }
        }

        public static double OffsetSec => OffsetMs / 1000.0;

        /// <summary>노트 낙하 속도 (1.0 ~ 10.0, 0.5 단위).</summary>
        public static float NoteSpeed
        {
            get => PlayerPrefs.GetFloat(KeySpeed, 5f);
            set
            {
                float v = Mathf.Round(Mathf.Clamp(value, MinSpeed, MaxSpeed) * 2f) / 2f;
                PlayerPrefs.SetFloat(KeySpeed, v);
                PlayerPrefs.Save();
            }
        }

        static string BestKey(string songId, string difficulty) => $"best.{songId}.{difficulty}";

        public static int GetBestScore(string songId, string difficulty) =>
            PlayerPrefs.GetInt(BestKey(songId, difficulty), 0);

        /// <returns>새 최고 기록이면 true</returns>
        public static bool TrySetBestScore(string songId, string difficulty, int score)
        {
            if (score <= GetBestScore(songId, difficulty)) return false;
            PlayerPrefs.SetInt(BestKey(songId, difficulty), score);
            PlayerPrefs.Save();
            return true;
        }
    }
}
