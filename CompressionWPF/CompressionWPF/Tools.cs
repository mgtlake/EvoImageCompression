using System;

namespace GenArt.Classes
{
    public static class Tools
    {
        private static readonly Random random = new Random();

        public static readonly int MaxPolygons = 250;

        public static System.Drawing.Color avgColour;

        public static int GetRandomNumber(int min, int max)
        {
            return random.Next(min, max);
        }

        public static int MaxWidth = 200; //why hard-coded to 200? This might be a problem for images of other sizes
        public static int MaxHeight = 200;

        public static bool WillMutate(int mutationRate) //find out if a point should mutate based on mutation rate
        {
            if (GetRandomNumber(0, mutationRate) == 1) //so there will be a 1 in mutationRate (1500) chance of it mutating
                return true;
            return false;
        }
    }
}