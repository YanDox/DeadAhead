using UnityEngine;
using UnityEngine.SceneManagement;

public class SC_EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public SC_DamageReceiver player;
    public Texture crosshairTexture;
    public float spawnInterval = 2; //Spawn new enemy each n seconds
    public int enemiesPerWave = 5; //How many enemies per wave
    public Transform[] spawnPoints;

    float nextSpawnTime = 0;
    int waveNumber = 1;
    bool waitingForWave = true;
    float newWaveTimer = 0;
    int enemiesToEliminate;
    //How many enemies we already eliminated in the current wave
    int enemiesEliminated = 0;
    int totalEnemiesSpawned = 0;

    // Start is called before the first frame update
    void Start()
    {
        //Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //Wait 10 seconds for new wave to start
        newWaveTimer = 10;
        waitingForWave = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (waitingForWave)
        {
            if(newWaveTimer >= 0)
            {
                newWaveTimer -= Time.deltaTime;
            }
            else
            {
                //Initialize new wave
                enemiesToEliminate = waveNumber * enemiesPerWave;
                enemiesEliminated = 0;
                totalEnemiesSpawned = 0;
                waitingForWave = false;
            }
        }
        else
        {
            if(Time.time > nextSpawnTime)
            {
                nextSpawnTime = Time.time + spawnInterval;

                //Spawn enemy 
                if(totalEnemiesSpawned < enemiesToEliminate)
                {
                    Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length - 1)];

                    GameObject enemy = Instantiate(enemyPrefab, randomPoint.position, Quaternion.identity);
                    SC_NPCEnemy npc = enemy.GetComponent<SC_NPCEnemy>();
                    npc.playerTransform = player.transform;
                    npc.es = this;
                    totalEnemiesSpawned++;
                }
            }
        }

        if (player.playerHP <= 0)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Scene scene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(scene.name);
            }
        }
    }

    
    

    public void EnemyEliminated(SC_NPCEnemy enemy)
    {
        enemiesEliminated++;

        if(enemiesToEliminate - enemiesEliminated <= 0)
        {
            //Start next wave
            newWaveTimer = 10;
            waitingForWave = true;
            waveNumber++;
        }
    }
}