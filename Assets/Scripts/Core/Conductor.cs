using System;
using UnityEngine;

namespace RhythmArchive
{
    /// <summary>
    /// 곡의 재생 시각을 관리한다. 프레임 시간(Time.time)이 아니라 오디오 하드웨어 시계
    /// (AudioSettings.dspTime)를 기준으로 해서, 프레임이 흔들려도 음악과 노트가 어긋나지 않는다.
    /// dspTime은 오디오 버퍼 단위로만 갱신되므로 실시간 시계로 보간해서 부드럽게 만든다.
    /// </summary>
    public class Conductor : MonoBehaviour
    {
        AudioSource source;

        double startDsp;          // AudioTime == 0 이 되는 dsp 시각
        double lastDsp;
        double lastDspRealtime;
        double lastReported;
        bool running;
        bool paused;
        double pausedAudioTime;

        /// <summary>false면 사용자 싱크 오프셋을 적용하지 않는다(보정 화면에서 사용).</summary>
        public bool ApplyOffset { get; set; } = true;

        public bool IsRunning => running;
        public bool IsPaused => paused;
        public AudioClip Clip => source != null ? source.clip : null;
        public double ClipLength => Clip != null ? Clip.length : 0;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        /// <summary>leadIn초 뒤에 곡 재생을 시작한다(그동안 SongTime은 음수).</summary>
        public void Play(AudioClip clip, double leadIn)
        {
            source.Stop();
            source.clip = clip;
            source.timeSamples = 0;
            double now = AudioSettings.dspTime;
            startDsp = now + Math.Max(0.1, leadIn);
            source.PlayScheduled(startDsp);
            running = true;
            paused = false;
            ResetClock(now);
        }

        public void Stop()
        {
            if (source != null) source.Stop();
            running = false;
            paused = false;
        }

        public void Pause()
        {
            if (!running || paused) return;
            pausedAudioTime = AudioTime;
            source.Stop();
            paused = true;
        }

        /// <summary>
        /// 일시정지한 지점보다 rewind초 앞에서부터 다시 진행한다.
        /// 플레이어가 다시 준비할 시간을 주기 위해 노트가 살짝 되감긴다.
        /// </summary>
        public void Resume(double rewind)
        {
            if (!running || !paused) return;
            rewind = Math.Max(0.5, rewind);
            double now = AudioSettings.dspTime;
            startDsp = now - (pausedAudioTime - rewind);

            double clipPos = Math.Max(0.0, pausedAudioTime);
            if (source.clip != null && clipPos < source.clip.length)
            {
                source.timeSamples = Mathf.Clamp((int)(clipPos * source.clip.frequency), 0, source.clip.samples - 1);
                source.PlayScheduled(startDsp + clipPos);
            }
            paused = false;
            ResetClock(now);
        }

        void ResetClock(double dspNow)
        {
            lastDsp = dspNow;
            lastDspRealtime = Time.realtimeSinceStartupAsDouble;
            lastReported = double.NegativeInfinity;
        }

        /// <summary>오디오 기준 재생 위치(초). 오프셋 미적용. 리드인 동안은 음수.</summary>
        public double AudioTime
        {
            get
            {
                if (!running) return 0;
                if (paused) return pausedAudioTime;

                double dsp = AudioSettings.dspTime;
                double real = Time.realtimeSinceStartupAsDouble;
                if (dsp != lastDsp)
                {
                    lastDsp = dsp;
                    lastDspRealtime = real;
                }
                // dspTime 갱신 사이를 실시간으로 보간(최대 0.1초까지만).
                double t = lastDsp + Math.Min(real - lastDspRealtime, 0.1) - startDsp;
                // 시간이 거꾸로 가지 않도록 보장.
                if (t < lastReported) t = lastReported;
                lastReported = t;
                return t;
            }
        }

        /// <summary>판정/노트 표시에 쓰는 곡 시각(초). 사용자 오프셋 적용.</summary>
        public double SongTime => AudioTime - (ApplyOffset ? GameSettings.OffsetSec : 0.0);
    }
}
