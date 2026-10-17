using System.Collections;
using TMPro;
using UnityEngine;

// notes just live in the field, so they last until the scene unloads
public class NotesApp : MonoBehaviour, IAppContent
{
    public TMP_InputField inputField;

    private void Awake()
    {
        // tab is a pda close key, so keep it out of the text
        inputField.onValidateInput = (text, index, character) => character == '\t' ? '\0' : character;
    }

    public bool Back()
    {
        return false;
    }

    public void OnAppOpened()
    {
        StartCoroutine(FocusAtEnd());
    }

    public void OnAppClosed()
    {
        inputField.DeactivateInputField();
    }

    private IEnumerator FocusAtEnd()
    {
        inputField.ActivateInputField();

        // activation happens late in the frame and places the caret itself
        yield return null;
        inputField.MoveTextEnd(false);
    }
}
