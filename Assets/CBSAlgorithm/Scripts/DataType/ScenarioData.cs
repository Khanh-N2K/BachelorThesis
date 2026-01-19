[System.Serializable]
public class ScenarioData
{
    public int agentId;
    public Int2 start;
    public Int2 goal;

    public ScenarioData(int agentId, Int2 start, Int2 goal)
    {
        this.agentId = agentId;
        this.start = start;
        this.goal = goal;
    }
}