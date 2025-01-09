public class ExerciseConfig
{
    public required string ExerciseName { get; set; }
    public  string? ExerciseReqConnectionType { get; set; }
    public required string ExerciseTile { get; set; }
    public required string ExerciseDescription { get; set; }
    public string? ExerciseDifficulty { get; set; }
    public required string DockerImage { get; set; }
    public string? port { get; set; }
    public required List<string> DisplayFields { get; set; } = new();
    public bool? ClientSide { get; set; }
    public string? ClientPort  { get; set; }
    public int? MaxUsers { get; set; }

}
