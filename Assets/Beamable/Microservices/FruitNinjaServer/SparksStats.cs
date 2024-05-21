using System;

[Serializable]
public class SparksStats
{
    public ticketstatus ticketstatus;
}

public enum ticketstatus
{
    Waiting,
    Accepted,
    Declined
}
