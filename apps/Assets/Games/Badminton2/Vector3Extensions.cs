using UnityEngine;

public static class Vector3Extensions
{
    public static Vector3 Copy(this Vector3 self)
    {
        return new Vector3(self.x, self.y, self.z);
    }
}
