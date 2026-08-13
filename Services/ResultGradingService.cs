namespace UserManagementApi.Services;

public record GradeResult(
    string Grade,
    string Remark);

public interface IResultGradingService
{
    GradeResult Calculate(decimal score);
}

public class ResultGradingService : IResultGradingService
{
    public GradeResult Calculate(decimal score)
    {
        return score switch
        {
            >= 80 => new("A", "Excellent"),
            >= 70 => new("B", "Very Good"),
            >= 60 => new("C", "Good"),
            >= 50 => new("D", "Pass"),
            >= 40 => new("E", "Fair"),
            _ => new("F", "Fail")
        };
    }
}