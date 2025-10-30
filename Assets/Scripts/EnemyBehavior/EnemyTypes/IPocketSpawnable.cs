public interface IPocketSpawnable
{
    CrawlerPocket Pocket { get; set; }
    void OnReturnedToPocket();
}