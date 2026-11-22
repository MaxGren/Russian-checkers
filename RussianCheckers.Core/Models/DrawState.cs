
namespace RussianCheckers.Core.Models
{
    public class DrawState
    {
        // Последовательные полуходы без взятий
        public int NoCapturePlies { get; set; }

        // Последовательные полуходы только дамками
        // без взятий и ходов простыми шашками
        public int KingOnlyPlies { get; set; }

        // Количество ходов каждой стороны
        // с момента изменения состава фигур
        public int WhiteMovesSinceMaterialChange { get; set; }

        public int BlackMovesSinceMaterialChange { get; set; }

        // Состав фигур на доске:
        // количество простых и дамок каждого цвета
        public string MaterialSignature { get; set; } = "";

        // Активное правило окончания по составу фигур
        public int? MaterialEndingLimit { get; set; }

        // Активное правило для ходов только дамками
        public int? KingOnlyEndingLimit { get; set; }


        public DrawState Clone()
        {
            return new DrawState
            {
                NoCapturePlies = NoCapturePlies,
                KingOnlyPlies = KingOnlyPlies,

                WhiteMovesSinceMaterialChange =
                    WhiteMovesSinceMaterialChange,

                BlackMovesSinceMaterialChange =
                    BlackMovesSinceMaterialChange,

                MaterialSignature = MaterialSignature,

                MaterialEndingLimit = MaterialEndingLimit,
                KingOnlyEndingLimit = KingOnlyEndingLimit
            };
        }

    }
}
