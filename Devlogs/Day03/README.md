# 3일차 — 00_Boot 및 전역 서비스

**날짜**: 2026-09-23
**단계**: 1단계 · 개발 기반 구축
**상태**: 구현 완료 · Play 모드 검증 남음

---

## 1. 목표

게임 실행 직후 전역 서비스를 정해진 순서로 초기화하고, Scene이 바뀌어도 유지되는 구조를 만든다. 초기화에 성공하면 `01_Title`로 자동 전환하고, 실패하면 오류를 표시한 채 멈춘다. 기획서 12.2 기준이다.

---

## 2. 구현 내용

### 2.1 전역 서비스 구조

| 서비스 | 역할 |
|---|---|
| `Bootstrapper` | 00_Boot의 유일한 진입점. 초기화 순서 제어, `DontDestroyOnLoad` 유일 호출 지점 |
| `GameServices` | 전역 서비스 접근 창구. 다른 코드는 여기로만 접근한다 |
| `DataManager` | StaticData(`GameDatabase`) 조회. 수정 기능 없음 |
| `SaveManager` | `save.json`(진행), `settings.json`(설정) 읽기·쓰기 |
| `AudioManager` | BGM 1채널 + SFX 재생, 설정 볼륨 적용 |
| `GameManager` | Scene 간 `SessionData` 전달 슬롯, 게임 종료 |
| `SceneFlow` | Scene 전환 요청 처리, 전환 중 중복 요청 차단 |
| `SceneNames` | Scene 이름 상수 |
| `EditorBootLoader` | 에디터 전용. 어느 Scene에서 Play해도 Boot를 먼저 거친 뒤 원래 Scene으로 복귀 |

매니저가 각자 `static Instance`를 갖지 않는다. `Bootstrapper`가 생성해 `GameServices`에 등록하는 방식으로, "`DontDestroyOnLoad`는 00_Boot에서만" 규칙을 코드 구조로 강제했다.

### 2.2 초기화 순서

```
DataManager → SaveManager → AudioManager → GameManager·SceneFlow 등록 → 01_Title 전환
```

`AudioManager`는 설정 파일의 볼륨을 쓰므로 반드시 `SaveManager` 다음이다. Unity의 `Awake` 순서에 맡기지 않고 `Bootstrapper`가 직접 호출한다.

### 2.3 저장 파일 처리

- 설정(`settings.json`)을 진행(`save.json`)과 분리했다. 새 게임으로 진행을 지워도 설정은 유지된다.
- 임시 파일에 먼저 쓰고 교체하는 원자적 쓰기로, 저장 중 종료돼도 기존 파일이 깨지지 않는다.
- 손상된 파일은 `.bak`으로 백업하고 새로 시작한다. 디스크에 쓸 수 없는 경우만 Boot를 멈춘다.

### 2.4 1일차 데이터 계약 수정

`SaveData`에 `hasSuspendedSession` 플래그를 추가했다. 1일차에는 "`suspendedSession`이 null이면 진행 중인 스테이지 없음"으로 정의했는데, `JsonUtility`는 커스텀 클래스 필드의 null을 보존하지 못하고 불러올 때 빈 객체로 채운다. 그대로 두면 이어하기가 항상 활성화되는 버그가 생긴다. 아래 검증에서 이 동작을 실제로 확인했다.

### 2.5 Scene·에셋

- `00_Boot`에 `[Bootstrap]` 오브젝트를 배치하고 `GameDatabase` 에셋을 인스펙터에 연결했다.
- `GameDatabase`는 지금은 빈 틀이며 5일차 보석 데이터부터 채운다.
- Boot 화면은 진행 상태와 오류 문구만 표시하는 임시 화면이다. 로고 화면은 아트 확정 후 교체한다.

---

## 3. 검증

### 3.1 통과

**컴파일** — Unity 내장 C# 컴파일러로 에디터 빌드(`UNITY_EDITOR`)와 게임 빌드 두 가지를 모두 컴파일했다. 오류 0건.

**Unity 헤드리스 실행** — 스크립트 컴파일·에셋 임포트 정상. 에디터 검사 13항목 모두 통과.

- 빌드 목록: 정식 Scene 5개, `00_Boot`가 0번
- Scene 7개 모두 에셋으로 인식
- `GameDatabase` 에셋이 올바른 타입으로 로드됨
- `00_Boot`에 `Bootstrapper` 1개, `Database`에 `GameDatabase` 연결됨
- `JsonUtility`가 null 필드를 빈 객체로 채우는 것을 실제로 확인 → `hasSuspendedSession` 플래그 필요성 실증
- `hasSuspendedSession` 기본값 `false` 유지

### 3.2 미완료 — Play 모드 검사

아래 항목은 자동 검증 도중 Unity가 크래시해 확인하지 못했다. 에디터에서 직접 확인해야 한다.

- 00_Boot에서 Play → 01_Title 자동 전환, `[GameServices]` 1세트만 존재
- Dev_PuzzleTest에서 Play → Boot 경유 후 Dev_PuzzleTest로 복귀
- `settings.json` 생성
- 손상된 `settings.json` → `.bak` 백업 후 정상 진행
- `Database` 연결 해제 → 오류 표시 후 중단

**크래시 원인**: Play 모드 진입 중 Unity 내부 에셋 데이터베이스(LMDB) 조회에서 크래시가 발생했다. 호출 경로는 Visual Scripting 패키지 초기화 → `Resources.Load` → 에셋 DB였고, 스택에 프로젝트 코드는 없었다. 당시 가용 메모리가 1.5GB(총 15.6GB)였고 Play 진입 시 어셈블리 리로드에 182초가 걸리는 등 시스템 자원이 부족한 상태였다.

---

## 4. 참고 사항

- **메모리 부족**: 크래시 당시 가용 메모리 1.5GB. Unity 작업 중에는 다른 무거운 프로그램을 줄이는 것이 좋다.
- **소프트웨어 렌더링**: 에디터가 여전히 Microsoft Basic Render Driver로 동작한다. 그래픽 드라이버 점검이 필요하다.
- **Visual Scripting 패키지**: 템플릿 기본 포함 패키지로 프로젝트에서 사용하지 않는다. 이번 크래시 경로에 있었으므로 제거를 검토한다.
- **Scene 직렬화 형식**: 2일차 Scene은 템플릿의 구버전 형식이다. 에디터에서 저장하면 Unity 6 형식으로 갱신되어 diff가 생기는데, 정상이다.

---

## 5. 다음 단계

**4일차 — 01_Title 및 Scene 전환**

새 게임·이어하기·설정·게임 종료 UI를 만들고 `SceneFlow`와 연결한다. 저장 파일 유무에 따라 이어하기 버튼 활성 상태를 처리한다. 착수 전에 3.2의 Play 모드 검사를 먼저 마친다.
