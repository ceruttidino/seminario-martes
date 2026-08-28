using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class Cheats : MonoBehaviour
{

    private bool cheating = false;
    [SerializeField] private GameObject cheatPanel;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject consoleText;

    private string[] command;

    private string cheatString;


    public void OnCheat(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (!cheating)
            Cheating();
        else
            StopCheating();
    }

    private void Cheating()
    {
        cheatPanel.SetActive(true);
        Time.timeScale = 0f;
        cheating = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void StopCheating()
    {
        cheatPanel.SetActive(false);
        Time.timeScale = 1f;
        cheating = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ReadCheat(string s)
    {
        cheatString = s;
        Debug.Log(cheatString);
        command = cheatString.Split(' ');

        switch (command[0])
        {
            case "help":
                Debug.Log("Available commands: givescrap [amount], givepick [amount], givehealth, skiplevel");
                consoleText.GetComponent<TextMeshProUGUI>().text = "Available commands: givescrap [amount], givepick [amount], givehealth, skiplevel";
                break;
            case "givescrap":
                if (command.Length >= 1)
                {
                    if (int.TryParse(command[1], out int scrapAmount))
                    {
                        player.GetComponent<PlayerScrap>().AddScrap(scrapAmount);
                    }
                    else { Debug.Log("Invalid amount specified"); }
                }
                else { Debug.Log("no specified ammount"); }
                break; 

            case "givepick":
                if (command.Length >= 1)
                {
                    if (int.TryParse(command[1], out int keyAmount))
                    {
                        player.GetComponent<PlayerKeys>().AddKeys(keyAmount);
                    }
                    else { Debug.Log("Invalid amount specified"); }
                }
                else { Debug.Log("no specified ammount"); }
                break;
            case "givehealth":
                Debug.Log("Cheat activated: health");
                break;
            case "skiplevel":
                Debug.Log("Skipping level");
                break;

        }
    }
}
