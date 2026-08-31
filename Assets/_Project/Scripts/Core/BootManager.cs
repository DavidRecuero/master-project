using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootManager : MonoBehaviour
{
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

        // TODO: To load the Main Menu scene once it exists
        //SceneManager.LoadScene(1);
    }
}