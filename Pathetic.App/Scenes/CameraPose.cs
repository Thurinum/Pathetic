using System.Numerics;

namespace Pathetic.App.Scenes;

public readonly record struct CameraPose(Vector3 Target, float Distance, float AzimuthDegrees = 0, float ElevationDegrees = 0);
