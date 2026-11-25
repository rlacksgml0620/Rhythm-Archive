using UnityEngine;
using UnityEngine.EventSystems;

namespace RhythmArchive
{
    /// <summary>
    /// 게임 전체 흐름(메뉴 → 플레이 → 결과)을 관리한다.
    /// 씬에 아무것도 배치하지 않아도 GameBootstrap이 자동으로 생성한다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        enum State { Menu, Playing, Paused, Result, Calibration }

        const double StartLeadIn = 3.0;
        const double ResumeRewind = 2.0;

        State state;
        Camera cam;
        Conductor conductor;
        Playfield field;
        GameplayController gameplay;
        InputReader input;

        MenuScreen menu;
        HudScreen hud;
        PauseScreen pause;
        ResultScreen result;
        CalibrationScreen calibration;

        SongEntry currentSong;
        ChartData currentChart;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_2022_2_OR_NEWER
            int refresh = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
#else
            int refresh = Screen.currentResolution.refreshRate;
#endif
            // 고주사율(90/120Hz) 기기에서는 그만큼 부드럽게 + 입력 지연 감소
            Application.targetFrameRate = Mathf.Max(60, refresh);
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;

            SetupCamera();
            EnsureEventSystem();

            conductor = new GameObject("Conductor").AddComponent<Conductor>();
            conductor.transform.SetParent(transform, false);

            field = new GameObject("Playfield").AddComponent<Playfield>();
            field.transform.SetParent(transform, false);
            field.Build(cam);

            gameplay = new GameObject("Gameplay").AddComponent<GameplayController>();
            gameplay.transform.SetParent(transform, false);
            gameplay.Init(conductor, field);
            gameplay.Judged += (lane, j, delta) => hud.OnJudged(j, delta);
            gameplay.Finished += OnSongFinished;

            input = new InputReader();

            var canvas = UIFactory.CreateCanvas("UI", 10);
            canvas.transform.SetParent(transform, false);
            menu = new MenuScreen(canvas.transform);
            hud = new HudScreen(canvas.transform);
            pause = new PauseScreen(canvas.transform);
            result = new ResultScreen(canvas.transform);
            calibration = new CalibrationScreen(canvas.transform, conductor);

            menu.PlayRequested += StartGame;
            menu.CalibrateRequested += ShowCalibration;
            hud.PauseRequested += PauseGame;
            pause.ResumeRequested += ResumeGame;
            pause.RetryRequested += Retry;
            pause.QuitRequested += ShowMenu;
            result.RetryRequested += Retry;
            result.MenuRequested += ShowMenu;
            calibration.BackRequested += ShowMenu;

            ShowMenu();
        }

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.035f, 0.07f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyEventSystem()) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(es);
        }

        static bool FindAnyEventSystem() => Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length > 0;

        void Update()
        {
            switch (state)
            {
                case State.Playing:
                    input.Poll(field.LaneFromScreen);
                    if (input.BackPressed) { PauseGame(); break; }
                    gameplay.Tick(input);
                    hud.Tick(gameplay);
                    break;

                case State.Paused:
                    input.Poll(field.LaneFromScreen);
                    if (input.BackPressed) ResumeGame();
                    break;

                case State.Calibration:
                    input.Poll(CalibrationScreen.TapArea);
                    if (input.BackPressed) { ShowMenu(); break; }
                    calibration.Tick(input);
                    break;

                case State.Result:
                    input.Poll(_ => -1);
                    if (input.BackPressed) ShowMenu();
                    break;

                case State.Menu:
                    input.Poll(_ => -1);
                    if (input.BackPressed) Application.Quit();
                    break;
            }
        }

        // 앱이 백그라운드로 가면(전화, 홈 버튼) 자동 일시정지
        void OnApplicationPause(bool paused)
        {
            if (paused && state == State.Playing) PauseGame();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus && state == State.Playing) PauseGame();
        }

        void HideAll()
        {
            menu.Hide();
            hud.Hide();
            pause.Hide();
            result.Hide();
            calibration.Hide();
        }

        void ShowMenu()
        {
            if (gameplay.IsPlaying) gameplay.Stop();
            conductor.Stop();
            HideAll();
            field.gameObject.SetActive(false);
            menu.Show();
            state = State.Menu;
        }

        void ShowCalibration()
        {
            HideAll();
            field.gameObject.SetActive(false);
            calibration.Show();
            state = State.Calibration;
        }

        void StartGame(SongEntry song, ChartData chart)
        {
            currentSong = song;
            currentChart = chart;
            HideAll();
            field.gameObject.SetActive(true);
            hud.Show($"{song.Title}  ·  {chart.difficulty} Lv.{chart.level}");
            gameplay.StartSong(song, chart, StartLeadIn);
            state = State.Playing;
        }

        void Retry()
        {
            if (gameplay.IsPlaying) gameplay.Stop();
            if (currentSong != null) StartGame(currentSong, currentChart);
            else ShowMenu();
        }

        void PauseGame()
        {
            if (state != State.Playing) return;
            conductor.Pause();
            pause.Show();
            state = State.Paused;
        }

        void ResumeGame()
        {
            if (state != State.Paused) return;
            pause.Hide();
            conductor.Resume(ResumeRewind);
            gameplay.NotifyResumed();
            state = State.Playing;
        }

        void OnSongFinished()
        {
            var score = gameplay.Score;
            bool newBest = GameSettings.TrySetBestScore(currentSong.Id, currentChart.difficulty, score.Score);
            HideAll();
            field.gameObject.SetActive(false);
            result.Show(currentSong, currentChart, score, newBest);
            state = State.Result;
        }
    }
}
