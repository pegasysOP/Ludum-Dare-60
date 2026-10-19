using UnityEngine;

// counts as found evidence the first time this shows, for windows and views rather than things in the room
public class EvidenceSource : MonoBehaviour
{
    public Evidence evidence;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.FoundEvidence(evidence);
    }
}
