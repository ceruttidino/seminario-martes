using TMPro;
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
                consoleText.GetComponent<TextMeshProUGUI>().text = "Please respect Capital Letters";
                break;
            case "Help": //help le mustra al usuario todos los commandos
                if (command.Length > 1)
                {
                    switch (command[1])
                    {
                        case "Help":
                            consoleText.GetComponent<TextMeshProUGUI>().text = "Shows all available commands when empty\nExplains what a commands does when placed behind it";
                            break;
                        case "GiveScrap":
                            consoleText.GetComponent<TextMeshProUGUI>().text = "Gives scrap to the player";
                            break;
                        case "GivePick":
                            consoleText.GetComponent<TextMeshProUGUI>().text = "Gives the specified ammount fo lockpicks to the player";
                            break;
                        case "GiveHealth":
                            consoleText.GetComponent<TextMeshProUGUI>().text = "heals the player the requested ammount ";
                            break;
                        case "AddHearts":
                            consoleText.GetComponent<TextMeshProUGUI>().text = "Increases the player's max health";
                            break;
                        case "SkipLevel":
                            consoleText.GetComponent<TextMeshProUGUI>().text = "Makes the player win, thus allowing to skip the level";
                            break;
                        default:
                            consoleText.GetComponent<TextMeshProUGUI>().text = "Command not recognized, Please remember to respect Capital Letters";
                            break;
                    }
                }
                else { consoleText.GetComponent<TextMeshProUGUI>().text = "Available commands: WARNING ALL COMMANDS ARE CASE SENSITIVE\nHelp [command] \nGiveScrap [amount]\nGivePick [amount]\nGiveHealth [amount]\nAddHearts [amount]\nSkipLevel"; }
                    break;
            case "GiveScrap"://entrega scrap (dinero) al jugador
                if (command.Length > 1)
                {
                    if (int.TryParse(command[1], out int Amount))
                    {
                        player.GetComponent<PlayerScrap>().AddScrap(Amount);
                        consoleText.GetComponent<TextMeshProUGUI>().text = "Adding " + Amount + " Scrap";
                    }                
                    else { consoleText.GetComponent<TextMeshProUGUI>().text = "Invalid amount specified"; }
                }
                else { consoleText.GetComponent<TextMeshProUGUI>().text = "no specified ammount"; }
        break; 

            case "GivePick": //entrega Lockpicks (llaves) al jugador
                if (command.Length > 1)
                {
                    if (int.TryParse(command[1], out int Amount))
                    {
                        player.GetComponent<PlayerKeys>().AddKeys(Amount);
                        consoleText.GetComponent<TextMeshProUGUI>().text = "Adding " + Amount + " Lockpicks";
                    }
                    else { consoleText.GetComponent<TextMeshProUGUI>().text = "Invalid amount specified"; }
                }
                else { consoleText.GetComponent<TextMeshProUGUI>().text = "no specified ammount"; }
                break;
            case "GiveHealth": //cura al jugador
                if (command.Length > 1)
                {
                    if (int.TryParse(command[1], out int Amount))
                    {
                        player.GetComponent<PlayerHealth>().PlayerHeal(Amount);
                        Debug.Log("Healing " + Amount + " hearts of healt");
                        consoleText.GetComponent<TextMeshProUGUI>().text = "Healing " + Amount + " hearts of healt";
                    }
                    else { consoleText.GetComponent<TextMeshProUGUI>().text = "Invalid amount specified"; }
                }
                else { consoleText.GetComponent<TextMeshProUGUI>().text = "no specified ammount"; }
                break;
            case "AddHearts": //aumenta la salud maxima del jugador
                if (command.Length > 1)
                {
                    if (int.TryParse(command[1], out int Amount))
                    {
                        if (Amount <= 3)
                        {
                            player.GetComponent<PlayerHealth>().PlayerAddHeart(Amount);
                            consoleText.GetComponent<TextMeshProUGUI>().text = "Adding " + Amount + " hearts to player";
                        }
                        else
                        {
                            player.GetComponent<PlayerHealth>().PlayerAddHeart(3);
                            consoleText.GetComponent<TextMeshProUGUI>().text = "Amount exceeds Health Cap, adding max ammount of hearts";
                        }
                    }
                    else { consoleText.GetComponent<TextMeshProUGUI>().text = "Invalid amount specified"; }
                }
                else { consoleText.GetComponent<TextMeshProUGUI>().text = "no specified ammount"; }
                break;
            case "SkipLevel": //saltea el nivel
                Debug.Log("Skipping level");

                VictoryManager victoryManager = FindFirstObjectByType<VictoryManager>();
                victoryManager.ContinueRun();

                consoleText.GetComponent<TextMeshProUGUI>().text = "Skiping to next level";
                break;

            default:
                consoleText.GetComponent<TextMeshProUGUI>().text = "Command not recognized\nPlease remember to respect Capital Letters\nUse 'HELP' to check all available commands";
                break;
        }
    }
}
