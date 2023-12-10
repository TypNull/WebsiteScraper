namespace WebsiteCreator.MVVM.Model
{
    internal interface IWebsiteParser
    {

        public void AddToWebsite(NewWebsite website);
        public void GetFromWebsite(NewWebsite website);
    }
}
