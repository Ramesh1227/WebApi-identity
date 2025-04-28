using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FaceAuthenticationsFunctions.Models
{
    public static class NativeLibraryPathHelper
    {
        public static void EnsureNativeDllPath()
        {
            string nativeLibPath = "/opt/Dependencies";

            // 1. Check environment override first
            var envOverride = Environment.GetEnvironmentVariable("NATIVE_DLL_PATH");
            if (!string.IsNullOrWhiteSpace(envOverride))
            {
                nativeLibPath = envOverride;
                Console.WriteLine($"[NativeLibraryPathHelper] Using NATIVE_DLL_PATH override: {nativeLibPath}");
            }

            // 2. Detect if running inside AWS Lambda
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")))
            {
                nativeLibPath = "/opt/Dependencies"; //  Fixed path
                Console.WriteLine($"[NativeLibraryPathHelper] Running in Lambda. Using: {nativeLibPath}");
            }

            // 3. Fallback: assume local run or Lambda Test Tool
            else
            {
                // Starting from AppContext.BaseDirectory, walk up to find /bin/Debug/net8.0
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && dir.Name != "net8.0")
                {
                    dir = dir.Parent;
                }

                if (dir != null)
                {
                    nativeLibPath = Path.Combine(dir.FullName,"opt","Dependencies");
                    Console.WriteLine($"[NativeLibraryPathHelper] Running locally. Using: {nativeLibPath}");
                }
                else
                {
                    Console.WriteLine("[NativeLibraryPathHelper] Could not locate 'net8.0' bin directory. Set NATIVE_DLL_PATH.");
                    return;
                }
            }

            // 4. Add to PATH if exists
            if (Directory.Exists(nativeLibPath))
            {
                var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

                if (!currentPath.Contains(nativeLibPath, StringComparison.OrdinalIgnoreCase))
                {
                    Environment.SetEnvironmentVariable("PATH", $"{nativeLibPath};{currentPath}");
                    Console.WriteLine($"[NativeLibraryPathHelper] PATH updated with: {nativeLibPath}");
                }
            }
            else
            {
                Console.WriteLine($"[NativeLibraryPathHelper] Native DLL folder not found: {nativeLibPath}");
            }

            // 5. Try loading one key native DLL to confirm success
            string testDll = Path.Combine(nativeLibPath, "libNBiometrics.dll");

            if (File.Exists(testDll))
            {
                if (NativeLibrary.TryLoad(testDll, out var handle))
                {
                    Console.WriteLine($"✅ [NativeLibraryPathHelper] Successfully loaded: {testDll}");
                    NativeLibrary.Free(handle); // Clean up
                }
                else
                {
                    Console.WriteLine($"❌ [NativeLibraryPathHelper] Failed to load: {testDll}");
                }
            }
            else
            {
                Console.WriteLine($"⚠️ [NativeLibraryPathHelper] Test DLL not found: {testDll}");
            }

        }
    }
}
