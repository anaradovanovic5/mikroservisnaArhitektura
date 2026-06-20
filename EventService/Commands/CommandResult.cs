namespace EventService.Commands
{
    public class CommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int? DogadjajId { get; set; }

        public static CommandResult Ok(string message, int? dogadjajId = null)
            => new CommandResult { Success = true, Message = message, DogadjajId = dogadjajId };

        public static CommandResult Fail(string message)
            => new CommandResult { Success = false, Message = message };
    }
}