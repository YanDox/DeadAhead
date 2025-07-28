
using UnityEngine;
using UnityEngine.SceneManagement;

public class pauseUI : MonoBehaviour
{
    public GameObject playerUi;
	public GameObject PauseUi;
	public bool ispause;
	// Start is called before the first frame update
	void Start()
    {
		backtogame();

	}

    // Update is called once per frame
    void Update()
    {
		if(Input .GetKeyUp(KeyCode.Escape))
		{
			if (ispause)
			{
				backtogame();
				
			}
			else
			{
				Pause();
			}
		}
        
    }
	public void backtogame()
	{
		playerUi.SetActive(true);
		Time.timeScale = 1;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		PauseUi.SetActive(false);
	}
	public void Pause()
	{
		playerUi.SetActive(false);
		Time.timeScale = 0;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		PauseUi.SetActive(true);
	}

	public void Exit()
	{
		SceneManager.LoadScene(0);
	}
}
