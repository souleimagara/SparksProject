using Beamable;
using Beamable.Api.Autogenerated.Models;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Social;
using Beamable.Common.Content;
using Beamable.Common.Leaderboards;
using Beamable.Server.Clients;
using Beamable.Theme;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Models;

public class SparksManager : MonoBehaviour
{


    public TextMeshProUGUI playerIdText, rankText, scoreText, countryText, playtimeText, playerStatusText;
    public Button detailButton;

    private LeaderboardEntry currentEntry;




    public  void Setup(LeaderboardEntry entry)
    {
        currentEntry = entry;
        detailButton.onClick.AddListener(() => RequestPanelDisplay(currentEntry));
        UpdateUI(entry);
      
    }

    private  void UpdateUI(LeaderboardEntry entry)
    {
        rankText.text = entry.Rank.ToString();
        playerIdText.text = entry.PlayerId.ToString();
        scoreText.text = entry.Score.ToString();
        countryText.text = entry.CountryCode;
        playtimeText.text = entry.TimePlayed;
        UpdateStatusText();
    }

    private async void UpdateStatusText()
    {
        int sparksStatus = await GetSparksInfo(currentEntry.PlayerId.ToString());
        playerStatusText.text = GetStatusText(sparksStatus);
        SetStatusColor(sparksStatus);
    }
    private string GetStatusText(int status)
    {
        return status switch
        {
            0 => "Waiting",
            1 => "Accepted",
            2 => "Declined",
            _ => "Unknown"
        };
    }
    private void SetStatusColor(int status)
    {
        Color color = status switch
        {
            0 => Color.blue,
            1 => Color.green,
            2 => Color.red,
            _ => Color.black
        };
        playerStatusText.color = color;
    }
    public async Task<int> GetSparksInfo(string playerId)
    {
        var ctx = BeamContext.Default;
        await ctx.OnReady;
        long playerIdLong = long.Parse(playerId);

      //  Debug.Log("Get sparks info " + playerIdLong);
        return await ctx.Microservices().LeaderboardServer().GetSparksStatusAsync(playerIdLong , ctx.PlayerId);
    }

   
    public async void RequestPanelDisplay(LeaderboardEntry data)
    {
        int sparksStatus = await GetSparksInfo(data.PlayerId.ToString());
        LeaderboardServiceExample.Instance.ShowPanel(sparksStatus, data.PlayerId.ToString(), data.Score, data.TimePlayed);
    }



}
