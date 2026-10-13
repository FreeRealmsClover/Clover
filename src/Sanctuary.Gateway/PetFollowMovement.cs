using System;
using System.Numerics;

namespace Sanctuary.Gateway;

internal static class PetFollowMovement
{
    public readonly record struct Step(Vector3 Position, Quaternion Rotation, byte State, float Speed);

    public static Vector3 GetTarget(Vector4 ownerPosition, Quaternion ownerRotation)
    {
        var forward = Vector3.Transform(Vector3.UnitZ, ownerRotation);
        forward.Y = 0;
        forward = forward.LengthSquared() > 0.001f ? Vector3.Normalize(forward) : Vector3.UnitZ;
        var side = new Vector3(forward.Z, 0, -forward.X);
        var target = new Vector3(ownerPosition.X, ownerPosition.Y - 0.35f, ownerPosition.Z);
        return target - forward * 1.8f + side * 0.9f;
    }

    public static Step Advance(Vector3 position, Quaternion rotation, Vector3 target, float deltaSeconds)
    {
        var offset = target - position;
        var distance = offset.Length();
        if (distance <= 0.55f)
            return new Step(position, rotation, 0, 0);
        if (distance > 35f)
            return new Step(target, rotation, 0, 0);

        var direction = offset / distance;
        var speed = distance > 9.5f ? 7f : 3f;
        var distanceToMove = MathF.Min(speed * Math.Clamp(deltaSeconds, 0f, 0.25f), distance);
        var newRotation = Quaternion.CreateFromYawPitchRoll(MathF.Atan2(direction.X, direction.Z), 0, 0);
        var arrived = distanceToMove >= distance - 0.55f;
        return new Step(position + direction * distanceToMove, newRotation,
            arrived ? (byte)0 : speed > 3f ? (byte)2 : (byte)1, arrived ? 0 : speed);
    }
}
