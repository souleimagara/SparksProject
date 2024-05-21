using Beamable.Common;

using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Leaderboards;
using Beamable.Common.Scheduler;
using Beamable.Server;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;



namespace Beamable.Microservices
{
    [Microservice("LeaderboardServer")]
    public class LeaderboardServer : Microservice
    {


        string SparksApprovedCurrency = "currency.Sparksapproved";

        public LeaderboardRef Leaderboardstest;

        [ClientCallable]

        public async Task<string> ApproveSparksAsync(long otherUserId, long currentUserId)
        {
            try
            {
                BeamableLogger.Log("Yes Entered ApproveSparksAsync ");

                var assumed = AssumeNewUser(otherUserId, requireAdminUser: false);
                await assumed.Services.Inventory.SetCurrency(SparksApprovedCurrency, 1);
                long sparksApproved = await assumed.Services.Inventory.GetCurrency(SparksApprovedCurrency);
                string message = "Congratulation!\nYour <color=#FFED00>sparks</color> have been accepted.";

                await CauseNotification(otherUserId, message);


                return $"The spark is approved with a total of {sparksApproved} sparks.";
            }
            catch (Exception ex)
            {

                BeamableLogger.LogError($"Failed to approve sparks for user {otherUserId}: {ex}");
                ReturnToAdminContext(currentUserId);
                return $"Error approving sparks: {ex.Message}";
            }
        }






        [ClientCallable]

        public async Task<string> DeclineSparksAsync(long otherUserId, long currentUserId)
        {
            try
            {
                var assumed = AssumeNewUser(otherUserId, requireAdminUser: false);
                await assumed.Services.Inventory.SetCurrency(SparksApprovedCurrency, 2);
                long sparksApproved = await assumed.Services.Inventory.GetCurrency(SparksApprovedCurrency);
                string message = "Unfortunately!\nYour <color=#FFED00>sparks</color> have been declined. Please contact support!";
                await CauseNotification(otherUserId, message);

                ReturnToAdminContext(currentUserId);

                return $"The spark is declined. Total sparks remaining: {sparksApproved}.";
            }
            catch (Exception ex)
            {
                BeamableLogger.LogError($"Failed to decline sparks for user {otherUserId}: {ex}");
                ReturnToAdminContext(currentUserId);
                return $"Error declining sparks: {ex.Message}";
            }
        }

        [ClientCallable]
        public async Task<int> GetSparksStatusAsync(long otherUserId, long currentUserId)
        {
            try
            {
                var assumed = AssumeNewUser(otherUserId, requireAdminUser: false);
                long sparksApprovingData = await assumed.Services.Inventory.GetCurrency(SparksApprovedCurrency);


                ReturnToAdminContext(currentUserId);
                return (int)sparksApprovingData; // Assuming 0: Waiting, 1: Approved, 2: Declined
            }
            catch (Exception ex)
            {
                BeamableLogger.LogError($"Failed to get sparks status for user {otherUserId}: {ex}");
                ReturnToAdminContext(currentUserId);
                throw; // Properly handle or escalate errors
            }
        }


        [ClientCallable]

        public void ReturnToAdminContext(long adminUserId)
        {
            AssumeNewUser(adminUserId, requireAdminUser: false);
        }


        public async Task SendNotificationFromMicroservice(long playerIds, string context, object payload)
        {
            var json = UnityEngine.JsonUtility.ToJson(payload);
            await Services.Notifications.NotifyPlayer(playerIds, context, json);
        }

        [ClientCallable]
        public async Task CauseNotification(long userid, string message)
        {

            const string channel = "Spark_Status";

            var recipients = new List<long>();
            recipients.Add(userid);

            await Services.Notifications.NotifyPlayer(recipients, channel, $"message={message}");
        }


        [ClientCallable]
        public async Promise<Job> AutoApproveSparks(string leaderboardId, long gamertag)
        {

         

            // Check if there are existing jobs
            bool hasExistingJobs = await GetAllJobs(gamertag);
            BeamableLogger.Log("Check for existing jobs: " + hasExistingJobs);

            if (hasExistingJobs == false)
            {
                BeamableLogger.Log("No existing jobs found. Scheduling a new job for leaderboard ID: " + leaderboardId);
                #region time code
                //Running Daily at 3 PM

                //var job = await Services.Scheduler
                //    .Schedule()
                //    .Microservice<LeaderboardServer>() // Execute a type-safe method on the LeaderboardServer
                //    .Run(t => t.sparkcontroller, leaderboardId, gamertag) // Run AwardBonus with the provided parameters
                //    .OnCron(c => c.Daily(18)) // Run at 6 PM
                //    .Save($"awarding0-{Context.UserId}"); // Save the job with a unique identifier


                // Running Every 2 Minutes 

                // var job = await Services.Scheduler
                // .Schedule()
                // .Microservice<LeaderboardServer>() // Execute a type-safe method on the ExampleService
                // .Run(t => t.AwardBonus, leaderboardId, gamertag) // Run AwardBonus with leaderboardId and gamertag
                //.OnCron(c => c.AtSecond(0).EveryNthMinute(2).EveryHour().EveryDayOfTheWeek().EveryMonth())
                // .Save($"awarding04-{gamertag}"); // Save the job




                // Running After X Minutes (3 minutes in this case)
                DateTime scheduledTime = DateTime.UtcNow.AddMinutes(2);
                int minute = scheduledTime.Minute;
                int hour = scheduledTime.Hour;
                var job = await Services.Scheduler
                    .Schedule()
                    .Microservice<LeaderboardServer>() // Execute a type-safe method on the ExampleService
                    .Run(t => t.sparkcontroller, leaderboardId, gamertag) // Run AwardBonus with leaderboardId and gamertag
                   .OnCron(c => c.AtSecond(0).AtMinute(minute).AtHour(hour).EveryDayOfTheWeek().EveryMonth()) // Run after 3 minutes
                    .Save($"sparkcontroller-{gamertag}"); // Save the job
                #endregion
                BeamableLogger.Log("Job ID is : " + job.id);
                //SaveJobIDinsideStats("project_scheduled_jobs", job.id);

                BeamableLogger.Log("SaveJobIDinsideStats is Entered ");
                string access = "private";
                Dictionary<string, string> setStats =
                  new Dictionary<string, string>() { { "project_scheduled_jobs", job.id } };
                await Services.Stats.SetStats(access, setStats);

                BeamableLogger.Log("Job is saved inside stats ");
                return job;
            }
            else
            {
                BeamableLogger.Log("Existing job found. Retrieving job details.");

                string scheduledJobId = await GetStats(gamertag);
                var schedulerService = Provider.GetService<BeamScheduler>();
                var jobExecutions = await schedulerService.GetJobActivity(scheduledJobId);
                BeamableLogger.Log("Found scheduled job executions.");


                BeamableLogger.Log("The sheduledjob is : " + scheduledJobId);

                if (jobExecutions.Count > 0)
                {
                    var firstJobExecution = jobExecutions[0]; // Assuming you want the first job execution

                    // Initialize the Job object with properties from the job execution
                    var existingJob = new Job
                    {
                        id = firstJobExecution.ToString(),

                    };

                    return existingJob;
                }

                else
                {
                    BeamableLogger.Log("No job executions found.");
                    return null; // Or handle accordingly if no job executions are found
                }
            }
        }

        [ServerCallable]
        public async Task sparkcontroller(string leaderbordid, long gamertag)
        {
            BeamableLogger.Log("Entered:  Get all players from leaderboard. " + leaderbordid);


            await LeaderboardServiceGetBoard(leaderbordid, gamertag);



        }
     

    
        [ClientCallable]
        public async Task<string> LeaderboardServiceGetBoard(string id, long gamertag)
        {
            BeamableLogger.Log("Entered: process all players from leaderboard.");

            var LeaderboardsService = Services.Leaderboards;
            LeaderBoardView leaderBoardView = await LeaderboardsService.GetBoard(id, 0, 100000, gamertag);

            List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

            foreach (var rankEntry in leaderBoardView.rankings)
            {
                long playeruserid = rankEntry.gt;

                var assumed = AssumeNewUser(playeruserid, requireAdminUser: false);
                long sparksStatus = await assumed.Services.Inventory.GetCurrency(SparksApprovedCurrency);
                BeamableLogger.Log("the sparks is approved yes or no ?" + sparksStatus);
                // Only approve sparks if they are in waiting mode (0)
                if (sparksStatus == 0)
                {
                    await ApproveSparksAsync(playeruserid, gamertag);
                    BeamableLogger.Log("The spark has been approved for user " + playeruserid);
                }
                else
                {
                    BeamableLogger.Log("The spark is not in waiting mode for user " + playeruserid);
                }
            }

            ReturnToAdminContext(gamertag);
            return entries.ToString();
        }


        public async Task<bool> GetAllJobs(long gamertag)
        {
            bool result = false;
       

            string scheduledJobId = await GetStats(gamertag);
            BeamableLogger.Log("The sheduledjob is : " + scheduledJobId);
            List<Job> jobs = await Services.Scheduler.GetJobs();
            BeamableLogger.Log("List of jbs is  : " + jobs.Count);
            foreach (Job job in jobs)
            {
               
                BeamableLogger.Log("The job  is : " + job.id);
                if (job.id.ToString() == scheduledJobId)
                {
                    result = true;
                    break;
                }
                else
                {
                    result = false;

                }


            }

            BeamableLogger.Log("The Result is : " + result);

            return result;
        }
        [ClientCallable]
        public async Task<List<Job>> RetrieveAndLogScheduledJobs()
        {



            List<Job> jobs = await Services.Scheduler.GetJobs();

            foreach (Job job in jobs)
            {
                await GetJobActivity(job.id);
            }
            BeamableLogger.Log("The Result is : " + jobs.Count);

            return jobs;
        }
        [ClientCallable]
        public async Task<string> GetJobActivity(string jobid)
        {

            string stat;
            var api = Provider.GetService<BeamScheduler>();
            var executions = await api.GetJobActivity(jobid);

            if (executions.Count > 0)
            {
                var firstJobExecution = executions[0]; // Assuming you want the first job execution





                foreach (var item in firstJobExecution.events)
                {
                    stat = item.state.ToString();
                }


            }
            return executions.ToString();
        }






        [ClientCallable]
        public async Task DeleteAllJobs()
        {
            List<Job> jobs = await Services.Scheduler.GetJobs();
            BeamableLogger.Log($"Found {jobs.Count} scheduled jobs.");
            foreach (Job job in jobs)
            {

                BeamableLogger.Log("the id is : " + job.id);



                await Services.Scheduler.DeleteJob(job.id);


            }




        }


        [ClientCallable]
        public async Task DeleteSpeficJob(string id)
        {
            List<Job> jobs = await Services.Scheduler.GetJobs();
            BeamableLogger.Log($"Found {jobs.Count} scheduled jobs.");
            foreach (Job job in jobs)
            {
                if( job.id.ToString() == id)
                {
                    await Services.Scheduler.DeleteJob(job.id);
                    BeamableLogger.Log("the id is : " + job.id + "deleted ");
                }
               else
               {
                    BeamableLogger.Log("No id matches");
               }



              


            }

        }




        [ClientCallable]
        public async Task<string> GetStats(long gamertag)
        {
            BeamableLogger.Log("thegamertag is  : " + gamertag);

            string[] deviceNames = new string[] { "project_scheduled_jobs" };
            Dictionary<string, string> stats = await Services.Stats.GetStats("client", "private", "player", gamertag, deviceNames);


            return stats["project_scheduled_jobs"];


        }


        #region  implement batch processing
        [ClientCallable]
        public async Task<string> LeaderboardServiceGetBoardBatch(string id, long gamertag)
        {
            BeamableLogger.Log("Entered: process all players from leaderboard.");

            var LeaderboardsService = Services.Leaderboards;
            LeaderBoardView leaderBoardView = await LeaderboardsService.GetBoard(id, 0, 100000, gamertag);

            // Ensure we use the correct type for the rankings
            List<RankEntry> entries = new List<RankEntry>(leaderBoardView.rankings);

            const int BatchSize = 100; // Adjust the batch size as needed

            for (int i = 0; i < entries.Count; i += BatchSize)
            {
                var batch = entries.Skip(i).Take(BatchSize).ToList();
                await ProcessBatch(batch, gamertag);
            }

            ReturnToAdminContext(gamertag);
            return "Processed all players from leaderboard.";
        }
        private async Task ProcessBatch(List<RankEntry> batch, long gamertag)
        {
            foreach (var rankEntry in batch)
            {
                long playeruserid = rankEntry.gt;

                var assumed = AssumeNewUser(playeruserid, requireAdminUser: false);
                long sparksStatus = await assumed.Services.Inventory.GetCurrency(SparksApprovedCurrency);
                BeamableLogger.Log("the sparks is approved yes or no ?" + sparksStatus);

                // Only approve sparks if they are in waiting mode (0)
                if (sparksStatus == 0)
                {
                    await ApproveSparksAsync(playeruserid, gamertag);
                    BeamableLogger.Log("The spark has been approved for user " + playeruserid);
                }
                else
                {
                    BeamableLogger.Log("The spark is not in waiting mode for user " + playeruserid);
                }
            }
        }
        #endregion






        [ClientCallable]
        public async Promise<Job> ApproveSparksAutomaticallyWithTime(string leaderboardId, long gamertag, int time , string name)
        {
            bool hasExistingJobs = await GetAllJobs(gamertag);
            BeamableLogger.Log("the name is : " + name);
            string shedulejobname = name;
            //Running Daily at 3 PM

            var job = await Services.Scheduler
                    .Schedule()
                    .Microservice<LeaderboardServer>() // Execute a type-safe method on the LeaderboardServer
                    .Run(t => t.sparkcontroller, leaderboardId, gamertag) // Run AwardBonus with the provided parameters
                    .OnCron(c => c.Daily(time)) // Run at 3 PM
                    .Save(name + "_" +Context.UserId); // Save the job with a unique identifier




                BeamableLogger.Log("Job ID is : " + job.id);
                //SaveJobIDinsideStats("project_scheduled_jobs", job.id);

                BeamableLogger.Log("SaveJobIDinsideStats is Entered ");
                string access = "private";
                Dictionary<string, string> setStats =
                  new Dictionary<string, string>() { { "project_scheduled_jobs", job.id } };
                await Services.Stats.SetStats(access, setStats);

                BeamableLogger.Log("Job is saved inside stats ");
                return job;
           
        }

    }

   
}






