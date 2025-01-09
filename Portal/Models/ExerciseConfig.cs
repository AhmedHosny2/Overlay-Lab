public class ExerciseConfig
{
    public ExerciseConfig()
    {
    }

    public ExerciseConfig(string ExerciseName, string ExerciseTile, string ExerciseDescription, string DockerImage, List<string> DisplayFields)
    {
        this.ExerciseName = ExerciseName;
        this.ExerciseTile = ExerciseTile;
        this.ExerciseDescription = ExerciseDescription;
        this.DockerImage = DockerImage;
        this.DisplayFields = DisplayFields;
    }

    public string ExerciseName { get; set; }
    public string? ExerciseReqConnectionType { get; set; }
    public string ExerciseTile { get; set; }
    public string ExerciseDescription { get; set; }
    public string? ExerciseDifficulty { get; set; }
    public string DockerImage { get; set; }
    public string? port { get; set; }
    public List<string> DisplayFields { get; set; } = new();
    public bool? ClientSide { get; set; }
    public string? ClientPort { get; set; }
    public int? MaxUsers { get; set; }

}
