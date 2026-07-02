using System.Diagnostics;

public class AiReviewTriggerTest
{
    private string password = "dummy-password-12345";

    public void RunDangerousCommand()
    {
        Process.Start("cmd.exe");
    }
}
