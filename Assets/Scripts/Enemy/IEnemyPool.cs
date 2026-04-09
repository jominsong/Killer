/// <summary>
/// 적 풀/스포너가 구현해야 할 인터페이스.
/// EnemyFSM이 구체 타입에 의존하지 않도록 분리.
/// </summary>
public interface IEnemyPool
{
    void DeactivateEnemy(UnityEngine.GameObject enemy);
}