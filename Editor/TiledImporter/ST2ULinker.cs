using System.Linq;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;



#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public static class ST2ULinker
{
    private static string superTiled2UnityPackageId = "com.seanba.super-tiled2unity";
    private static string superTiled2UnityGitUrl = "https://github.com/Seanba/SuperTiled2Unity.git?path=/SuperTiled2Unity/Packages/com.seanba.super-tiled2unity";
    public static bool? isSuperTiled2UnityInstalled{get;private set;}

    public static bool CheckSuperTiled2Unity()
    {
        // Request the list of installed packages
        ListRequest listRequest = Client.List();

        // Wait for the request to finish
        while (!listRequest.IsCompleted)
        {
        }
        if (listRequest.Status == StatusCode.Success)
        {
            var installedPackages = listRequest.Result;

            // Check if SuperTiled2Unity is installed
            bool packageFound = installedPackages.Any(p => p.name == superTiled2UnityPackageId);

            if (packageFound)
            {
                isSuperTiled2UnityInstalled = true;
                return true;
            }
            else
            {
                isSuperTiled2UnityInstalled = false;
                return false;
            }
        }
        return false;
    }

    public static void AddPackage()
    {
        // Create a request to add the package
        AddRequest addRequest = Client.Add(superTiled2UnityGitUrl);

        // Wait for the request to complete
        while (!addRequest.IsCompleted)
        {
            // You can optionally display a loading progress bar here
            EditorUtility.DisplayProgressBar("Adding Package", "Please wait while the package is added...", 0.5f);
        }
        EditorUtility.ClearProgressBar();

        // Check the result
        if (addRequest.Status == StatusCode.Success)
        {
            UnityEngine.Debug.Log("Package added successfully: " + superTiled2UnityGitUrl);
        }
        else
        {
            UnityEngine.Debug.LogError("Failed to add package: " + superTiled2UnityGitUrl);
        }

    }

    [InitializeOnLoadMethod,
    #if FINGDEBUG
    MenuItem("FingTools/DEBUG/Force Define Scripting Symbols Update")
    #endif
    ]
    public static void DefineAllST2USymbol()
    {
        CheckSuperTiled2Unity();
        NamedBuildTarget[] builds = { NamedBuildTarget.Standalone, NamedBuildTarget.Android, NamedBuildTarget.WebGL };
        foreach (NamedBuildTarget namedBuildTarget in builds)
        {
            DefineST2USymbol(namedBuildTarget);
        }
        
    }

    private static void DefineST2USymbol(NamedBuildTarget build)
    {
        if (isSuperTiled2UnityInstalled == true)
        {
            var scriptingString = PlayerSettings.GetScriptingDefineSymbols(build);
            if (string.IsNullOrEmpty(scriptingString))
            {
                PlayerSettings.SetScriptingDefineSymbols(build, "SUPER_TILED2UNITY_INSTALLED");
            }
            else if (!scriptingString.Contains("SUPER_TILED2UNITY_INSTALLED"))
            {
                scriptingString += ";SUPER_TILED2UNITY_INSTALLED";
                PlayerSettings.SetScriptingDefineSymbols(build, scriptingString);
            }
        }
        else
        {
            var scriptingString = PlayerSettings.GetScriptingDefineSymbols(build);
            if (scriptingString.Contains("SUPER_TILED2UNITY_INSTALLED"))
            {
                // Remove the "SUPER_TILED2UNITY_INSTALLED" entry from the string
                var defines = scriptingString.Split(';')
                                            .Where(define => define != "SUPER_TILED2UNITY_INSTALLED")
                                            .ToArray();

                // Join the remaining defines and update the symbols
                PlayerSettings.SetScriptingDefineSymbols(build, string.Join(";", defines));
            }
        }
    }
}
[InitializeOnLoad]
public static class CompilationErrorHandler
{
    static CompilationErrorHandler()
    {
        CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
    }

    private static void OnAssemblyCompilationFinished(string assembly, CompilerMessage[] messages)
    {
        // Check if any of the compiler messages are errors
        bool hasPackageErrors = false;
        foreach (var message in messages)
        {
            if (message.type == CompilerMessageType.Error && message.message.Contains("FingTools"))
            {
                hasPackageErrors = true;
            }
        }

        if (hasPackageErrors)
        {
            // Custom logic to execute on compilation failure
            Debug.LogWarning("Compilation failed because SuperTiled2Unity has been removed. Re-checking define symbol");
            ST2ULinker.DefineAllST2USymbol();
        }
    }
}
#endif