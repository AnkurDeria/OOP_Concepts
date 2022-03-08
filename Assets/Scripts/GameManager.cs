using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

#region Enums, which would normally be in a scriptable object but for the sake of example are kept here
public enum GameState
{
    STOP,
    PLAY
}
public enum EnemyBodyType
{
    CUBE = 1,
    SPHERE = 2,
    CYLINDER = 3
}

#endregion

////--------------------------------------------------------------------------------------------------------------------

#region  Game Manager
public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance;

    #region Gameplay timing values, animation timings, which are usually kept in a scriptable object

    [Tooltip("Color gradient used to indicate full health and loss in health.")]
    public Gradient ColorGradient;

    [Tooltip("Time in seconds for the spawn animation of enemies.")]
    public float enemySpawnAnimationTimeInSeconds;

    [Tooltip("Time in seconds for the death animation of type 1 enemies.")]
    public float enemyType1KillAnimationTimeInSeconds;

    [Tooltip("Time in seconds for the death animation of type 2 enemies.")]
    public float enemyType2KillAnimationTimeInSeconds;

    [Tooltip("Slider to increase or decrease time between each enemy spawn.")]
    [Range(0f, 5f)]
    [SerializeField] private float spawnWaitTime = 1f;

    [Tooltip("Slider to increase or decrease speed of spawning.")]
    [Range(0f, 5f)]
    [SerializeField] private float spawnSpeed = 1f;

    [Tooltip("Slider to increase or decrease grid size.")]
    [Range(0, 50)]
    [SerializeField] private int gridSize;

    #endregion

    //--------------------------------------------------------------------------------------------------------------------

    // private variables
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private List<EnemyBaseClass> enemyPool;
    [SerializeField] private int[] enemyPoolSize;
    [SerializeField] private Transform enemyParent;

    private bool[,] grid;
    private List<(int, int)> indicesOfAvailableSpots;
    private List<int> indicesOfAvailableEnemies;

    private GameState gameState;
    private RaycastHit hit;
    private Camera mainCam;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        gameState = GameState.PLAY;
        mainCam = Camera.main;

        InitializeLists();
        CreateEnemyPool();
        StartCoroutine(StartEnemySpawn());
    }


    /// <summary>
    /// Set list variables
    /// </summary>
    private void InitializeLists()
    {
        indicesOfAvailableSpots = new List<(int, int)>();
        indicesOfAvailableEnemies = new List<int>();
        grid = new bool[gridSize, gridSize];
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                grid[i, j] = false;
            }
        }

    }


    /// <summary>
    /// Create an enemy pool before spawning enemies
    /// </summary>
    private void CreateEnemyPool()
    {
        // Loop through body types of enemies
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            // Loop through pool size of enemy type (i)
            for (int j = 0; j < enemyPoolSize[i]; j++)
            {
                // 50% chance of the enemy having either behaviour type
                if (Random.value > 0.5f)
                {
                    enemyPool.Add(Instantiate(enemyPrefabs[i], enemyParent).AddComponent<EnemyBehaviourType1>().GetComponent<EnemyBaseClass>());
                }
                else
                {
                    enemyPool.Add(Instantiate(enemyPrefabs[i], enemyParent).AddComponent<EnemyBehaviourType2>().GetComponent<EnemyBaseClass>());
                }

                // Setting the body type of the created enemy 
                enemyPool[i + j].SetEnemyType((EnemyBodyType)(i + 1));
            }
        }
    }


    /// <summary>
    /// Coroutine to start enemy spawning. Will continue spawning till gameState is changed or all enemies from enemy pool are on screen
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartEnemySpawn()
    {
        while (gameState == GameState.PLAY)
        {
            yield return new WaitForSeconds(spawnWaitTime * spawnSpeed);
            var spotInGrid = GetRandomFreeSpot();
            if (spotInGrid == (-1, -1))
            {
                continue;
            }
            grid[spotInGrid.Item1, spotInGrid.Item2] = true;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("Free spot selected = " + spotInGrid);
#endif

            int enemyIndex = GetEnemyToSpawn();
            if (enemyIndex == -1)
            {
                continue;
            }
            var enemy = enemyPool[enemyIndex];
            Debug.Log("Enemy " + enemyIndex + " is spawning");
            enemy.gameObject.SetActive(true);
            enemy.transform.position = new Vector3(spotInGrid.Item1 - (gridSize * 0.5f) + enemy.halfSize.x, -enemy.halfSize.y, spotInGrid.Item2 - (gridSize * 0.5f) + enemy.halfSize.z);
            enemy.transform.DOMoveY(enemy.halfSize.y, enemySpawnAnimationTimeInSeconds).SetEase(Ease.OutQuad);
            Debug.Log("Next enemy");

        }
    }

    /// <summary>
    /// Select an enemy from the enemy pool that is not on screen
    /// </summary>
    /// <returns> Index of an enemy that can be spawned </returns>
    private int GetEnemyToSpawn()
    {
        // stash all the indices of the unspawned enemies in the list
        indicesOfAvailableEnemies.Clear();
        for (int i = 0; i < enemyPool.Count; i++)
        {
            if (!enemyPool[i].gameObject.activeSelf)
            {
                indicesOfAvailableEnemies.Add(i);
            }
        }

        if (indicesOfAvailableEnemies.Count < 1)
        {
            return -1;
        }

        // return a random enemy index from that list
        return indicesOfAvailableEnemies[Random.Range(0, indicesOfAvailableEnemies.Count)];
    }


    /// <summary>
    /// Function to find a spot on the grid where no enemy is present
    /// </summary>
    /// <returns> x and y index of a random free spot </returns>
    private (int, int) GetRandomFreeSpot()
    {
        // stash all the indices of the free spots in the list
        indicesOfAvailableSpots.Clear();
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (!grid[i, j])
                {
                    indicesOfAvailableSpots.Add((i, j));
                }
            }
        }

        if (indicesOfAvailableSpots.Count < 1)
        {
            return (-1, -1);
        }

        // return a random spot index from that list
        return indicesOfAvailableSpots[Random.Range(0, indicesOfAvailableSpots.Count)];
    }


    /// <summary>
    /// User input. Left click for light hit and right click for heavy hit
    /// </summary>
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out hit))
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("Light Hit");
#endif
                hit.transform.GetComponent<IDamageable>().GotHit(1);
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out hit))
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("Heavy Hit");
#endif
                hit.transform.GetComponent<IDamageable>().GotHit(2, Random.value);
            }
        }
    }
}

#endregion

////--------------------------------------------------------------------------------------------------------------------

#region interfaces

public interface IDamageable
{
    void GotHit(int _damage);

    void GotHit(int _damage, float _missChance);
}

public interface IChangeableColour
{
    void ChangeColor();
}

#endregion

////--------------------------------------------------------------------------------------------------------------------