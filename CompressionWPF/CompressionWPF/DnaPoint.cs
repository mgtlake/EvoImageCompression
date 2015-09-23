using System;
using GenArt.Classes;

namespace GenArt.AST
{
    [Serializable]
    public class DnaPoint
    {
        public int X { get; set; } //gives the point an x and y value
        public int Y { get; set; }

        public void Init()
        {
            X = Tools.GetRandomNumber(0, Tools.MaxWidth); //sets the x and y to a random number within the image range
            Y = Tools.GetRandomNumber(0, Tools.MaxHeight);
        }

        public DnaPoint Clone() //produces an exact clone of the point
        {
            return new DnaPoint
                       {
                           X = X,
                           Y = Y,
                       };
        }

        public void Mutate(DnaDrawing drawing) //produces a mutated/changed copy - basically, samll chance to mutate a lot, larger chance to mutate a little
        {
            if (Tools.WillMutate(Settings.ActiveMovePointMaxMutationRate)) //find out if it should mutate based on max rate
            {
                X = Tools.GetRandomNumber(0, Tools.MaxWidth); //set x and y to another random point
                Y = Tools.GetRandomNumber(0, Tools.MaxHeight);
                drawing.SetDirty(); //???? makes it dirty - this means that the drawing can be updates
            }

            if (Tools.WillMutate(Settings.ActiveMovePointMidMutationRate))
            {
                X =
                    Math.Min(
                        Math.Max(0,
                                 X +
                                 Tools.GetRandomNumber(-Settings.ActiveMovePointRangeMid,
                                                       Settings.ActiveMovePointRangeMid)), Tools.MaxWidth);
                Y =
                    Math.Min(
                        Math.Max(0,
                                 Y +
                                 Tools.GetRandomNumber(-Settings.ActiveMovePointRangeMid,
                                                       Settings.ActiveMovePointRangeMid)), Tools.MaxHeight);
                drawing.SetDirty();
            }

            if (Tools.WillMutate(Settings.ActiveMovePointMinMutationRate))
            {
                X =
                    Math.Min(
                        Math.Max(0,
                                 X +
                                 Tools.GetRandomNumber(-Settings.ActiveMovePointRangeMin,
                                                       Settings.ActiveMovePointRangeMin)), Tools.MaxWidth);
                Y =
                    Math.Min(
                        Math.Max(0,
                                 Y +
                                 Tools.GetRandomNumber(-Settings.ActiveMovePointRangeMin,
                                                       Settings.ActiveMovePointRangeMin)), Tools.MaxHeight);
                drawing.SetDirty();
            }
        }
    }
}