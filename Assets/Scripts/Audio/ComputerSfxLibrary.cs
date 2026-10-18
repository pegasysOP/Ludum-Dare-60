using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Audio/Computer Sfx Library", fileName = "ComputerSfxLibrary")]
public class ComputerSfxLibrary : ScriptableObject
{
    private const string ResourcePath = "ComputerSfxLibrary";

    [Header("OS")]
    public SfxBank click = new SfxBank();
    [FormerlySerializedAs("startup")] public SfxBank wake = new SfxBank();
    public SfxBank shutdown = new SfxBank();
    public SfxBank error = new SfxBank();
    public SfxBank keyPress = new SfxBank();
    public SfxBank submit = new SfxBank();

    [Header("Misc")]
    public SfxBank fan = new SfxBank();
    public SfxBank boot = new SfxBank();
    public SfxBank cdInsert = new SfxBank();

    private void OnEnable()
    {
        // Every phone sound goes to the Phone mixer group, so it stays clear while the world is muffled.
        foreach (KeyValuePair<string, SfxBank> bank in AllBanks())
            bank.Value.routeToPhone = true;
    }

    public static ComputerSfxLibrary Load()
    {
        ComputerSfxLibrary library = Resources.Load<ComputerSfxLibrary>(ResourcePath);
        if (library == null)
            Debug.LogError($"ComputerSfxLibrary: no asset found at Resources/{ResourcePath}.");
        return library;
    }

    public IEnumerable<KeyValuePair<string, SfxBank>> AllBanks()
    {
        yield return new KeyValuePair<string, SfxBank>("Click", click);
        yield return new KeyValuePair<string, SfxBank>("Wake", wake);
        yield return new KeyValuePair<string, SfxBank>("shutdown", shutdown);
        yield return new KeyValuePair<string, SfxBank>("Error", error);
        yield return new KeyValuePair<string, SfxBank>("Key Press", keyPress);
        yield return new KeyValuePair<string, SfxBank>("Submit", submit);
        yield return new KeyValuePair<string, SfxBank>("Fan", fan);
        yield return new KeyValuePair<string, SfxBank>("Boot", boot);
        yield return new KeyValuePair<string, SfxBank>("CD Insert", cdInsert);
    }

}
