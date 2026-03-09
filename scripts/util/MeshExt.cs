using Godot;

namespace SurveillanceStategodot.scripts.util;

public static class MeshExt
{
    
    public static void SetColor(this MeshInstance3D meshInstance, Color color)
    {
        var material = (Material)meshInstance.GetActiveMaterial(0).Duplicate();
        material.Set("albedo_color", color);
        meshInstance.SetSurfaceOverrideMaterial(0, material);
    }
    
}