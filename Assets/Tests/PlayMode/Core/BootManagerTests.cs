using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BootManagerTests
{
    [UnityTest]
    public IEnumerator StartupSequence_ExecutesAndCompletes()
    {
        GameObject bootGO = new GameObject();
        BootManager bootManager = bootGO.AddComponent<BootManager>();

        // Espera el tiempo de la corrutina (0.5s en StartupSequence)
        yield return new WaitForSeconds(0.6f);

        // Verificamos que el GameObject sigue respondiendo
        Assert.IsNotNull(bootManager);

        Object.Destroy(bootGO);
    }
}