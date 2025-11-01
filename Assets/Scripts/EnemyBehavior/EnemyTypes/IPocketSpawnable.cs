// IPocketSpawnable.cs
// Purpose: Marker interface for entities that can be spawned from a pocket (boss spawn points).
// Works with: BossRoombaController, CrawlerPocket, ScenePoolManager.

public interface IPocketSpawnable
{
    CrawlerPocket Pocket { get; set; }
    void OnReturnedToPocket();
}