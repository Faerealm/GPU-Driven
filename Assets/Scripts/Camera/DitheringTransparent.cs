using UnityEngine;

public class DitherEffect : MonoBehaviour
{
    public Transform player;  // 角色的 Transform
    public Camera mainCamera; // 主相机
    public Material material01; // 上面创建的 Dithering 材质
    public Material material02;
    public Material material03;
    //public Material material04;
    private float minDistance = 2f; // 最小距离，角色完全消失时和相机的距离
    private float maxDistance = 2.1f; // 最大距离，角色完全显示时和相机的距离

    private void Update()
    {
        // 计算相机与角色的距离
        float distance = Vector3.Distance(mainCamera.transform.position, player.position+Vector3.up*1.5f);
        // 根据距离计算 Dither 强度
        float ditherIntensity = Mathf.InverseLerp(minDistance, maxDistance, distance);  
        material01.SetFloat("_DitherIntensity", ditherIntensity);
        material02.SetFloat("_DitherIntensity", ditherIntensity);
        material03.SetFloat("_DitherIntensity", ditherIntensity);
        //material04.SetFloat("_DitherIntensity", ditherIntensity);
        //Debug.Log(distance + "  " + ditherIntensity);

        //float alpha = Mathf.InverseLerp(minDistance, maxDistance, distance);
        //Debug.Log(distance + "  " + ditherIntensity+" "+alpha);
        //material01.SetFloat("_Cutoff", alpha);
        //material02.SetFloat("_Cutoff", alpha);
        //material03.SetFloat("_Cutoff", alpha);
        //material04.SetFloat("_Cutoff", alpha);
    }
}
