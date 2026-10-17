using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ComputerInteractable : Interactable
{
    [SerializeField] private Canvas computerCanvas;
    [SerializeField] private Canvas homeScreenCanvas;

    [SerializeField] private Button loginButton;

    [SerializeField] private TMP_InputField passwordInputField;

    private string password = "password"; //TODO: Change this to be based on the clue    

    public void Start()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        //Enable the computer Canvas
        computerCanvas.gameObject.SetActive(true);

        //Show invoicing PDF, Timesheet with Annual Leave and other clutter (Optionally, allow drag and drop of UI elements) 
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        //TODO: Should we do anything on pointer up? 
    }

    private void OnLoginButtonClicked()
    { 
        if(passwordInputField.text == password)
        {
            //TODO: If correct, change to homescreen canvas
            computerCanvas.gameObject.SetActive(false);
            homeScreenCanvas.gameObject.SetActive(true);
        }
        else
        {
            //If incorrect, play an error sound
            Debug.Log("Incorrect password");
        }
    }
}
