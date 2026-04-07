using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class AntMoveState : IEnemyState
{
    private Transform player;
    private EnemyMovement movement;
    private Transform enemyTransform;
    private MonoBehaviour coroutineRunner;
    private Vector2 moveDirection;

    private float moveSpeed = 3f;
    private float tileSize = 1f;

    private int minTiles = 5;
    private int maxTiles = 8;
    private float waitTime = 2f;

    private Coroutine movementCoroutine;

    public AntMoveState(Transform player, EnemyMovement movement, Transform transform, MonoBehaviour runner)
    {
        this.player = player;
        this.movement = movement;
        this.enemyTransform = transform;
        this.coroutineRunner = runner;
    }

    public void Enter()
    {
        movementCoroutine = coroutineRunner.StartCoroutine(MovementLoop());
    }

    public void Exit()
    {
        if (movementCoroutine != null)
        {
            coroutineRunner.StopCoroutine(movementCoroutine);
        }

        movement.Move(Vector2.zero);
    }

    public void Tick()
    {
    }

    private IEnumerator MovementLoop()
    {
        while (true)
        {
            int tilesToMove = Random.Range(minTiles, maxTiles + 1);
            float distance = tilesToMove * tileSize;

            Vector2 startPos = enemyTransform.position;

            if (player != null)
            {
                moveDirection = (player.position - enemyTransform.position).normalized;
            }
            else
            {
                moveDirection = GetRandomDirection();
            }

            while (Vector2.Distance(startPos, enemyTransform.position) < distance)
            {
                movement.Move(moveDirection);
                yield return null;
            }

            movement.Move(Vector2.zero);

            yield return new WaitForSeconds(waitTime);
        }
    }

    private Vector2 GetRandomDirection()
    {
        float angle = Random.Range(0f, 360f);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
    }

    public void OnWallHit(Vector2 normal)
    {
        moveDirection = Vector2.Reflect(moveDirection, normal).normalized;
    }
}
