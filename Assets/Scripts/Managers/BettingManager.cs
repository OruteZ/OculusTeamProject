// ReSharper disable CheckNamespace
public class BettingManager : Util.Singleton<BettingManager>
{
    public int currentBet;
    public int totalBet;
    public int totalPot;

    public void Bet(int amount)
    {
        if (amount < currentBet)
        {
            return;
        }
        
        currentBet = amount;
        totalBet += amount;
        totalPot += amount;
    }
    
    public bool CanBet(int amount)
    {
        return amount >= currentBet;
    }

    public void Reset()
    {
        currentBet = 0;
        totalBet = 0;
    }

    public bool CanCheck()
    {
        // 1. 현재 배팅이 0이라면 가능
        if (currentBet == 0)
        {
            return true;
        }

        return false;
    }

    public bool BetFinished()
    {
        // 연속 몇개의 배팅을 받았는지에 대해서 판단
        return false;
    }
}