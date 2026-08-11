using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScenes : MonoBehaviour
{
	[Header("場景設定")]
    [SerializeField] private string sceneName = "Scene2";

    [Header("Player 目標座標")]
    [SerializeField] private Vector3 playerPosition = Vector3.zero;
	
	[Header("Door 開關")]
    [SerializeField] private bool door = false;

    private void OnEnable()
    {
        if (door)
            return;

        StartSceneChange();
    }
	
	private void OnTriggerEnter(Collider other)
    {
        if (!door)
            return;
        if (other.gameObject.name != "Player")
            return;
		
        StartSceneChange();
    }
	
	private void StartSceneChange()
    {
        // 不切換場景，只移動 Player
        if (string.IsNullOrEmpty(sceneName))
        {
            MovePlayer();
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != sceneName)
            return;

        MovePlayer();

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
	
	private void MovePlayer()
	{
	    GameObject player = GameObject.Find("Player");
	    if (player == null)
	        return;
		
		CharacterController controller = player.GetComponent<CharacterController>();
    	if (controller != null)
    	    controller.enabled = false;
		
		player.transform.SetPositionAndRotation(playerPosition, player.transform.rotation);
		
		if (controller != null)
        controller.enabled = true;
	}
}
