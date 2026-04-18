namespace Baboomz.Simulation
{
    public enum SeriesFormat
    {
        Single,
        BestOf3,
        BestOf5
    }

    public struct SeriesState
    {
        public SeriesFormat Format;
        public int RoundsPlayed;
        public int TargetWins;
        public int[] WinsPerPlayer;
        public int[] RoundWinners;

        public bool IsActive => Format != SeriesFormat.Single;

        public static SeriesState Create(SeriesFormat format, int playerCount)
        {
            int target = format == SeriesFormat.BestOf5 ? 3 : format == SeriesFormat.BestOf3 ? 2 : 1;
            int maxRounds = format == SeriesFormat.BestOf5 ? 5 : format == SeriesFormat.BestOf3 ? 3 : 1;
            return new SeriesState
            {
                Format = format,
                RoundsPlayed = 0,
                TargetWins = target,
                WinsPerPlayer = new int[playerCount],
                RoundWinners = new int[maxRounds]
            };
        }

        public void RecordRound(int winnerIndex)
        {
            if (RoundsPlayed < RoundWinners.Length)
                RoundWinners[RoundsPlayed] = winnerIndex;
            RoundsPlayed++;
            if (winnerIndex >= 0 && winnerIndex < WinsPerPlayer.Length)
                WinsPerPlayer[winnerIndex]++;
        }

        public bool IsSeriesOver()
        {
            if (!IsActive) return true;
            for (int i = 0; i < WinsPerPlayer.Length; i++)
                if (WinsPerPlayer[i] >= TargetWins) return true;
            return false;
        }

        public int GetSeriesWinner()
        {
            for (int i = 0; i < WinsPerPlayer.Length; i++)
                if (WinsPerPlayer[i] >= TargetWins) return i;
            return -1;
        }
    }
}
