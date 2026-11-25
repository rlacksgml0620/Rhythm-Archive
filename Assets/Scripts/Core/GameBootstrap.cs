using UnityEngine;

namespace RhythmArchive
{
    /// <summary>
    /// 씬 설정 없이 Play만 누르면 게임이 시작되도록 자동으로 GameManager를 만든다.
    /// </summary>
    static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ConfigureAudio()
        {
            // 오디오 버퍼를 작게 해서 출력 지연을 줄인다(리듬게임에 중요).
            var config = AudioSettings.GetConfiguration();
            if (config.dspBufferSize == 0 || config.dspBufferSize > 256)
            {
                config.dspBufferSize = 256;
                AudioSettings.Reset(config);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateGame()
        {
            if (GameManager.Instance == null)
                new GameObject("GameManager").AddComponent<GameManager>();
        }
    }
}
