using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class Models
{
        public class AppUsageStats
        {

             public string packageName;
             public long firstTimeStamp;
             public long lastTimeStamp;
             public long totalTimeInForeground;

        }

        public class RewardsResponse
        {
            public long currentTimePlayedRewarded;         
            public long TotalTimePlayed;
            public long currentSparks;
            public long currentRequiredTime;
            public bool validated;
            public bool whitelisted;
            public bool blacklisted;
            public bool error;
            public string message;
        }

        public class SendToUsernameResponse
        {
            public bool success;
            public string message;
            public long currentTimePlayed;
            public long currentSats;
            public long currentRequiredTime;
        }



        public class PlayIntegrityResponse
        {
            public bool success;
            public string message;
            public string token;
        }


            public class GameData
            {

              
                public string userId;
                public float totalPlaytimeMins;

            }



   public class TicketStatus
   {
        public  Ticketstatus Status;
    }

    public enum Ticketstatus
    {
        Waiting,
        Accepted,
        Declined
    }

}
