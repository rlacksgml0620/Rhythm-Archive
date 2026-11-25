# 커스텀 곡 추가하기

`Assets/Resources/Songs/` 폴더에 **음원 파일 + JSON 곡 파일**을 넣으면 게임의 곡 목록에 자동으로 나타납니다.

```
Assets/Resources/Songs/
├─ my_song.ogg        ← 음원 (ogg, mp3, wav 가능)
└─ my_song_chart.json ← 곡 정보 + 채보
```

> 음원과 JSON의 파일 이름(확장자 제외)이 같으면 Resources에서 이름이 겹치므로, 위처럼 다르게 짓는 것이 안전합니다.

## JSON 형식

```json
{
  "id": "my_song",
  "title": "My Song",
  "artist": "Composer Name",
  "audio": "Songs/my_song",
  "bpm": 128,
  "offset": 0.25,
  "timeUnit": "beat",
  "charts": [
    {
      "difficulty": "EASY",
      "level": 2,
      "notes": [
        { "time": 0,  "lane": 1 },
        { "time": 2,  "lane": 2 },
        { "time": 4,  "lane": 0, "duration": 2 },
        { "time": 8,  "lane": 1 },
        { "time": 8,  "lane": 2 }
      ]
    }
  ]
}
```

| 필드 | 설명 |
|---|---|
| `id` | 곡 고유 ID. 최고 기록 저장에 사용됩니다. 출시 후에는 바꾸지 마세요 |
| `audio` | `Resources` 폴더 기준 음원 경로, **확장자 제외** |
| `bpm` | 곡의 BPM. `timeUnit`이 `"beat"`일 때 박자를 초로 바꾸는 데 씁니다 |
| `offset` | 음원 파일에서 **첫 박(beat 0)이 시작되는 시각(초)** |
| `timeUnit` | `"beat"`(박자 단위, 권장) 또는 `"second"`(초 단위) |
| `charts[].difficulty` | `EASY`, `NORMAL`, `HARD` 또는 원하는 이름. 버튼 색은 앞의 세 가지만 정해져 있습니다 |
| `charts[].level` | 표시용 난이도 숫자 |
| `notes[].time` | 노트 시각 (`timeUnit` 단위) |
| `notes[].lane` | 0~3 (왼쪽부터) |
| `notes[].duration` | 생략하거나 0이면 단노트, 0보다 크면 롱노트 길이(`timeUnit` 단위) |

### 박자 단위 예시 (4/4박자)

- `0, 1, 2, 3` → 1마디의 1·2·3·4박
- `0.5` → 8분음표 엇박
- `4` → 2마디 첫 박
- `"duration": 2` → 2박 동안 누르는 롱노트

## 팁

- 같은 레인에서 겹치는 노트는 로드할 때 자동으로 제거됩니다.
- `offset`이 조금 틀리면 모든 노트가 일정하게 빠르거나 느리게 느껴집니다. 0.01초 단위로 조정해 보세요.
- 모바일 용량을 줄이려면 음원 Import Settings에서 **Load Type: Compressed In Memory**, **Compression: Vorbis**를 권장합니다.
- 음원은 **상업적 사용이 허락된 곡**만 사용하세요.
