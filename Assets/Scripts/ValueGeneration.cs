using UnityEngine;

public class ValueGeneration : MonoBehaviour
{
    public float PerlinNoise3D(Vector3 point, float scale)
    {
        float xy = Mathf.PerlinNoise(point.x / scale, point.y / scale);
        float xz = Mathf.PerlinNoise(point.x / scale, point.z / scale);
        float yz = Mathf.PerlinNoise(point.y / scale, point.z / scale);
        float yx = Mathf.PerlinNoise(point.y / scale, point.x / scale);
        float zx = Mathf.PerlinNoise(point.z / scale, point.x / scale);
        float zy = Mathf.PerlinNoise(point.z / scale, point.y / scale);

        return (xy + xz + yz + yx + zx + zy) / 6;
    }

    public float SphereFunc(Vector3 point, float size){
        return (point.x - size / 2) * (point.x - size / 2) + (point.y - size / 2) * (point.y - size / 2) + (point.z - size / 2) * (point.z - size / 2);
    }
}
