using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Server;
using Beamable.Server.Api.RealmConfig;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static Models;

namespace Beamable.Microservices
{
	[Microservice("FruitNinjaServer")]
	public class FruitNinjaServer : Microservice
	{
        string sparksKey = "currency.sparks";
        string RewardedTime = "currency.rewardedTime";
        string TotalPlaytime = "currency.totalPlaytime";
        string Sparksapproved = "currency.Sparksapproved";




        [ClientCallable]
        public async Task<string> GetStats()
        {

           
            RewardsResponse rewardRes = new RewardsResponse();
            long currentsparks = await Services.Inventory.GetCurrency(sparksKey);
            long currentTimePlayerrewarded = await Services.Inventory.GetCurrency(RewardedTime);
            RealmConfig config = await Services.RealmConfig.GetRealmConfigSettings();

            long sparkTime = long.Parse(config.GetSetting("accounts", "sparkTime", "15"));

            rewardRes.currentSparks = currentsparks;
            rewardRes.currentTimePlayedRewarded = currentTimePlayerrewarded;
            rewardRes.currentRequiredTime = sparkTime;
            return JsonConvert.SerializeObject(rewardRes);
        }



        [ClientCallable]
        public async Task<string> SendGamePlay(string payload)
        {
            BeamableLogger.Log(" The  payload : " + payload);

            RewardsResponse sparkRes = new RewardsResponse();
            try
            {
                RealmConfig config = await Services.RealmConfig.GetRealmConfigSettings();

                long sparkTime = long.Parse(config.GetSetting("accounts", "sparkTime", "6"));
                sparkRes.currentRequiredTime = sparkTime;
                BeamableLogger.Log(" The  sparkTime : " + sparkTime);

                int sparkAmount = int.Parse(config.GetSetting("accounts", "sparksAmount", "1"));
                BeamableLogger.Log(" The  sparkAmount : " + sparkAmount);


                long rewardedtime = await Services.Inventory.GetCurrency(RewardedTime);

               long totalplaytimefromserver = await Services.Inventory.GetCurrency(TotalPlaytime);

                long currentsparks = await Services.Inventory.GetCurrency(sparksKey);

                GameData gameData = JsonConvert.DeserializeObject<GameData>(payload);
                BeamableLogger.Log(" The json converted payload : " + gameData.ToString());

                if (gameData.totalPlaytimeMins == 0)
                {
                    sparkRes.error = true;
                    sparkRes.message = "no play time detected";
                    return JsonConvert.SerializeObject(sparkRes);
                }

                long totalPlaytimeMins = (long)gameData.totalPlaytimeMins;

                if(totalplaytimefromserver >  totalPlaytimeMins)
                {
                    totalPlaytimeMins = totalplaytimefromserver;
                   
                }
              
                await Services.Inventory.SetCurrency(TotalPlaytime, totalPlaytimeMins);

                BeamableLogger.Log(" The totalPlaytimeMins : " + totalPlaytimeMins);

                long currentPlayTimeMins = totalPlaytimeMins - rewardedtime;
                BeamableLogger.Log(" The currentPlayTimeMins 1 : " + currentPlayTimeMins);

                
                if (currentPlayTimeMins >= sparkTime)
                {
                    currentsparks += sparkAmount;
                    rewardedtime = totalPlaytimeMins;
                    currentPlayTimeMins = 0;
                    await Services.Inventory.SetCurrency(sparksKey, currentsparks);
                    await Services.Inventory.SetCurrency(RewardedTime, rewardedtime);
                    await Services.Inventory.SetCurrency(TotalPlaytime, totalPlaytimeMins);

                    sparkRes.error = false;
                    sparkRes.message = " is here why not adding " + currentsparks + " " + rewardedtime;
                    BeamableLogger.Log(" The currentPlayTimeMins 2 : " + currentPlayTimeMins);

                    sparkRes.currentRequiredTime = sparkTime;
                    sparkRes.currentSparks = currentsparks;
                    sparkRes.currentTimePlayedRewarded = currentPlayTimeMins;
                    sparkRes.TotalTimePlayed = totalPlaytimeMins;
                    sparkRes.message = "SparkTime : " + sparkRes.currentRequiredTime + " current sparks : " + sparkRes.currentSparks + " current time played : " + sparkRes.currentTimePlayedRewarded;

                    return JsonConvert.SerializeObject(sparkRes);

                }
                else
                {
                    BeamableLogger.Log(" The currentPlayTimeMins 2 : " + currentPlayTimeMins);

                    sparkRes.currentRequiredTime = sparkTime;
                    sparkRes.currentSparks = currentsparks;
                    sparkRes.currentTimePlayedRewarded = currentPlayTimeMins;
                    sparkRes.TotalTimePlayed = totalPlaytimeMins;
                    sparkRes.message = "SparkTime : " + sparkRes.currentRequiredTime + " current sparks : " + sparkRes.currentSparks + " current time played : " + sparkRes.currentTimePlayedRewarded;
                    return JsonConvert.SerializeObject(sparkRes);

                }

               
            }
            catch (Exception e)
            {

                BeamableLogger.LogError(e);

                sparkRes.error = true;
                sparkRes.message = e + "";
                return JsonConvert.SerializeObject(sparkRes);
            }
        }





        [ClientCallable]
        public async Task<string> GetplayerLocation(long useris)
        {

            string[] statslist = new string[] { "location", "geo_continent_code" };
            Dictionary<string, string> statsDictionary = await Services.Stats.GetProtectedPlayerStats(useris, statslist);

            PlayerStats playerStats = new PlayerStats
            {
                CountryCode = statsDictionary["location"],
                GeoContinentCode = statsDictionary["geo_continent_code"]
            };
            string serializedStats = JsonConvert.SerializeObject(playerStats);

            return serializedStats;

        }
        private async void LeaderboardServiceSetSparks(string leaderboardid, double score, long currenttotalplaytime , long userid)
        {
            string playerstatsresult = await GetplayerLocation(userid);
            BeamableLogger.Log(" The result total : " + playerstatsresult.ToString());

            PlayerStats result = JsonConvert.DeserializeObject<PlayerStats>(playerstatsresult);

            BeamableLogger.Log(" The result of countrycode : " + result.CountryCode);
            BeamableLogger.Log(" The result of continetncode : " + result.GeoContinentCode);

            var LeaderboardsService = Services.Leaderboards;
  

            Dictionary<string, object> leaderboardStats = new Dictionary<string, object>();
            leaderboardStats.Add("player_country_code", result.CountryCode);
            leaderboardStats.Add("player_geoContinent_code", result.GeoContinentCode);
            leaderboardStats.Add("player_total_time_played", currenttotalplaytime);
            string formattedDate = DateTime.Now.ToString("ddd, MMM dd, yyyy HH:mm:ss");
            leaderboardStats.Add("leaderboard_score_datetime", formattedDate);

            await LeaderboardsService.SetScore(leaderboardid, score, leaderboardStats);

            //    await beamContext.Api.LeaderboardService.IncrementScore(id, 35 , leaderboardStats);


        }


        [ClientCallable]
        public async Task<string> GetplayerStats(long useris)
        {


            string[] statslist = new string[] { "location", "geo_continent_code" };
            Dictionary<string, string> statsDictionary = await Services.Stats.GetProtectedPlayerStats(useris, statslist);

            PlayerStats playerStats = new PlayerStats
            {
                CountryCode = statsDictionary["location"],
                GeoContinentCode = statsDictionary["geo_continent_code"]
            };
            string serializedStats = JsonConvert.SerializeObject(playerStats);

            return serializedStats;

        }

        [ClientCallable]
        public async Task<int> GetSparksStatus()
        {
            long sparksBalance = await Services.Inventory.GetCurrency(Sparksapproved);


            int sparksStats = (int)sparksBalance;
            BeamableLogger.Log(" The sparksStats : " + sparksStats);
            return sparksStats;
        }

        [ClientCallable]
        public async Task<string> WithdrawBitcoin(string username )
        {

            long currentSparks = await Services.Inventory.GetCurrency(sparksKey);
          

            SendToUsernameResponse res = new SendToUsernameResponse();
            if (currentSparks == 0)
            {
                res.success = false;
                res.message = "You have not earned enough bitcoin to withdraw";
                return JsonConvert.SerializeObject(res);
            }


            RealmConfig config = await Services.RealmConfig.GetRealmConfigSettings();

            string apikey = config.GetSetting("accounts", "zbdApiKey", "");

            if (apikey.Length == 0)
            {
                res.success = false;
                res.message = "zbdApiKey not set in beamable dashboard";
                await Services.Inventory.SetCurrency(sparksKey, currentSparks);
                return JsonConvert.SerializeObject(res);
            }





            res = await ZBDAPIController.SendToUsername(username, (int)currentSparks, "Withdrawal", apikey);


            if (!res.success)
            {
                await Services.Inventory.SetCurrency(sparksKey, currentSparks);              
                return JsonConvert.SerializeObject(res);

            }


            await Services.Inventory.SetCurrency(sparksKey, 0);
            await Services.Inventory.SetCurrency(Sparksapproved, 0);
            res.currentSats = 0;



            return
            JsonConvert.SerializeObject(res);

        }
    }
}
