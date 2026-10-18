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

    private ComputerSfxLibrary sounds;

    [SerializeField] private TMP_InputField passwordInputField;

    //FIXME: Replace this with a proper way to exit the computer
    public Keyboard keyboard;

    private string password = "password"; //TODO: Change this to be based on the clue    
    public void Start()
    {
        sounds = ComputerSfxLibrary.Load();
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        turnOnButton.onClick.AddListener(OnTurnOnButtonClicked);
        turnOnButton2.onClick.AddListener(OnTurnOnButtonClicked);
        passwordInputField.onValueChanged.AddListener(OnPasswordTyped);

        keyboard = Keyboard.current;
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

    private void OnPasswordTyped(string text)
    {
        // only real typing, not the field being cleared on shutdown
        if (passwordInputField.isFocused)
            sounds.keyPress.Play();
    }

    private void OnLoginButtonClicked()
    {
        sounds.submit.Play();
        if (passwordInputField.text == password)
        {
            //TODO: If correct, change to homescreen canvas
            computerCanvas.gameObject.SetActive(false);
            homeScreenCanvas.gameObject.SetActive(true);
            sounds.startup.Play();
        }
        else
        {
            //If incorrect, play an error sound
            Debug.Log("Incorrect password");
            sounds.error.Play();
        }
    }

    private void OnTurnOnButtonClicked()
    {
        if (blackScreenPanel.gameObject.activeSelf)
        {
            blackScreenPanel.gameObject.SetActive(false);
            //TODO: This shouldn't be one shot but play on loop. 
            sounds.fan.Play();
        }
        else
        {
            computerCanvas.gameObject.SetActive(false);
            homeScreenCanvas.gameObject.SetActive(false);
            blackScreenPanel.gameObject.SetActive(true);

            passwordInputField.text = "";
            sounds.shutdown.Play();
        }
        
    }
}