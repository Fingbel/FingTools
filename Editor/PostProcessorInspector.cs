using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class PostprocessorInspector
{
    [MenuItem("Tools/List All Asset Postprocessors")]
    public static void ListAllPostprocessors()
    {
        // Find all types that inherit from AssetPostprocessor
        var postprocessorTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(AssetPostprocessor)) && !type.IsAbstract);

        // Prepare list for display
        List<(string Name, int Order)> postprocessors = new List<(string Name, int Order)>();

        foreach (var type in postprocessorTypes)
        {
            // Create an instance of the postprocessor to call GetPostprocessOrder
            var instance = Activator.CreateInstance(type);
            MethodInfo method = type.GetMethod("GetPostprocessOrder");
            int order = 0;

            if (method != null && method.DeclaringType == type) // Ensure method is overridden
            {
                order = (int)method.Invoke(instance, null);
            }

            postprocessors.Add((type.FullName, order));
        }

        // Sort by order for clarity
        postprocessors = postprocessors.OrderBy(p => p.Order).ToList();

        // Display results in the console
        Debug.Log("List of Asset Postprocessors and their GetPostprocessOrder values:");
        foreach (var (name, order) in postprocessors)
        {
            Debug.Log($"Postprocessor: {name}, Order: {order}");
        }
    }
}
