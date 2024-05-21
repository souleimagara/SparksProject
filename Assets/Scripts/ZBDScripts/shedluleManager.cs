using Beamable;
using Beamable.Common.Scheduler;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Beamable.Server.Clients;

public class shedluleManager : MonoBehaviour
{
    public TextMeshProUGUI idText, nameText , stateText;
    private Job JobEntry;

    public Button deletebutton;
    public void Setup(Job entry)
    {
        JobEntry = entry;
        deletebutton.onClick.AddListener(() => RequestPanelDisplay(JobEntry));
     
        UpdateUI(entry);

    }


    private void UpdateUI(Job entry)
    {
        idText.text = entry.id;
        nameText.text = entry.name;
      

    }



    public async void RequestPanelDisplay(Job data)
    {
        var ctx = BeamContext.Default;
        await ctx.OnReady;
        var result = await ctx.Microservices().LeaderboardServer().DeleteSpeficJob(data.id);
        Debug.Log("The result is " + result);

       await LeaderboardServiceExample.Instance.GetAllJobs();
    }


   
}
