namespace WebsiteCreator.MVVM.Model
{
    internal enum UsingState
    {
        Ready,
        NotUsed,
        NotFinished,
    }

    internal interface IProofable
    {
        public UsingState GetUsingState();
    }
}
