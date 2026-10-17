using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ComputerInteractable : Interactable
{
    [SerializeField] private GameObject blackScreenPanel;
    [SerializeField] private Canvas computerCanvas;
    [SerializeField] private Canvas homeScreenCanvas;

    [SerializeField] private Button loginButton;
    [SerializeField] private Button turnOnButton;
    [SerializeField] private Button turnOnButton2;


    [SerializeField] private TMP_InputField passwordInputField;

    //FIXME: Replace this with a proper way to exit the computer
    public Keyboard keyboard;

    private string password = "password"; //TODO: Change this to be based on the clue    

    public void Start()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        turnOnButton.onClick.AddListener(OnTurnOnButtonClicked);
        turnOnButton2.onClick.AddListener(OnTurnOnButtonClicked);

        keyboard = Keyboard.current;
    }

    public void Update()
    {
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            computerCanvas.gameObject.SetActive(false);
            homeScreenCanvas.gameObject.SetActive(false);
            passwordInputField.text = "";
        }
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
        if (passwordInputField.text == password)
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

    private void OnTurnOnButtonClicked()
    {
        if (blackScreenPanel.gameObject.activeSelf)
        {
            blackScreenPanel.gameObject.SetActive(false);
            //TODO: Also play audio at this point to simulate the computer turning on
        }
        else
        {
            computerCanvas.gameObject.SetActive(false);
            homeScreenCanvas.gameObject.SetActive(false);
            blackScreenPanel.gameObject.SetActive(true);
        }
        
    }
}