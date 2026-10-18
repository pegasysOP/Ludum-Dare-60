using UnityEngine;

// one asset per person, everything that shows a name, job title or headshot reads from here
[CreateAssetMenu(menuName = "Game/Character", fileName = "Character")]
public class Character : ScriptableObject
{
    public string characterName;
    public string jobTitle;
    public Sprite headshot;

    [Tooltip("Dialogue blip pitch in semitones from the shared voice clip (Bb4). Negative = deeper.")]
    [Range(-12f, 12f)] public float voiceSemitones;
}
