using System;
using System.IO;
using System.Linq;
using System.Reflection;

// Unity 없이 NUnit [Test] 메서드를 직접 실행하는 최소 러너. Unity 네이티브 기능이 필요한 테스트는 건너뛴다.
static class Runner
{
    static int Main(string[] args)
    {
        var searchDirs = args.Skip(1).ToArray();
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
        {
            var file = new AssemblyName(e.Name).Name + ".dll";
            foreach (var dir in searchDirs) { var p = Path.Combine(dir, file); if (File.Exists(p)) return Assembly.LoadFrom(p); }
            return null;
        };

        var unityOnly = new[] { "PuzzleConfig_DefaultsToSixRowsTwelveColumns" };
        int pass = 0, fail = 0, skip = 0;
        foreach (var type in Assembly.LoadFrom(args[0]).GetTypes())
        foreach (var method in type.GetMethods())
        {
            if (!method.GetCustomAttributes().Any(a => a.GetType().Name == "TestAttribute")) continue;
            var name = $"{type.Name}.{method.Name}";
            if (unityOnly.Contains(method.Name)) { skip++; Console.WriteLine($"SKIP {name} (Unity 에디터 전용)"); continue; }
            try { method.Invoke(Activator.CreateInstance(type), null); pass++; Console.WriteLine($"PASS {name}"); }
            catch (TargetInvocationException e) { fail++; Console.WriteLine($"FAIL {name}\n     {e.InnerException.GetType().Name}: {e.InnerException.Message.Trim()}"); }
        }

        Console.WriteLine($"\n결과: 통과 {pass} / 실패 {fail} / 건너뜀 {skip}");
        return fail == 0 ? 0 : 1;
    }
}
