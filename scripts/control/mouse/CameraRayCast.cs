using Godot;
using Godot.Collections;

namespace SurveillanceStategodot.scripts.control.mouse;

public static class CameraRayCast
{
    public static Dictionary ShootRayFromCamera(this Node self, Camera3D camera, uint collisionMask = 1)
    {
        var raycast_length = 1000;
        var space_state = self.GetViewport().World3D.DirectSpaceState;
        var mouse_position = self.GetViewport().GetMousePosition();
            
        var args = new PhysicsRayQueryParameters3D();
        args.From = camera.ProjectRayOrigin(mouse_position);
        args.To = args.From + camera.ProjectRayNormal(mouse_position) * raycast_length;
        args.CollisionMask = collisionMask;

        var result =  space_state.IntersectRay(args);
        return result;
    }
}