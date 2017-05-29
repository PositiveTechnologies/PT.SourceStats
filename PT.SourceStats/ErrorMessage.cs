namespace PT.SourceStats
{
    public class ErrorMessage : Message
    {
        public override MessageType MessageType => MessageType.Error;

        public string Message { get; set; }

        public ErrorMessage()
        {
        }

        public ErrorMessage(string message)
        {
            Message = message;
        }
    }
}
