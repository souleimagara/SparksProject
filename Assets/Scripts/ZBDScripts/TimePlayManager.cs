using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;

using System.Collections.Generic;
using System;
using Beamable;
using Beamable.Server.Clients;
using Beamable.Common.Leaderboards;

using System.Threading.Tasks;
using Beamable.Common.Api.Leaderboards;
using ZBD;
using static Models;
using static UnityEngine.EventSystems.EventTrigger;
using Beamable.Serialization.SmallerJSON;

public class TimePlayManager : MonoBehaviour
{
    public TMP_Text countdownLabel;
    public TMP_Text responseLabel;
    public Text sparksLabel;
    public Text sparksLabelTwo;
    public Slider rewardsSlider;
    public Text rewardsSliderInfo;

    public Button Withdrawbtn;
    public TMP_InputField userinputfield;

    public GameObject errorPanel;
    public GameObject successPanel;
    public Text errorPanelText;
    public RewardsResponse currentStats;

    public GameObject NotificationPanel;
    public Text Notificationmessage;

    private float currentTime;
    private float TotalAmountPlayTime;
    int checkTime = 60;
    string beamableGamerTag;
    long gamertag;
    Coroutine checkLoop;


    [SerializeField] private LeaderboardRef _leaderboardRef = null;


    private  void Start()
    {



        DetectNotification();
        GetBeamableGamerTag();
        Invoke("CheckTimePlayed", 5);

   
     

    }

   async void DetectNotification()
    {
        Debug.Log("Notification 2");
        var ctx = await BeamContext.Default.Instance;
        ctx.Api.NotificationService.Subscribe("Spark_Status", ReceiveNotificationthree);
        Debug.Log("Notification 2");
       
    }


    private void ReceiveNotificationthree(object obj)
    {
        Debug.Log("Notification received:");
        if (obj is ArrayDict dict)
        {
            Debug.Log("(found ArrayDict):");
            foreach (var kvp in dict)
            {
                NotificationPanel.SetActive(true);
                Notificationmessage.text = kvp.Value.ToString();
                Debug.Log($"  {kvp.Key} = {kvp.Value}");

            }
        }
        else
        {
            Debug.Log("(generic object):");
            Debug.Log(obj.ToString());
        }
    }
    private void ReceiveNotificationtwo(object obj)
    {
        Debug.Log("Notification received: " + obj);

        if (obj is Beamable.Serialization.SmallerJSON.ArrayDict dict)
        {
            Debug.Log("Found ArrayDict: Processing entries.");

           

            foreach (var kvp in dict)
            {
                Debug.Log($"Key: {kvp.Key}, Value: {kvp.Value}");
            }
        }
        else
        {
            Debug.Log("Received object is not in the expected ArrayDict format.");
        }
    }

    private void OnGlobalNotification(object obj)
    {
        
   
        switch (obj)
        {
            
            case string message:
                Debug.Log($"Global notification received: {message}");
                break;
            default:
                Debug.Log($"Unknown notification type received. type={obj.GetType()}");
                break;
        }
    }
    private void ReceiveNotification(object obj)
    {
        Debug.Log("Notification received: " + obj);

        if (obj is string jsonString)
        {
            try
            {

                NotificationData notificationData = JsonUtility.FromJson<NotificationData>(jsonString);
                if (notificationData != null)
                {
                    Debug.Log($"Notification Title: {notificationData.Title}");
                    Debug.Log($"Notification Message: {notificationData.Message}");
                }
                else
                {
                    Debug.Log("Failed to deserialize notification.");
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Error parsing notification JSON: " + ex.Message);
            }
        }
        else
        {
            Debug.Log("Received notification is not in the expected format.");
        }
    }

    async void GetStats()
    {

        var ctx = BeamContext.Default;
        await ctx.OnReady;
        var result = await ctx.Microservices().FruitNinjaServer().GetStats();
        currentStats = JsonConvert.DeserializeObject<RewardsResponse>(result);
     
      
        SetStats();
    }


    void SetStats()
    {

        sparksLabel.text = currentStats.currentSparks + "";
        sparksLabelTwo.text = currentStats.currentSparks.ToString();
        responseLabel.text = "Loading ... ";
       

        float currentEarningPercent = (float)currentStats.currentTimePlayedRewarded / (float)currentStats.currentRequiredTime;
        if (currentEarningPercent > 1)
        {
            currentEarningPercent = 1;
        }
        rewardsSlider.value = currentEarningPercent;
        long timePlayedCorrected = currentStats.currentTimePlayedRewarded;
        if (timePlayedCorrected > currentStats.currentRequiredTime)
        {
            timePlayedCorrected = currentStats.currentRequiredTime;
        }
        rewardsSliderInfo.text = FormatRewardsSliderInfo(timePlayedCorrected, currentStats.currentRequiredTime);

        if (currentStats.error)
        {
            responseLabel.text = currentStats.message;
        }
        else
        {
            responseLabel.text = currentStats.currentTimePlayedRewarded + "/" + currentStats.currentRequiredTime + " mins\n" + currentStats.currentSparks + " sparks\n" + " User ID : " +beamableGamerTag ;

        }

 
    }


    string FormatRewardsSliderInfo(long currentTime, long requiredTime)
    {
        return currentTime + "/" + requiredTime + " mins approx";
    }


    void CheckTimePlayed()
    {
        countdownLabel.text = "updating...";
        GetStats();
       

        try
        {
          
            GameData gameData = new GameData();
            gameData.totalPlaytimeMins = ZBDPlaytimeTracker.Instance.GetTotalPlayTimeMins();
            gameData.userId = SystemInfo.deviceUniqueIdentifier; ;

            Debug.Log("Total time in minutes : " + gameData.totalPlaytimeMins);
            if( gameData.totalPlaytimeMins > 0 )
            {
                string payload = JsonConvert.SerializeObject(gameData);

                Debug.Log("Check time paylod " + payload);

                SendToGameServer(payload);
            }
            else
            {

                ResetTimePlayed();
            }
           





        }
        catch (Exception e)
        {
            Debug.LogError("catch " + e);
            responseLabel.text = e + "";
            ResetTimePlayed();
        }


    }


    async void SendToGameServer(string payload )
    {
        responseLabel.text = "Loading ... ";
        var ctx = BeamContext.Default;
        await ctx.OnReady;
        try
        {

            Debug.Log("the payload is " + payload);
            var result = await ctx.Microservices().FruitNinjaServer().SendGamePlay(payload);
            Debug.Log("the result is : " +result);
            currentStats = JsonConvert.DeserializeObject<RewardsResponse>(result);
            ZBDPlaytimeTracker.Instance.ForceSavePlayTime(currentStats.TotalTimePlayed);
            if (currentStats.error == true)
            {
                Debug.Log("The error is " + currentStats.error);
                responseLabel.text = currentStats.message;
                ResetTimePlayed();

            }
            else
            {
            
                LeaderboardServiceSetSparks(_leaderboardRef.Id, currentStats.currentSparks, currentStats.TotalTimePlayed , gamertag);
                SetStats();
                ResetTimePlayed();

            }




        }
        catch (Exception e)
        {

            Debug.LogError(e);
            responseLabel.text = e.ToString();

        }
    }


    void ResetTimePlayed()
    {
       
        currentTime = checkTime;
        if (checkLoop != null)
        {
            StopCoroutine(checkLoop);
        }
        checkLoop = StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        while (currentTime >= 0)
        {
            yield return new WaitForSeconds(1f);


            currentTime--;
            TotalAmountPlayTime++;
          
            if (currentTime == 0)
            {
                CheckTimePlayed();
            }
            else if (currentTime > 0)
            {
                countdownLabel.text = currentTime.ToString("0");
            }
        }
    }




    public async void GetBeamableGamerTag()
    {
        try
        {
            var ctx = BeamContext.Default;
            await ctx.OnReady;
            beamableGamerTag = ctx.Api.User.id + "";
            gamertag = ctx.Api.User.id;
            Debug.Log("GamerTag : " + beamableGamerTag);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            beamableGamerTag = "error getting beamable id";
        }
     

    }
    public async Task<int> GetSparksInfo()
    {
        var ctx = BeamContext.Default;
        await ctx.OnReady;
        try
        {
            

           
                int status = await ctx.Microservices().FruitNinjaServer().GetSparksStatus();
                return status;
          
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return -1;  
        }
    }

    private async void LeaderboardServiceSetSparks(string leaderbordid, double score  ,  long currentimeplayed , long gamertagid )
    {
        var beamContext = BeamContext.Default;
        await beamContext.OnReady;

        Debug.Log($"beamContext.PlayerId = {beamContext.PlayerId}");
        string playerstatsresult = await beamContext.Microservices().FruitNinjaServer().GetplayerStats(beamContext.PlayerId);
        PlayerStats result = JsonConvert.DeserializeObject<PlayerStats>(playerstatsresult);
      
     
        Dictionary<string, object> leaderboardStats = new Dictionary<string, object>();
        leaderboardStats.Add("player_country_code", result.CountryCode);
        leaderboardStats.Add("player_geoContinent_code", result.GeoContinentCode);
        leaderboardStats.Add("player_time_played", currentimeplayed);
        int sparksStatus = await GetSparksInfo();
        leaderboardStats.Add("player_sparks_status", sparksStatus);
        string formattedDate = DateTime.Now.ToString("ddd, MMM dd, yyyy HH:mm:ss");
        leaderboardStats.Add("player_updatedata_datetime", formattedDate); 
        leaderboardStats.Add("player_gamertag_id", gamertagid);                      
        await beamContext.Api.LeaderboardService.SetScore(leaderbordid, score , leaderboardStats);
      
       
        Debug.Log($"LeaderboardService.SetScore({leaderbordid},{score})");
       
    }

   

    public void WithdrawBitcoin()
    {

        string username = userinputfield.text;

        if (username.Length == 0)
        {
            ShowError("please enter your ZBD username");
            return;
        }
        Withdrawbtn.interactable = false;
        try
        {
                    ContinueWithdraw(username); 
        }
        catch (Exception e)
        {
            Debug.LogError("The error " + e.ToString());

            Withdrawbtn.interactable = false;
            ShowError("error");
            Debug.LogError(e);

        }

    }
    async void ContinueWithdraw(string username)
    {

        var ctx = BeamContext.Default;
        await ctx.OnReady;
        var result = await ctx.Microservices().FruitNinjaServer().WithdrawBitcoin(username);

        SendToUsernameResponse res = JsonConvert.DeserializeObject<SendToUsernameResponse>(result);

        if (!res.success)
        {
            ShowError(res.message);
        }
        else
        {
            PlayerPrefs.SetString("username", username);
            ShowSuccess();
            sparksLabel.text = "0";
            sparksLabelTwo.text = "0";
            rewardsSliderInfo.text = FormatRewardsSliderInfo(0, res.currentRequiredTime);
            ResetTimePlayed();
           
        }
        Withdrawbtn.interactable = true;
    }

    void ShowError(string message)
    {
        errorPanel.SetActive(true);
        errorPanelText.text = message;
    }
    void ShowSuccess()
    {
        successPanel.SetActive(true);
    }

}
