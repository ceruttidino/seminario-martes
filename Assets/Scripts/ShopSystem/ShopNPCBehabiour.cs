using UnityEngine;

public class ShopNPCBehabiour : MonoBehaviour
{
    [SerializeField] string[] fillerDialogue;
    [SerializeField] string[] greetingDialogue;
    [SerializeField] string[] purchaseDialogue;
    [SerializeField] string[] failedPurchaseDialogue;
    [SerializeField] string[] worriedDialogue;

    string currentDialogue = " ";

    [SerializeField] GameObject textPosition;
    TextMesh text;

    [SerializeField] float textDurationPerWord = 0.5f;
    [SerializeField] float timeBetweenDialogues = 3;
    float timer = 0;
    float timerCap;

    float areYouFine = 0;

    public void ChooseDialogue(string[] typeOfArray)
    {
        timer = 0;
        string chosenDialogue = typeOfArray[Random.Range(0, greetingDialogue.Length - 1)];
        while (currentDialogue == chosenDialogue)
        {
            chosenDialogue = typeOfArray[Random.Range(0, greetingDialogue.Length - 1)];
        }
        text.text = chosenDialogue;
        currentDialogue = chosenDialogue;
        timerCap = currentDialogue.Split(" ").Length * textDurationPerWord;
    }

    private void Awake()
    {
        text = textPosition.GetComponent<TextMesh>();
        OnGreetingSay();
    }

    private void OnEnable()
    {
        OnGreetingSay();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        areYouFine -= Time.deltaTime;
        areYouFine = Mathf.Clamp(areYouFine, 0, 20);

        if (timer < timerCap)
        {
            text.text = currentDialogue;
        }
        else if (timer <= timerCap + timeBetweenDialogues)
        {
            text.text = " ";
        }
        else
        {
            ChooseDialogue(fillerDialogue);
        }
    }

    public void OnPurchaseSay()
    {
        ChooseDialogue(purchaseDialogue);
    }

    public void OnFailedPurchaseSay()
    {
        ChooseDialogue(failedPurchaseDialogue);
    }

    private void OnGreetingSay()
    {
        areYouFine += 1;
        ChooseDialogue(greetingDialogue);
        if (areYouFine >= 5)
        {
            ChooseDialogue(worriedDialogue);
        }
    }


}

