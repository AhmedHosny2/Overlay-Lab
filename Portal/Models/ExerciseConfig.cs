public class ExerciseConfig
{
    public string ExerciseName { get; set; }
    public string ExerciseReqConnectionType { get; set; }
    public string ExerciseTile { get; set; }
    public string ExerciseDescription { get; set; }
    public string ExerciseDifficulty { get; set; }
    public string DockerImage { get; set; }
    public string port { get; set; }
    public List<string> DisplayFields { get; set; } = new();
    public bool ClientSide { get; set; }
    public string ClientPort  { get; set; }


}
