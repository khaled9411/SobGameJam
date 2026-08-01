using UnityEngine;

namespace SobGameJam.MiniGames
{
public class HeartGameController : MiniGameBase
{
    [SerializeField] private CircleSpawner spawner;
    private void Start()  //for testing, remove once finished
   {
      OnGameStarted(1);
   }
    protected override void OnGameStarted(int roundNumber)
    {
        Debug.Log($"Round {roundNumber} started!");
        spawner.StartSpawning();
    }


}

}
