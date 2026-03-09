using System.Collections.Generic;
using Godot;

namespace SurveillanceStategodot.scripts.util;

public static class NodeExt
{
    public static List<T> FindAllChildrenOfType<T>(this Node node) where T : Node
    {
        var results = new List<T>();
        FindAllChildrenOfType(node, results);
        return results;
    }
    
    private static void FindAllChildrenOfType<T>(this Node node, List<T> results) where T : Node
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is T match)
                results.Add(match);

            FindAllChildrenOfType(child, results);
        }
    }
    
    public static T FindFirstChildOfType<T>(this Node node) where T : Node
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is T match)
                return match;

            var found = FindFirstChildOfType<T>(child);
            if (found != null)
                return found;
        }
        return null;
    }

}