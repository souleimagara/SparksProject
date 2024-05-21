
using Beamable;
using Beamable.Common.Api.Leaderboards;

using Beamable.Common.Leaderboards;
using Beamable.Common.Scheduler;
using Beamable.Server.Clients;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class LeaderboardServiceExample : MonoBehaviour
{

    public static LeaderboardServiceExample Instance;

    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Transform entriesParent;
    [SerializeField] private LeaderboardRef leaderboardRef;
    [SerializeField] private GameObject shedlulePrefab;
    [SerializeField] private Transform shedluleParent;
    [SerializeField] private GameObject infoPanel; // Assign in inspector
    [SerializeField] private Text statustxt;       // Assign in inspector
    [SerializeField] private Text playerinfostxt;  // Assign in inspector
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private InputField scheduleJobInputField;


    private long gamertag;
    private string selectedPlayerId;
    private int selectedTime;

    private Dictionary<string, int> timeMappings;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private async void Start()
    {


        timeMappings = new Dictionary<string, int>
        {
            { "3 PM", 15 },
            { "6 PM", 18 },
            { "1 AM", 1 }

            // Add more time
        };

        dropdown.onValueChanged.AddListener(delegate
        {
            DropdownValueChanged(dropdown);
        });

        await LoadLeaderboardAsync();
     //   await ApproveSparksAutomatically();
        await GetAllJobs();


        // To delete all jobs
        await DeleteAllJobs();

    }

    private void DropdownValueChanged(TMP_Dropdown change)
    {
        selectedTime = GetSelectedTime(change);
        Debug.Log("Selected time: " + selectedTime);
    }

    private int GetSelectedTime(TMP_Dropdown dropdown)
    {
        string selectedOption = dropdown.options[dropdown.value].text;
        return timeMappings.TryGetValue(selectedOption, out int timeValue) ? timeValue : -1;
    }

    private async Task LoadLeaderboardAsync()
    {
        DestroyAllChildren(entriesParent);

        if (leaderboardRef == null)
        {
            Debug.LogError("Leaderboard reference not set.");
            return;
        }

        try
        {
            var leaderboardRankEntries = await LeaderboardServiceGetBoard(leaderboardRef.Id);
            foreach (var entry in leaderboardRankEntries)
            {
                InstantiateLeaderboardEntry(entry);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load leaderboard: {e.Message}");
        }
    }

  


    private void InstantiateLeaderboardEntry(LeaderboardEntry data)
    {
        GameObject entryGameObject = Instantiate(entryPrefab, entriesParent);
        SparksManager entryUI = entryGameObject.GetComponent<SparksManager>();

        if (entryUI != null)
        {
            entryUI.Setup(data);
        }
        else
        {
            Debug.LogError("LeaderboardEntryUI component is missing from the prefab.");
        }
    }

    private async Task<List<LeaderboardEntry>> LeaderboardServiceGetBoard(string id)
    {
        var beamContext = BeamContext.Default;
        await beamContext.OnReady;
        long gamertag = beamContext.Api.User.id;

        Debug.Log("GamerTag is " + gamertag);

        LeaderBoardView leaderBoardView = await beamContext.Api.LeaderboardService.GetBoard(id, 0, 100, gamertag);
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        foreach (var rankEntry in leaderBoardView.rankings)
        {
            long nextUserId = rankEntry.gt;
            var stats = await beamContext.Api.StatsService.GetStats("client", "public", "player", nextUserId);

            string countryCode = "", timePlayed = "", sparkStatus = "";

            foreach (var stat in rankEntry.stats)
            {
                if (stat.name == "player_country_code")
                    countryCode = stat.value.ToString();
                else if (stat.name == "player_time_played")
                    timePlayed = stat.value.ToString();
                else if (stat.name == "player_sparks_status")
                    sparkStatus = stat.value.ToString();
            }

            int parsedStatus = Convert.ToInt32(sparkStatus);

            entries.Add(new LeaderboardEntry
            {
                Rank = rankEntry.rank,
                PlayerId = rankEntry.gt,
                Score = rankEntry.score,
                CountryCode = countryCode,
                TimePlayed = timePlayed,
                StatutSparks = parsedStatus
            });
        }

        return entries;
    }

    public async Task ApproveSparksAutomatically()
    {
        var ctx = BeamContext.Default;
        await ctx.OnReady;

        try
        {
            Job job = await ctx.Microservices().LeaderboardServer().AutoApproveSparks(leaderboardRef.Id, ctx.PlayerId);

            if (job != null)
            {
                Debug.Log($"Job Details: \n" +
                          $"Status: {job.action}\n" +
                          $"Name: {job.name}\n" +
                          $"Last Update: {job.lastUpdate}\n" +
                          $"ID: {job.id}\n" +
                          $"Owner: {job.owner}\n" +
                          $"Triggers: {job.triggers}\n" +
                          $"Source: {job.source}");
            }
            else
            {
                Debug.LogWarning("No job was returned from ApproveSparksAutomatically.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while approving sparks: {ex.Message}");
        }
    }

    public async void DeclineSparks()
    {
        var ctx = BeamContext.Default;
        await ctx.OnReady;

        try
        {
            gamertag = ctx.Api.User.id;
            long playerIdLong = long.Parse(selectedPlayerId);

            var result = await ctx.Microservices().LeaderboardServer().DeclineSparksAsync(playerIdLong, gamertag);
            Debug.Log("Result of declining: " + result);

            await LoadLeaderboardAsync();
            infoPanel.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public async void AcceptSparks()
    {
        var ctx = BeamContext.Default;
        await ctx.OnReady;

        try
        {
            gamertag = ctx.Api.User.id;
            long playerIdLong = long.Parse(selectedPlayerId);

            var result = await ctx.Microservices().LeaderboardServer().ApproveSparksAsync(playerIdLong, gamertag);
            Debug.Log("Result of accepting: " + result);

            await LoadLeaderboardAsync();
            infoPanel.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void InstantiateJobEntity(Job job)
    {
        GameObject entryGameObject = Instantiate(shedlulePrefab, shedluleParent);
        shedluleManager entryUI = entryGameObject.GetComponent<shedluleManager>();

        if (entryUI != null)
        {
            entryUI.Setup(job);
        }
        else
        {
            Debug.LogError("shedluleManager component is missing from the prefab.");
        }
    }

    public async void ScheduleApproveSparks()
    {
        var ctx = BeamContext.Default;
        await ctx.OnReady;

        long gamertag = ctx.Api.User.id;
        Debug.Log("The input field is " + scheduleJobInputField.text);

        var result = await ctx.Microservices().LeaderboardServer()
            .ApproveSparksAutomaticallyWithTime(leaderboardRef.Id, ctx.PlayerId, selectedTime, scheduleJobInputField.text);

        Debug.Log("The result is " + result);

        await GetAllJobs();
    }

    public void ShowPanel(int status, string playerId, double score, string timePlayed)
    {
        selectedPlayerId = playerId;
        infoPanel.SetActive(true);

        switch (status)
        {
            case 0:
                statustxt.text = "Waiting";
                statustxt.color = Color.blue;  // Display blue text for waiting
                break;
            case 1:
                statustxt.text = "Accepted";
                statustxt.color = Color.green;  // Display green text for accepted
                break;
            case 2:
                statustxt.text = "Declined";
                statustxt.color = Color.red;  // Display red text for declined
                break;
            default:
                statustxt.text = "Unknown";
                statustxt.color = Color.black;  // Handle unexpected values
                break;
        }

        playerinfostxt.text = $"The player with the Gamertag {playerId} has earned {score} sparks in {timePlayed} minutes";
    }


    public async Task DeleteAllJobs()
    {
        var ctx = BeamContext.Default;
        await ctx.OnReady;

        var result = await ctx.Microservices().LeaderboardServer().DeleteAllJobs();
        Debug.Log("The result is " + result);
    }

    public async Task GetAllJobs()
    {
        DestroyAllChildren(shedluleParent);

        var ctx = BeamContext.Default;
        await ctx.OnReady;

        List<Job> result = await ctx.Microservices().LeaderboardServer().RetrieveAndLogScheduledJobs();

        foreach (Job job in result)
        {
          

            InstantiateJobEntity(job);
        }

      
    }

    private void DestroyAllChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

}

