using GenArt.Classes;
using System;

namespace GenArt.AST
{
    [Serializable]
    public class DnaBrush
    {
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
        public int Alpha { get; set; }

        public void Init()
        {
            Red = Tools.GetRandomNumber(0, 255); //set the colour channels to a random value
            Green = Tools.GetRandomNumber(0, 255);
            Blue = Tools.GetRandomNumber(0, 255);
            Alpha = Tools.GetRandomNumber(10, 60);
        }

        public DnaBrush Clone() //if we're cloning, keep the same colours
        {
            return new DnaBrush
                       {
                           Alpha = Alpha,
                           Blue = Blue,
                           Green = Green,
                           Red = Red,
                       };
        }

        public void Mutate(DnaDrawing drawing)
        {
            if (Tools.WillMutate(Settings.ActiveRedMutationRate)) //chance that the red will mutate
            {
                Red = Tools.GetRandomNumber(Settings.ActiveRedRangeMin, Settings.ActiveRedRangeMax); //set the red to a random number between some values
                drawing.SetDirty(); //make it so that we can update the drawing
            }

            if (Tools.WillMutate(Settings.ActiveGreenMutationRate))
            {
                Green = Tools.GetRandomNumber(Settings.ActiveGreenRangeMin, Settings.ActiveGreenRangeMax);
                drawing.SetDirty();
            }

            if (Tools.WillMutate(Settings.ActiveBlueMutationRate))
            {
                Blue = Tools.GetRandomNumber(Settings.ActiveBlueRangeMin, Settings.ActiveBlueRangeMax);
                drawing.SetDirty();
            }

            if (Tools.WillMutate(Settings.ActiveAlphaMutationRate))
            {
                Alpha = Tools.GetRandomNumber(Settings.ActiveAlphaRangeMin, Settings.ActiveAlphaRangeMax);
                drawing.SetDirty();
            }
        }
    }
}