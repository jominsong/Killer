using UnityEngine;

public class RotateToMouse : MonoBehaviour
{
    [SerializeField]
    private float rotCamXAxisSpeed = 5f;  // 카메라 x축 회전속도
    [SerializeField]
    private float rotCamYAxisSpeed = 3;  // 카메라 y축 회전속도

    private float limitMinX = -80;  // 카메라 x축 회전 범위(최소)
    private float limitMaxX = 50;   // 카메라 x축 회전 범위(최대)

    private float eulerAngleX;
    private float eulerAngleY;
 

    public void UpdateRotate(float mouseX,float mouseY)
    {
        eulerAngleY += mouseX * rotCamYAxisSpeed;  // 마우스 좌/우 이동으로 카메라 y축 회전
        eulerAngleX -= mouseY * rotCamXAxisSpeed;  // 마우스 상/하 이동으로 카메라 x축 회전

        // 카메라 x축 회전의 경우 회전 범위를 설정
        eulerAngleX = Mathf.Clamp(eulerAngleX, limitMinX, limitMaxX);

        transform.localRotation = Quaternion.Euler(eulerAngleX, eulerAngleY, 0f);
    }

    public void AddRecoil(float x, float y)
    {
        eulerAngleX += x;
        eulerAngleX = Mathf.Clamp(eulerAngleX, limitMinX, limitMaxX);
        eulerAngleY += y;
    }
    

    // 외부 참조용
    public float EulerAngleX => eulerAngleX;
    public float EulerAngleY => eulerAngleY;
}
