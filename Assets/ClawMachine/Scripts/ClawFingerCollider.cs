using UnityEngine;

public class ClawFingerCollider : MonoBehaviour
{
    private Collider fingerCollider;
    //private int contactCount = 0;
    void Start()
    {
        fingerCollider = GetComponent<Collider>();  // 获取碰撞体引用
    }
    public bool IsContactingTarget(GameObject target)
    {
        // 获取当前碰撞体的所有接触
        Collider collider = GetComponent<Collider>();
        if (collider == null) return false;

        // 使用 Physics.ComputePenetration 或 Physics.OverlapBox 等方法检测碰撞
        Bounds bounds = collider.bounds;
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider == null) return false;

        return bounds.Intersects(targetCollider.bounds);
    }

}

