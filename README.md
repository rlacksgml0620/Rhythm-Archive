# Rhythm Archive

Unity로 만든 **4키 낙하형 모바일 리듬게임** 프로토타입입니다.
씬이나 프리팹, 이미지, 음원 파일 없이 **코드만으로** 화면과 데모곡을 만들기 때문에, 프로젝트를 열고 ▶ Play만 누르면 바로 플레이할 수 있습니다.

## 들어있는 기능

| 분류 | 내용 |
|---|---|
| 플레이 | 4레인, 단노트와 롱노트(홀드), 멀티터치 동시치기 |
| 판정 | PERFECT ±45ms / GREAT ±90ms / GOOD ±135ms / MISS, FAST·SLOW 표시 |
| 싱크 | 오디오 DSP 시계 기준 타이밍, 오프셋 수동 조절, **자동 싱크 보정**(메트로놈에 맞춰 탭) |
| 점수 | 100만 점 만점, 콤보, 정확도, 랭크(SSS~D), FULL COMBO / ALL PERFECT, 최고 기록 저장 |
| 화면 | 곡 선택, 난이도 선택, 노트 속도(1.0~10.0), 플레이 HUD, 일시정지, 결과 |
| 데모곡 | 코드로 합성한 오리지널 곡 *Neon Pulse* (140 BPM, 약 55초). 음원 저작권 걱정이 없습니다 |
| 난이도 | EASY Lv.3(노트 58개), NORMAL Lv.6(108개), HARD Lv.9(232개) |
| 모바일 대응 | 세로 화면 고정, 화면 꺼짐 방지, 고주사율 지원, 홈 버튼·전화 수신 시 자동 일시정지, 안드로이드 뒤로가기 |
| 커스텀 곡 | `Assets/Resources/Songs`에 음원과 JSON 채보를 넣으면 곡 목록에 자동으로 추가 ([가이드](Docs/CHART_FORMAT.md)) |

## 실행 방법

1. [Unity Hub](https://unity.com/download)를 설치하고 **Unity 6 (6000.0 LTS 이상)**을 설치합니다.
   - 설치할 때 모듈에서 **Android Build Support**(OpenJDK, Android SDK & NDK 포함)를 체크합니다. iOS로 출시하려면 iOS Build Support도 체크합니다(Mac 필요).
2. 이 저장소를 내려받습니다.
3. Unity Hub에서 **Add → Add project from disk**로 이 폴더를 엽니다.
   - 버전이 다르다는 경고가 나오면, 설치된 Unity 6 버전을 선택해서 열면 됩니다.
   - "Input System을 활성화할까요? (재시작 필요)" 창이 나오면 **Yes**를 누릅니다.
4. 처음 열 때 `Assets/Scenes/Main.unity` 씬이 자동으로 만들어지고 빌드 목록에 등록됩니다.
5. 상단의 ▶ **Play**를 누릅니다.
   - Game 뷰 해상도를 세로 비율(예: 1080x1920)로 바꾸면 폰과 같은 화면으로 볼 수 있습니다.
   - PC에서는 **D F J K** 키 또는 마우스 클릭으로 플레이합니다.

## 폰에서 실행해 보기 (Android)

1. **File → Build Profiles**(또는 Build Settings)에서 **Android**를 선택하고 *Switch Platform*을 누릅니다.
2. 메뉴 **Rhythm Archive → Apply Mobile Player Settings**를 누릅니다. 세로 화면, IL2CPP/ARM64, 앱 ID가 설정됩니다.
3. 폰에서 *개발자 옵션 → USB 디버깅*을 켜고 PC에 연결합니다.
4. **Build And Run**을 누르면 폰에 설치되고 실행됩니다.
5. 처음 실행하면 메뉴에서 **AUTO SYNC CALIBRATION**으로 싱크를 맞춰 주세요. 블루투스 이어폰은 지연이 크므로 꼭 필요합니다.

## 프로젝트 구조

```
Assets/
├─ Scripts/
│  ├─ Core/       GameManager(전체 흐름), Conductor(음악 시계), InputReader(입력), Judgement(판정·점수), GameSettings
│  ├─ Gameplay/   GameplayController(노트 처리), Playfield(레인·이펙트), NoteView, RuntimeSprites
│  ├─ Chart/      ChartData(채보 형식), SongLibrary(곡 목록 로드)
│  ├─ Audio/      Synth(신디사이저), DemoSong(데모곡·채보 생성)
│  └─ UI/         MenuScreen, HudScreen, PauseScreen, ResultScreen, CalibrationScreen, UIFactory
├─ Editor/        RhythmProjectSetup(씬 자동 생성, 모바일 설정)
└─ Resources/Songs/  ← 직접 만든 곡을 넣는 곳
Docs/CHART_FORMAT.md  커스텀 곡/채보 작성법
```

## 타이밍 처리 방식

리듬게임에서 가장 중요한 부분이라 따로 정리합니다.

- 노트 위치와 판정은 프레임 시간이 아니라 **`AudioSettings.dspTime`(오디오 하드웨어 시계)**를 기준으로 계산합니다. 프레임이 끊겨도 음악과 노트가 어긋나지 않습니다.
- 곡은 `PlayScheduled`로 정확한 시각에 예약 재생합니다.
- 오디오 버퍼를 256 샘플로 줄여 출력 지연을 낮췄습니다.
- 기기마다 다른 지연은 **오프셋**으로 보정합니다. 자동 보정은 16번 탭한 값의 중앙값을 사용합니다.

## 상용화까지 남은 작업

- [ ] 오리지널 곡 확보: 작곡가 계약이나 상업용 라이선스 음원. 기존 유명곡은 사용하면 안 됩니다
- [ ] 채보 에디터: 음악을 들으며 탭해서 노트를 녹음하는 도구
- [ ] 아트: 노트 스킨, 배경, 판정 이펙트, 폰트(한글 UI라면 한글 폰트 필요)
- [ ] 효과음: 타격음과 UI 사운드
- [ ] 수익화: 광고(예: Unity LevelPlay, AdMob), 곡 팩 인앱결제(Unity IAP)
- [ ] 스토어 준비: 아이콘, 스크린샷, 개인정보처리방침, Google Play 비공개 테스트
- [ ] 여러 기기 테스트: 저사양 안드로이드, 블루투스 이어폰 환경
