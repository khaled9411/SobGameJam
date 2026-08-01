using UnityEngine;
using UnityEngine.UI;

namespace SobGameJam.MiniGames
{
public class HeartGameController : MiniGameBase
{
   [SerializeField] private CircleSpawner spawner;
   [SerializeField] private Slider scoreBar;

   [SerializeField] private int maxScore = 100;
   [SerializeField] private int perfectPoints = 10;
   [SerializeField] private int missedPoints = -5;


   private int currentScore = 0;


   private void Start()  //for testing, remove once finished
   {
      OnGameStarted(1);
   }
    protected override void OnGameStarted(int roundNumber)
    {
        Debug.Log($"Round {roundNumber} started!");

        scoreBar.maxValue = maxScore;
        scoreBar.value = currentScore;

        spawner.OnPerfectTiming += HandlePerfect;
        spawner.OnBadTiming += HandleBadTiming;
        spawner.OnMissed += HandleMissed;

        spawner.StartSpawning();

    }
    private void OnDestroy()
    {
        spawner.OnPerfectTiming -= HandlePerfect;
        spawner.OnBadTiming -= HandleBadTiming;
        spawner.OnMissed -= HandleMissed;
    }
    private void AddScore(int amount)
    {
        currentScore += amount;
    
        currentScore = Mathf.Clamp(currentScore, 0, maxScore);

        RefreshScoreBar();
    
        if (currentScore >= maxScore)
        {
            spawner.StopSpawning();
            //StartPhaseTwo
        }
    }

    private void HandlePerfect()
    {
        AddScore(perfectPoints);
    }
    
    private void HandleBadTiming()
    {
        // no score change
    }
    
    private void HandleMissed()
    {
        AddScore(missedPoints);
    }
    private void RefreshScoreBar()
    {
       scoreBar.value = currentScore;
       //Add VFX here
    }
}


}
