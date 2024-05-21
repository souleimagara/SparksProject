public class LeaderboardEntry
{
    public long Rank { get; set; }
    public long PlayerId { get; set; }
    public double Score { get; set; }
    public string CountryCode { get; set; }
    public string TimePlayed { get; set; }
 
    public int StatutSparks { get; set; }

    // Add other properties that you expect to receive from the leaderboard
}
