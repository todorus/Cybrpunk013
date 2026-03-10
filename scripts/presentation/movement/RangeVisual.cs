using Godot;

namespace SurveillanceStategodot.scripts.presentation.movement;

public partial class RangeVisual : MeshInstance3D
{
    [Export] private Color _fillColor = new(0.2f, 0.8f, 1f, 0.18f);
    [Export] private int _segments = 64;

    private StandardMaterial3D _material;

    public override void _Ready()
    {
        _material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            AlbedoColor = _fillColor
        };

        MaterialOverride = _material;
    }

    public void SetRange(float range)
    {
        if (range <= 0f)
        {
            Visible = false;
            Mesh = null;
            return;
        }
        
        Mesh = BuildDiscMesh(range, Mathf.Max(3, _segments));
    }

    private static ArrayMesh BuildDiscMesh(float radius, int segments)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        Vector3 center = Vector3.Zero;
        Vector3 normal = Vector3.Up;

        for (int i = 0; i < segments; i++)
        {
            float a0 = Mathf.Tau * i / segments;
            float a1 = Mathf.Tau * (i + 1) / segments;

            Vector3 p0 = new(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
            Vector3 p1 = new(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);

            st.SetNormal(normal);
            st.AddVertex(center);

            st.SetNormal(normal);
            st.AddVertex(p0);

            st.SetNormal(normal);
            st.AddVertex(p1);
        }

        return st.Commit();
    }
}