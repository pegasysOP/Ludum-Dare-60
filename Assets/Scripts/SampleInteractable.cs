using UnityEngine;

public class SampleInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log("Interacted with object");
    }

    public bool IsInteractable()
    {
        return true;
    }

}
