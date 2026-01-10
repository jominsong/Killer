using UnityEngine;

public class MovementTransform : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 0.0f;
    [SerializeField]
    private Vector3 movedirection = Vector3.zero;

    ///<summary>
    ///이동 방향이 설정되면 알아서 이동하도록 함
    /// </summary>
    private void Update()
    {
        transform.position += movedirection * moveSpeed * Time.deltaTime;
    }

    ///<summary>
    /// 외부에서 매개변수로 이동 방향을 설정
    /// </summary>
    public void MoveTo(Vector3 direction)
    {
        movedirection = direction;
    }
}
