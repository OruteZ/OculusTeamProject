using Poker;

public static class NumberExtensions
{
    public static int GetInt(this Number number)
    {
        if (number == Number.A)
        {
            return 14;
        }
        
        return (int) number;
    }
    
    public static Number GetNumber(this int number)
    {
        if (number == 14)
        {
            return Number.A;
        }
        
        return (Number) number;
    }
}