namespace MyRunshaw.Contracts.Requests;

public class BatchGetBody
{
    public List<string> user_ids { get; set; } = new List<string>();
}