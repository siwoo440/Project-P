# 프로젝트 P — 개발 기준선

보석 연결 퍼즐 + 턴제 전략 RPG. 1인 개발. Unity 6000.3.21f1 / 2D URP.

기준 기획서: https://docs.google.com/document/d/1hJNksh4wGaiMJcNdWm9G2PhJGs7tmS1sIrvnxYpz6sg/edit
문서 내용이 충돌하면 **기획서 탭(0~12장)이 레거시 탭보다 항상 우선**한다.

---

## 1. Scene 구조

게임 "단계"가 바뀔 때만 Scene을 분리한다. 같은 단계 내부의 화면 변화는 Panel 또는 State 전환으로 처리한다.

| Scene | 책임 |
|---|---|
| `00_Boot` | 전역 시스템·공통 데이터 초기화. 성공 시 즉시 01_Title로 전환 |
| `01_Title` | 새 게임 / 이어하기 / 설정 / 종료 |
| `02_MainHub` | 스테이지 **밖**의 전부 — 캐릭터·성장·편성·스테이지 선택 (Panel 전환) |
| `03_Gameplay` | 스테이지 **안**의 전부 — 이동·전투·상점·이벤트·보상 (State 전환) |
| `90_Ending` | 엔딩·크레딧. 프로토타입 제외 |
| `Dev_PuzzleTest` | 퍼즐 단독 검증. 빌드 제외 |
| `Dev_BattleTest` | 전투 단독 검증. 빌드 제외 |

기본 흐름: `00_Boot → 01_Title → 02_MainHub → 03_Gameplay → 02_MainHub`

### 금지 사항
- 화면마다 Scene을 만들지 않는다
- Room 종류를 Scene 이름으로 분기하지 않는다 → `RoomData`를 읽은 `RoomManager`가 State와 Panel을 활성화한다
- 챕터마다 Gameplay Scene을 만들지 않는다 → 배경·적·방 구성·보상은 `StageData`/`RoomData`로 주입한다
- MainHub 내부 메뉴 이동 때문에 Scene을 재로드하지 않는다
- Dev Scene 전용 규칙을 본 게임 로직에 넣지 않는다 → 실제 런타임 모듈을 그대로 쓰고 테스트 UI만 추가한다

---

## 2. 데이터 3계층

모든 데이터는 아래 셋 중 하나에만 속한다. 어디 소속인지 즉답할 수 없으면 코드를 쓰지 않는다.

| 계층 | 소유자 | 형태 | 수명 | 변경 주체 |
|---|---|---|---|---|
| **StaticData** | `DataManager` | ScriptableObject | 영구 불변 | 없음 (런타임 읽기 전용) |
| **SaveData** | `SaveManager` | 직렬화 클래스 → 디스크 | 영구 | 성장 서비스, 스테이지 클리어 처리 |
| **SessionData** | `03_Gameplay` | 직렬화 가능한 순수 C# 클래스 | **스테이지 1회** | Gameplay 내부 State |

- StaticData: 보석·캐릭터·스킬·적·스테이지·방·이벤트 정의
- SaveData: 캐릭터 레벨, 스킬 강화, 해금, 영구 재화, 챕터 진행, 이어하기 스냅샷
- SessionData: 현재 방, 방문 경로, 메인 현재 HP, 임시 재화, 임시 강화, 편성 스냅샷

### 절대 규칙
1. **UI는 원본 데이터를 직접 수정하지 않는다.** 전용 서비스에 변경을 요청하고 표시 데이터만 갱신한다.
2. **MainHub → Gameplay 전달은 참조가 아니라 값 복사(스냅샷)다.** 상점 임시 강화가 원본을 오염시키면 안 된다.
3. **스테이지 진행 상태는 전부 `SessionData` 안에만 존재한다.** `MonoBehaviour`는 상태를 소유하지 않는다.
4. **상점 구매 결과는 `SessionData`에만 반영한다.** `SaveData`에 직접 쓰지 않는다.

---

## 3. 매니저 수명

| 수명 | 생성 위치 | 대상 |
|---|---|---|
| 전역 (앱 종료까지) | `00_Boot` | `GameManager`, `SaveManager`, `AudioManager`, `DataManager`, `SceneFlow` |
| 스테이지 한정 | `03_Gameplay` | `StageManager`, `RoomManager`, `TurnManager`, `BattleManager`, `PuzzleManager`, `GameplayUIController` |

**`DontDestroyOnLoad`는 `00_Boot`에서만 호출한다.** 다른 곳에서 쓰면 매니저 중복 생성 버그가 난다.

`01_Title`은 장기 게임 상태를 보유하지 않는다. 버튼 이벤트는 `SceneFlow`에 전환을 요청만 한다.

### 전역 서비스 접근
- 전역 매니저는 `Bootstrapper`가 생성해 `GameServices`에 등록한다. 매니저에 `static Instance`를 두지 않는다.
- 다른 코드는 `GameServices.Data` / `.Save` / `.Audio` / `.Game` / `.Scenes`로만 접근한다.
- 초기화 순서: `DataManager` → `SaveManager` → `AudioManager` → `GameManager`·`SceneFlow` 등록 → 전환. 순서를 바꾸지 않는다.
- Scene 이름은 문자열로 직접 쓰지 않고 `SceneNames` 상수를 쓴다.
- Scene 간 `SessionData` 전달은 `GameManager.SetPendingSession` / `TakePendingSession`으로 한다.

### 에디터 Play 동작
- `EditorBootLoader`: 어느 Scene에서 Play해도 `00_Boot`를 먼저 거친 뒤 원래 Scene으로 돌아온다. 빌드에서는 동작하지 않는다.
- 원래 Scene은 디스크에서 다시 불러온다. **Play 전에 Scene을 저장해야 변경이 반영된다.**

---

## 4. 저장 규칙

저장 시점은 4개뿐이다:
1. 스테이지 시작
2. **방 이동 완료** (= 방 진입 직후, 방 내용 처리 **전**)
3. 스테이지 클리어
4. 캐릭터 성장 변경

전투 중간 상태는 저장하지 않는다. 게임 재실행 시 현재 방 진입 직전 상태에서 재개한다.

**이어하기와 패배 재도전은 같은 메커니즘이다** — 둘 다 "방 진입 직전 `SessionData` 스냅샷"을 되감는다. 재도전용 사본은 Gameplay가 메모리에도 들고 있는다.

### 저장 파일 처리
- 위치: `Application.persistentDataPath`의 `save.json`(진행), `settings.json`(설정). **설정은 진행과 분리한다** — 새 게임으로 진행을 지워도 설정은 유지된다.
- 쓰기는 임시 파일에 쓴 뒤 교체한다(원자적 쓰기).
- 손상된 파일은 `.bak`으로 백업하고 새 데이터로 시작한다. 디스크에 쓸 수 없는 경우만 Boot를 멈춘다.
- 직렬화는 `JsonUtility`를 쓴다. **JsonUtility는 커스텀 클래스 필드의 null을 보존하지 못한다** — 불러오면 빈 객체가 채워진다. 존재 여부는 null이 아니라 `bool` 플래그로 판정한다 (예: `SaveData.hasSuspendedSession`).

---

## 5. 프로토타입 범위

아래가 첫 플레이 가능 버전의 전부다. 이 선을 넘는 기능은 "6단계 이후"로 미룬다.

| 항목 | 범위 |
|---|---|
| 캐릭터 | 4명 (메인 1 + 패시브 3) |
| 스킬 | 대표 스킬 4개 + 패시브 3개 |
| 적 | 2~3종 |
| 보석 | 5종 |
| 보드 | **6행 × 12열** (기획서 5.1은 4행 — 2026-09-23 사용자 결정으로 변경) |
| 연결 | 8방향, 최소 2개, 혼합 연결 |
| 행동력 | 기본 6, 보석 1개당 1 소비, 턴당 연결 1회 |
| 스킬 자원 | 턴 기반 쿨타임, 턴당 파티 전체 1회 |
| 적 행동 | 행동 카운트, 다음 행동 사전 공개 |
| 피버 | 게이지 100 → 2턴, 행동력 +3, 보석 효과 +50% |
| 화면 | Exploration + Battle State만 |

**제외**: 보스, 성장, 상점, 이벤트, 스토리, 게임패드, 엔딩, 난이도 분기

---

## 6. 핵심 게임 규칙 요약

- 보석 5종: 물리(물리 피해) / 마법(마법 피해) / 회복(메인 HP) / 혼돈(적 행동 카운트 +1) / 균형(다음 턴 행동력 +)
- 8방향 드래그 연결, 경로 내 재선택 금지, 우클릭·ESC 취소
  - **되돌리기 허용**: 직전 보석으로 되돌아가면 마지막 선택을 취소한다(재선택이 아님)
  - **보드 밖에서 놓아도 확정**한다 ("놓는 순간 확정"). 최소 개수 미만이면 조용히 취소
  - 마우스 판정은 칸 중심의 **80% 원** 안 — 대각선으로 그을 때 옆 칸이 잘못 잡히지 않게
  - 연결선은 **강조색(금색) 한 가지**, 가늘게. 선택 표시는 칸 외곽선·확대가 맡는다
- 동일 종류 연속 시 2번째부터 **+20% 누적** (3연속 = 120% / 140%)
- 4연속 → 강공격 / 5연속 이상 → 강공격 + 특수 보석(경로 마지막 칸 생성)
- 피버 충전: 보석 1개당 +5, 4연속 이상 추가 +10
- 파티: 메인 1 + 패시브 3. **메인 스탯만 전투 판정 기준**. 패시브는 피격되지 않고 전투 중 역할 교체 없음
- 패시브 캐릭터 GameObject를 전투 필드에 필수 생성하지 않는다 (데이터/로직으로 처리)
- 적 행동: 플레이어 행동 후 전체 카운트 -1 → 0인 적 행동 → 기본값 복귀. 동시 0이면 화면 왼쪽부터
- 승리 = 적 전멸 / 패배 = 메인 HP 0

---

## 7. 코딩 규칙

- 네임스페이스: `ProjectP.<영역>` (예: `ProjectP.Data`, `ProjectP.Gameplay.Puzzle`)
- 우리 코드·에셋은 전부 `Assets/_Project/` 아래. 템플릿·외부 에셋과 섞지 않는다

### 어셈블리 구조
| 어셈블리 | 위치 | 내용 |
|---|---|---|
| `ProjectP.Runtime` | `Scripts/` | 게임 코드 전부 |
| `ProjectP.Editor` | `Scripts/Dev/Editor/` | 조립 메뉴 등 에디터 도구 (Editor 전용) |
| `ProjectP.Tests.EditMode` | `Tests/EditMode/` | 자동 테스트 (NUnit) |

- `ProjectP.Runtime` 폴더 안에서 `Editor` 폴더 이름만으로는 에디터 전용이 되지 않는다. 에디터 코드는 반드시 `ProjectP.Editor` 쪽에 둔다.
- 새 에디터 코드 폴더를 만들면 asmdef가 필요하다.

### 규칙과 화면 분리 (퍼즐·전투 공통)
- **게임 규칙은 Unity에 의존하지 않는 순수 C#** 으로 만든다 (예: `Board`, `BoardGenerator`). 그래야 Play 없이 자동 테스트할 수 있다.
- 화면(`BoardView` 등)은 규칙 모델을 **읽기만** 한다.
- `PuzzleManager` 같은 MonoBehaviour가 규칙과 화면을 묶는다. Dev Scene과 본 게임이 같은 컴포넌트를 쓴다.
- 무작위는 `UnityEngine.Random`이 아니라 **시드를 받는 `System.Random`** 을 쓴다. 같은 시드 = 같은 결과.
- 규칙을 추가하면 `Tests/EditMode`에 테스트를 함께 추가한다.
- 입력(`BoardInput`)·연결 표시(`ConnectionView`)는 `PuzzleManager` 메서드를 호출하거나 이벤트를 받기만 한다.

### 검증 도구 (Unity 없이)
- `bash Tools/Verify/verify.sh` (Git Bash) — 어셈블리 4종(게임 빌드·에디터용 런타임·에디터 도구·테스트)을 Unity 내장 컴파일러로 컴파일하고, 순수 C# 테스트를 Unity 번들 .NET으로 실행한다.
- Unity 에디터가 열려 있어도 쓸 수 있다. Unity 네이티브 기능이 필요한 테스트(ScriptableObject 등)는 건너뛰므로 에디터 Test Runner로 한 번 더 확인한다.
- 조립 도구가 이름으로 연결하는 필드(`Wire`)는 컴파일러가 못 잡으므로, 필드명을 바꾸면 조립 도구도 함께 고친다.

### 보드 좌표
- **행 0 = 맨 아래, 열 0 = 맨 왼쪽.** 칸 번호 = `row * Columns + column`.
- 낙하는 행 번호가 줄어드는 방향, 보충은 맨 위 행부터.
- 보상 계산과 UI 표시를 분리한다. UI는 계산된 결과만 표시하고 중복 지급 방지 플래그를 쓴다
- 이벤트 결과를 UI 문구에 하드코딩하지 않는다. `EventData` 기반으로 처리한다
- 와이어프레임 이미지보다 기획서 본문의 책임·데이터 흐름 정의가 우선이다

### UI 규칙
- UI는 **uGUI + TextMeshPro**를 쓴다. 이후 전투 HUD(보석 드래그·연결선)와 기술을 통일하기 위함이다.
- EventSystem에는 **`InputSystemUIInputModule`** 을 쓴다. 프로젝트가 새 Input System 전용이라 `StandaloneInputModule`은 동작하지 않는다.
- Canvas는 `Scale With Screen Size`, 기준 해상도 **1920×1080**, Match 0.5. (기획서 10.1 공란 → 레거시 옵션표 기본값)
- 설정 변경은 `AudioManager.SetVolume`처럼 서비스 메서드로 요청한다. UI가 `SettingsData`를 직접 고치지 않는다.
- 버튼을 누르면 해당 화면의 버튼을 모두 잠가 중복 입력을 막는다.
- 한글 표시: TMP 설정의 fallback에 `KoreanFallback_MalgunGothic`(Windows 맑은 고딕을 **참조만** 하는 Dynamic OS 폰트)을 등록했다. 폰트 파일은 프로젝트에 없다. **개발용 임시 조치이며 출시 전에 배포 가능한 폰트(Noto Sans KR 등)로 교체한다.**
- 한글 폰트 에셋은 동적 방식이라 새 글자가 화면에 쓰일 때마다 에셋 파일(약 2MB)이 바뀐다. 커밋할 때 폰트 에셋 변경만 섞여 있으면 되돌려도 된다. 글자는 다시 필요할 때 자동으로 생성된다.
- Build All은 모든 화면을 다시 만들기 때문에, 내용이 같아도 씬 내부 ID가 바뀌어 큰 diff가 생긴다. 커밋 전에 ID를 무시하고 비교해 **내용 차이가 없는 씬은 되돌린다**. 조립 도구를 바꾸지 않은 화면이 여기에 해당한다.
- **디자인 값은 `Art/UI/UITheme.asset` 한 곳에서 관리한다.** 색을 바꾸고 Build All을 다시 실행하면 전체 화면에 반영된다.
- UI 그래픽(둥근 사각형·그림자·보석 타일·보석 아이콘)은 `SpriteShapes`가 코드로 그린 임시 그래픽이다. 아트가 확정되면 `UITheme`·`GemData`의 스프라이트만 교체한다.
- 보석은 색만으로 구분하지 않는다(기획서 10.2). 종류별 고유 모양: 물리 ◆ / 마법 ★ / 회복 ✚ / 혼돈 ✸ / 균형 ◎.
- 버튼 스타일: Primary(강조색, 주 행동) / Secondary(보조) / Danger(되돌릴 수 없는 행동).
- UI 조립 메뉴:
  - `Project P > Build All` — TMP 준비 → UI 그래픽 → 퍼즐 데이터 → 00_Boot → 01_Title → Dev_PuzzleTest 순서로 전부 다시 만든다.
  - `Project P > Rebuild > ...` — 화면 하나만 다시 만든다.
  - 다시 만들면 해당 화면의 조립 루트(`[BootUI]`, `[TitleUI]`, `[PuzzleTestUI]`, `[Puzzle]`)를 지우고 새로 만든다. 씬에서 직접 다듬은 배치는 사라진다.
  - 배치 모드: `-executeMethod ProjectP.EditorTools.ProjectSetupMenu.BuildAllBatch`
- 크래시 후 Unity가 만드는 `Assets/_Recovery/`는 `.gitignore`로 제외했다. 커밋하지 않는다.

---

## 8. 진행 상황

전체 36일차 계획. 현재 위치만 갱신한다.

- [x] **1일차** 기획·씬 구조 확정 — 이 문서가 산출물
- [x] **2일차** Unity 프로젝트 기본 구조 — 폴더·Scene 7개·빌드 목록 등록 완료
- [x] **3일차** 00_Boot 및 전역 서비스 — Boot→Title 전환·settings.json 생성·Dev 씬 Boot 경유 복귀 확인 (손상 파일·초기화 실패 처리는 미확인)
- [x] **4일차** 01_Title 및 Scene 전환 — UI 조립·새 게임→MainHub 확인 (이어하기·확인창·설정 유지·종료는 미확인)
- [x] **5일차** 보석 데이터·6×12 보드·UI 디자인 — Build All 완료, 에디터 Test Runner 13/13 통과 (Dev_PuzzleTest Play 화면은 미확인)
- [x] **6일차** 보석 연결 입력 — Build All 완료, Test Runner 36/36, Dev_PuzzleTest Play 오류 없음 (드래그 조작감은 기록으로 미확인)
- [ ] 7~10일차 퍼즐 프로토타입 (행동력·효과·낙하·콤보·피버)
- [ ] 11~17일차 전투 프로토타입 (Dev_BattleTest)
- [ ] 18~23일차 Gameplay 통합 → **첫 플레이 가능 버전**
- [ ] 24~28일차 MainHub·성장·저장
- [ ] 29~33일차 확장 콘텐츠
- [ ] 34~36일차 검증·폴리싱

---

## 9. 미확정 항목 (구현 전 확정 필요)

기획서에 `(공란)`으로 남아 있어 해당 일차 착수 전에 값을 정해야 하는 것들:

- **5~8일차 전**: 보석별 피해·회복 계수, 연속 보너스 상한, 강공격 배율, 특수 보석 종류·효과
- **임시 적용 중**: 보석 생성 확률 5종 균등(가중치 20) / 혼돈 보석 색은 검정 대신 짙은 보라 — 밸런스·아트 단계에서 재검토
- **11~14일차 전**: 물리·마법 피해 공식, 회복 공식, 방어 적용 여부, 최소 피해, 반올림 규칙, 캐릭터 4명·적 3종 스탯
- **19일차 전**: `StageData` / `RoomData` 필드 정의, 노드 연결 규칙, 랜덤 방 배치 규칙
- **미정**: 동일 캐릭터 중복 편성 가능 여부 → 확정 전까지 구현 규칙으로 가정하지 않는다
- **28일차 전**: 스테이지 도중 종료한 게임의 이어하기 경로. 기획서 11.1(방 진입 직전부터 재개)과 12.3(이어하기 → MainHub)이 다르다. 4일차에는 12.3대로 MainHub로 보낸다.
- **출시 전**: 한글 폰트를 배포 가능한 폰트로 교체 (현재는 OS 맑은 고딕 참조)
