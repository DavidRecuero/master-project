using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootManager : MonoBehaviour
{
    private ISceneLoader _sceneLoader;

    public void Initialize(ISceneLoader sceneLoader)
    {
        _sceneLoader = sceneLoader;
    }

    private void Awake()
    {
        _sceneLoader ??= new UnitySceneLoader();
    }

    private void Start()
    {
        // TODO: here we'll load UGS, login, privacity popup...
        StartCoroutine(StartupSequence());
    }

    private IEnumerator StartupSequence()
    {
        Debug.Log("[BOOT] Init services...");

        // TODO: Baas Conection simulator
        yield return new WaitForSeconds(0.5f);

        Debug.Log("[BOOT] Everything ready, loading main menu...");

        // Loads the Main Menu scene 
        _sceneLoader.LoadScene(1);
    }
}