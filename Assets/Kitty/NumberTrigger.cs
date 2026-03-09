using UnityEngine;
using TMPro;

public class NumberTrigger : MonoBehaviour
{
    public GameObject inputUI;
    public TMP_InputField numberInput;
    public PlayerController playerMovement; 

    private bool playerInZone = false;
    private bool uiActive = false;

    private int correctNumber = 12;

    void Start()
    {
        inputUI.SetActive(false);
    }

    void Update()
    {
        if (playerInZone && !uiActive && Input.GetKeyDown(KeyCode.E))
        {
            OpenUI();
        }
        // enter submits answer
        if (uiActive && Input.GetKeyDown(KeyCode.Return))
        {
            SubmitNumber();
        }

        // escp closes UI
        if (uiActive && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseUI();
        }
    }

    void OpenUI()
    {
        uiActive = true;
        inputUI.SetActive(true);

        if (playerMovement != null)
            playerMovement.enabled = false;

        numberInput.text = "";
        numberInput.ActivateInputField();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SubmitNumber()
    {
        int enteredNumber;

        if (int.TryParse(numberInput.text, out enteredNumber))
        {
            if (enteredNumber == correctNumber)
            {
                AudioManager.instance.PlaySFX(AudioManager.instance.signRight);
            }
            else
            {
                AudioManager.instance.PlaySFX(AudioManager.instance.signWrong);
            }
        }
        else
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.signWrong);
        }

        CloseUI();
    }

    void CloseUI()
    {
        uiActive = false;
        inputUI.SetActive(false);

        if (playerMovement != null)
            playerMovement.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
        }
    }
}