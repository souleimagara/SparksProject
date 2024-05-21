using Beamable;
using Beamable.Server.Clients;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Models;

public class SparksController : MonoBehaviour
{
    public Button withdrawButton;
    public TextMeshProUGUI infoText;
    public TMP_InputField userinputfield;



    private TicketStatus sparksStatus = new TicketStatus();

    private async void OnEnable()
    {
        await UpdateSparksStatus();
    }

 

    private async Task UpdateSparksStatus()
    {
        try
        {
            int ticketStatusValue = await GetSparksStatus();
            sparksStatus.Status = (Ticketstatus)ticketStatusValue;
            UpdateUI();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to update Sparks status  " +e.Message);
        }
    }

    private async Task<int> GetSparksStatus()
    {
        var beamContext = BeamContext.Default;
        await beamContext.OnReady;

        return await beamContext.Microservices().FruitNinjaServer().GetSparksStatus();
    }

    private void UpdateUI()
    {
        switch (sparksStatus.Status)
        {
            case Ticketstatus.Waiting:
                infoText.text = "Your <color=#FFED00>sparks</color> are currently being processed. Please wait.";
                withdrawButton.interactable = false;
                userinputfield.interactable = false;
                break;
            case Ticketstatus.Accepted:
                infoText.text = "Congratulations! You can cash out your <color=#FFED00>sparks</color>.";
                withdrawButton.interactable = true;
                userinputfield.interactable = true;
                break;
            case Ticketstatus.Declined:
                infoText.text = "You can't cash out your <color=#FFED00>sparks</color>. Please contact support!";
                withdrawButton.interactable = false;
                userinputfield.interactable = false;
                break;
            default:
                infoText.text = "Unknown <color=#FFED00>sparks</color> status. Please try later or contact support !";
                withdrawButton.interactable = false;
                userinputfield.interactable = false;
                Debug.LogWarning("Unknown ticket status");
                break;
        }
    }

}
