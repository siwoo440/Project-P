#!/usr/bin/env bash
# Unity를 열지 않고 코드를 검증한다.
#   1) 게임 빌드 / 에디터용 런타임 / 에디터 도구 / 테스트 — 4개 어셈블리를 Unity 내장 컴파일러로 컴파일
#   2) 순수 C# 규칙 테스트(NUnit)를 Unity 번들 .NET 런타임으로 실행 (Unity 네이티브 기능이 필요한 테스트는 건너뜀)
# 사용법 (Git Bash): bash Tools/Verify/verify.sh
# 전제: Unity 에디터로 프로젝트를 한 번 열어 Library/ScriptAssemblies와 PackageCache가 있어야 한다.
set -u
ROOT="$(cd "$(dirname "$0")/../.." && (pwd -W 2>/dev/null || pwd))"  # Windows 컴파일러용 D:/ 형식 경로
U="${UNITY_EDITOR_DATA:-C:/Program Files/Unity/Hub/Editor/6000.3.21f1/Editor/Data}"
OUT="$ROOT/Temp/ProjectPVerify"; SA="$ROOT/Library/ScriptAssemblies"; mkdir -p "$OUT"
DOTNET="$U/NetCoreRuntime/dotnet.exe"; CSC="$U/DotNetSdkRoslyn/csc.dll"
NUNIT=$(ls "$ROOT"/Library/PackageCache/com.unity.ext.nunit@*/net40/unity-custom/nunit.framework.dll | head -1)
SHIM="$U/NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll"
cd "$ROOT/Assets/_Project"

base() {
  echo "-nologo -nostdlib+ -target:library -langversion:9.0 -nowarn:1591,0649"
  echo "\"-r:$U/NetStandard/ref/2.1.0/netstandard.dll\""
  for f in "$U"/Managed/UnityEngine/*.dll; do echo "\"-r:$f\""; done
  for d in UnityEngine.UI Unity.TextMeshPro Unity.InputSystem; do echo "\"-r:$SA/$d.dll\""; done
}
{ base; find Scripts -name "*.cs" -not -path "*/Dev/Editor/*" | sed 's|^|"|;s|$|"|'; } > "$OUT/runtime.rsp"
{ base; echo "\"-r:$OUT/ProjectP.Runtime.dll\""; find Scripts/Dev/Editor -name "*.cs" | sed 's|^|"|;s|$|"|'; } > "$OUT/editor.rsp"
{ base; echo "\"-r:$OUT/ProjectP.Runtime.dll\""; echo "\"-r:$NUNIT\""; echo "\"-r:$SHIM\""; find Tests -name "*.cs" | sed 's|^|"|;s|$|"|'; } > "$OUT/tests.rsp"

fail=0
run() {
  local label=$1; shift
  "$DOTNET" "$CSC" "$@" > "$OUT/csc.log" 2>&1; local code=$?
  if [ "$code" = "0" ]; then echo "컴파일 OK   $label (경고 $(grep -c "warning CS" "$OUT/csc.log"))"
  else echo "컴파일 실패 $label"; grep -E "error|warning" "$OUT/csc.log" | head -20; fail=1; fi
}
run "게임 빌드"      "@$OUT/runtime.rsp" "-out:$OUT/player.dll"
run "런타임(에디터)" "@$OUT/runtime.rsp" -define:UNITY_EDITOR "-out:$OUT/ProjectP.Runtime.dll"
run "에디터 도구"    "@$OUT/editor.rsp" -define:UNITY_EDITOR "-out:$OUT/ProjectP.Editor.dll"
run "테스트"         "@$OUT/tests.rsp" -define:UNITY_EDITOR -define:UNITY_INCLUDE_TESTS "-out:$OUT/ProjectP.Tests.dll"
[ "$fail" = "1" ] && exit 1

"$DOTNET" "$CSC" -nologo -nostdlib+ -target:exe -langversion:9.0 "-r:$U/NetStandard/ref/2.1.0/netstandard.dll" \
  "-out:$OUT/Runner.dll" "$ROOT/Tools/Verify/Runner.cs" > "$OUT/csc.log" 2>&1 || { echo "러너 컴파일 실패"; cat "$OUT/csc.log"; exit 1; }
echo '{ "runtimeOptions": { "tfm": "net6.0", "framework": { "name": "Microsoft.NETCore.App", "version": "6.0.0" }, "rollForward": "LatestMinor" } }' > "$OUT/Runner.runtimeconfig.json"

echo "----- 테스트 실행 (Unity 없이) -----"
"$DOTNET" "$OUT/Runner.dll" "$OUT/ProjectP.Tests.dll" "$OUT" "$(dirname "$NUNIT")" "$U/Managed/UnityEngine" "$(dirname "$SHIM")" \
  | { iconv -f cp949 -t utf-8 2>/dev/null || cat; }
exit "${PIPESTATUS[0]}"
